<#
.SYNOPSIS
Runs the Chronos Pointer support-branch build cascade.

.DESCRIPTION
Starting from Dev, this script builds the source branch 1.6 payload, then merges
down the supported Chronos Pointer branch chain:

  Dev -> Support/1.5 -> Support/1.4 -> Support/1.3

Each support branch is built after its merge and committed locally. After the
final support build, versioned payload assembly folders are copied back to Dev
without merging support source code. Missing support branches are created from
the previous branch, which keeps the workflow usable while the repo is being
bootstrapped to the Better Work Tab style.
#>
[CmdletBinding()]
param(
    [string]$SourceBranch = "Dev",
    [string]$Remote = "origin",
    [string]$ReleaseLabel = "api-refactor",
    [string[]]$SupportVersions = @("1.5", "1.4", "1.3"),
    [switch]$SkipSourceBuild,
    [switch]$SkipPayloadCopyBackToDev,
    [switch]$Push
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$ProjectPath = "Source\ChronosPointer.csproj"
$PayloadCopyBackPaths = @(
    "1.3/Assemblies",
    "1.4/Assemblies",
    "1.5/Assemblies",
    "1.6/Assemblies"
)
$AutoResolvableConflictPatterns = @(
    "^[^/]+/Assemblies/ChronosPointer\.dll$"
)

function Invoke-Native {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments
    )

    Write-Host ">> $FilePath $($Arguments -join ' ')"
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

function Invoke-Git {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    Invoke-Native git @Arguments
}

function Get-GitOutput {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)

    $output = & git @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
    }

    if ($null -eq $output) {
        return @()
    }

    return @($output)
}

function Assert-CleanWorktree {
    $status = @(Get-GitOutput status --porcelain)
    if ($status.Count -gt 0) {
        throw "Working tree must be clean before continuing.`n$($status -join [Environment]::NewLine)"
    }
}

function Test-BranchExists {
    param([Parameter(Mandatory = $true)][string]$Branch)

    & git rev-parse --verify --quiet $Branch | Out-Null
    return $LASTEXITCODE -eq 0
}

function Get-SupportBranchName {
    param([Parameter(Mandatory = $true)][string]$Version)
    return "Support/$Version"
}

function Get-PayloadPath {
    param([Parameter(Mandatory = $true)][string]$Version)
    return "$Version/Assemblies"
}

function Get-UnmergedPaths {
    return @((Get-GitOutput diff --name-only --diff-filter=U) | ForEach-Object { $_ -replace "\\", "/" })
}

function Test-AutoResolvableConflictPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $normalized = $Path -replace "\\", "/"
    foreach ($pattern in $AutoResolvableConflictPatterns) {
        if ($normalized -match $pattern) {
            return $true
        }
    }

    return $false
}

function Invoke-MergeOrStopAtConflict {
    param(
        [Parameter(Mandatory = $true)][string]$PreviousBranch,
        [Parameter(Mandatory = $true)][string]$Message
    )

    Write-Host ">> git merge --no-ff $PreviousBranch -m `"$Message`""
    & git merge --no-ff $PreviousBranch -m $Message 2>&1 | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -eq 0) {
        return $false
    }

    $unmerged = @(Get-UnmergedPaths)
    if ($unmerged.Count -eq 0) {
        throw "Merge from $PreviousBranch failed without unmerged paths."
    }

    $unexpected = @($unmerged | Where-Object { !(Test-AutoResolvableConflictPath $_) })
    if ($unexpected.Count -gt 0) {
        throw "Merge from $PreviousBranch stopped at source conflicts:`n$($unexpected -join [Environment]::NewLine)"
    }

    Write-Host "Only expected payload DLL conflicts found. Rebuilding this branch will resolve:"
    $unmerged | ForEach-Object { Write-Host "  $_" }
    return $true
}

function Build-Version {
    param([Parameter(Mandatory = $true)][string]$Version)

    Invoke-Native dotnet build $ProjectPath -c $Version --no-restore
}

function Clear-BuildIntermediates {
    if (Test-Path -LiteralPath "Source\obj") {
        Invoke-Git restore -- Source\obj
        Invoke-Git clean -fd -- Source\obj
    }
}

function Commit-IfChanged {
    param(
        [Parameter(Mandatory = $true)][string[]]$Paths,
        [Parameter(Mandatory = $true)][string]$Header,
        [Parameter(Mandatory = $true)][string[]]$Body
    )

    Invoke-Git add -- @Paths
    $status = @(Get-GitOutput status --porcelain -- @Paths)
    if ($status.Count -eq 0) {
        Write-Host "No changes to commit for: $($Paths -join ', ')"
        return
    }

    $args = @("commit", "-m", $Header)
    foreach ($paragraph in $Body) {
        $args += @("-m", $paragraph)
    }

    Invoke-Git @args
}

function Build-And-CommitPayload {
    param(
        [Parameter(Mandatory = $true)][string]$Version,
        [Parameter(Mandatory = $true)][string]$Header
    )

    Build-Version -Version $Version
    Clear-BuildIntermediates
    Commit-IfChanged `
        -Paths @((Get-PayloadPath $Version)) `
        -Header $Header `
        -Body @(
            "Rebuild the RimWorld $Version assembly as part of the $ReleaseLabel support cascade.",
            "Only payload files under $(Get-PayloadPath $Version) are staged for this build commit."
        )
}

function Assert-PayloadOnlyChanges {
    $changed = @()
    $changed += @(Get-GitOutput diff --cached --name-only)
    $changed += @(Get-GitOutput diff --name-only)
    $unexpected = @($changed | Where-Object { $_ -notmatch "^1\.[3-6]/Assemblies/" })

    if ($unexpected.Count -gt 0) {
        throw "Payload copy-back touched non-payload paths:`n$($unexpected -join [Environment]::NewLine)"
    }
}

Assert-CleanWorktree
Invoke-Git checkout $SourceBranch
Assert-CleanWorktree

if (!$SkipSourceBuild) {
    Build-And-CommitPayload `
        -Version "1.6" `
        -Header "Build RimWorld 1.6 payload for $ReleaseLabel"
    Assert-CleanWorktree
}

$previousBranch = $SourceBranch
foreach ($version in $SupportVersions) {
    $branch = Get-SupportBranchName $version
    if (Test-BranchExists $branch) {
        Invoke-Git checkout $branch
    }
    else {
        Invoke-Git checkout -b $branch $previousBranch
    }
    Assert-CleanWorktree

    $mergeMessage = "Merge $previousBranch into RimWorld $version support for $ReleaseLabel"
    $mergeNeedsBuildResolution = Invoke-MergeOrStopAtConflict `
        -PreviousBranch $previousBranch `
        -Message $mergeMessage

    if ($mergeNeedsBuildResolution) {
        Build-Version -Version $version
        Clear-BuildIntermediates
        Invoke-Git add -- (Get-PayloadPath $version)

        $remainingConflicts = @(Get-UnmergedPaths)
        if ($remainingConflicts.Count -gt 0) {
            throw "Build did not resolve all payload conflicts:`n$($remainingConflicts -join [Environment]::NewLine)"
        }

        Invoke-Git commit `
            -m $mergeMessage `
            -m "Resolve expected payload DLL conflicts by rebuilding RimWorld $version."
    }
    else {
        Build-And-CommitPayload `
            -Version $version `
            -Header "Build RimWorld $version payload for $ReleaseLabel"
    }

    Assert-CleanWorktree
    $previousBranch = $branch
}

if (!$SkipPayloadCopyBackToDev) {
    Invoke-Git checkout $SourceBranch
    Assert-CleanWorktree

    Invoke-Git checkout $previousBranch -- @PayloadCopyBackPaths
    Assert-PayloadOnlyChanges

    Commit-IfChanged `
        -Paths $PayloadCopyBackPaths `
        -Header "Update $SourceBranch support payloads for $ReleaseLabel" `
        -Body @(
            "Copy rebuilt support assemblies from the completed support cascade back to $SourceBranch.",
            "No support source code is merged back into $SourceBranch."
        )

    Assert-CleanWorktree
}

if ($Push) {
    $branches = @($SourceBranch) + ($SupportVersions | ForEach-Object { Get-SupportBranchName $_ })
    Invoke-Git push $Remote @branches
}

Write-Host "Chronos Pointer support cascade complete."
