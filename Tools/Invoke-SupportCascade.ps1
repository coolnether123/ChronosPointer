<#
.SYNOPSIS
Runs the Chronos Pointer all-version support-branch build cascade.

.DESCRIPTION
Starting from Dev, this script builds the 1.6 payload, then cascades through
every RimWorld support branch used by the Better Work Tab archive:

  Dev -> Support/1.5 -> Support/1.4 -> Support/1.3 -> Support/1.2
      -> Support/1.1 -> Support/1.0 -> Support/0.19 -> Support/0.18
      -> Support/0.17 -> Support/0.16 -> Support/0.15 -> Support/0.14
      -> Support/0.13 -> Support/Alpha4

The old 0.x/Alpha4 builds use Better Work Tab's decompiled RimWorld managed
folders through the ChronosPointer.csproj LegacyRimWorldManagedDir settings.
After the final branch build, versioned payload assembly folders are copied back
to Dev without merging support source code back upward.
#>
[CmdletBinding()]
param(
    [string]$SourceBranch = "Dev",
    [string]$Remote = "origin",
    [string]$ReleaseLabel = "all-version-support",
    [string[]]$SupportVersions = @("1.5", "1.4", "1.3", "1.2", "1.1", "1.0", "0.19", "0.18", "0.17", "0.16", "0.15", "0.14", "0.13", "Alpha4"),
    [string]$MSBuildPath,
    [switch]$SkipSourceBuild,
    [switch]$SkipPayloadCopyBackToDev,
    [switch]$Push
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$ProjectPath = "Source\ChronosPointer.csproj"
$PayloadCopyBackPaths = @(
    "0.13/Assemblies",
    "0.14/Assemblies",
    "0.15/Assemblies",
    "0.16/Assemblies",
    "0.17/Assemblies",
    "0.18/Assemblies",
    "0.19/Assemblies",
    "1.0/Assemblies",
    "1.1/Assemblies",
    "1.2/Assemblies",
    "1.3/Assemblies",
    "1.4/Assemblies",
    "1.5/Assemblies",
    "1.6/Assemblies",
    "Alpha4/Assemblies"
)
$AutoResolvableConflictPatterns = @(
    "^Assemblies/ChronosPointer\.dll$",
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

function Resolve-MSBuildPath {
    if ($MSBuildPath) {
        if (!(Test-Path -LiteralPath $MSBuildPath)) {
            throw "MSBuildPath does not exist: $MSBuildPath"
        }

        return (Resolve-Path -LiteralPath $MSBuildPath).Path
    }

    $programFilesX86 = [Environment]::GetEnvironmentVariable("ProgramFiles(x86)")
    $vswhere = Join-Path $programFilesX86 "Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path -LiteralPath $vswhere) {
        $found = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" |
            Select-Object -First 1
        if ($LASTEXITCODE -eq 0 -and $found) {
            return $found
        }
    }

    $knownPaths = @(
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
    )

    foreach ($path in $knownPaths) {
        if (Test-Path -LiteralPath $path) {
            return $path
        }
    }

    throw "Could not locate MSBuild. Pass -MSBuildPath explicitly."
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

    if ($Version -eq "Alpha4" -or $Version.StartsWith("0.")) {
        return "Assemblies"
    }

    return "$Version/Assemblies"
}

function Get-VersionedPayloadPath {
    param([Parameter(Mandatory = $true)][string]$Version)

    if ($Version -eq "Debug" -or $Version -eq "Release") {
        return "Assemblies"
    }

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
    param(
        [Parameter(Mandatory = $true)][string]$MSBuild,
        [Parameter(Mandatory = $true)][string]$Version
    )

    Invoke-Native $MSBuild $ProjectPath /t:Build /p:Configuration=$Version /p:Platform=AnyCPU /v:minimal
}

function Clear-BuildIntermediates {
    if (Test-Path -LiteralPath "Source\obj") {
        Invoke-Git restore -- Source\obj
        Invoke-Git clean -fd -- Source\obj
    }
}

function Copy-PayloadFileIfPresent {
    param(
        [Parameter(Mandatory = $true)][string]$SourceDir,
        [Parameter(Mandatory = $true)][string]$DestinationDir,
        [Parameter(Mandatory = $true)][string]$FileName
    )

    $source = Join-Path $SourceDir $FileName
    if (!(Test-Path -LiteralPath $source)) {
        return
    }

    if (!(Test-Path -LiteralPath $DestinationDir)) {
        New-Item -ItemType Directory -Path $DestinationDir -Force | Out-Null
    }

    Copy-Item -LiteralPath $source -Destination (Join-Path $DestinationDir $FileName) -Force
}

function Sync-VersionedPayloadFolder {
    param([Parameter(Mandatory = $true)][string]$Version)

    $buildPayloadPath = Get-PayloadPath $Version
    $versionedPayloadPath = Get-VersionedPayloadPath $Version
    if ($buildPayloadPath -eq $versionedPayloadPath) {
        return
    }

    Copy-PayloadFileIfPresent -SourceDir $buildPayloadPath -DestinationDir $versionedPayloadPath -FileName "ChronosPointer.dll"
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
        [Parameter(Mandatory = $true)][string]$MSBuild,
        [Parameter(Mandatory = $true)][string]$Version,
        [Parameter(Mandatory = $true)][string]$Header
    )

    Build-Version -MSBuild $MSBuild -Version $Version
    Clear-BuildIntermediates
    Sync-VersionedPayloadFolder $Version

    $payloadPaths = @((Get-PayloadPath $Version), (Get-VersionedPayloadPath $Version)) |
        Select-Object -Unique

    Commit-IfChanged `
        -Paths $payloadPaths `
        -Header $Header `
        -Body @(
            "Rebuild the RimWorld $Version assembly as part of the $ReleaseLabel support cascade.",
            "Only payload files under $($payloadPaths -join ', ') are staged for this build commit."
        )
}

function Assert-PayloadOnlyChanges {
    $changed = @()
    $changed += @(Get-GitOutput diff --cached --name-only)
    $changed += @(Get-GitOutput diff --name-only)
    $unexpected = @($changed | Where-Object { $_ -notmatch "^(1\.[0-6]/Assemblies/|0\.(13|14|15|16|17|18|19)/Assemblies/|Alpha4/Assemblies/)" })

    if ($unexpected.Count -gt 0) {
        throw "Payload copy-back touched non-payload paths:`n$($unexpected -join [Environment]::NewLine)"
    }
}

Assert-CleanWorktree
$resolvedMSBuild = Resolve-MSBuildPath
Write-Host "Using MSBuild: $resolvedMSBuild"

Invoke-Git checkout $SourceBranch
Assert-CleanWorktree

if (!$SkipSourceBuild) {
    Build-And-CommitPayload `
        -MSBuild $resolvedMSBuild `
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
        Build-Version -MSBuild $resolvedMSBuild -Version $version
        Clear-BuildIntermediates
        Sync-VersionedPayloadFolder $version

        $payloadPaths = @((Get-PayloadPath $version), (Get-VersionedPayloadPath $version)) |
            Select-Object -Unique
        Invoke-Git add -- @payloadPaths

        $remainingConflicts = @(Get-UnmergedPaths)
        if ($remainingConflicts.Count -gt 0) {
            throw "Build did not resolve all payload conflicts:`n$($remainingConflicts -join [Environment]::NewLine)"
        }

        Invoke-Git commit `
            -m $mergeMessage `
            -m "Resolve expected payload DLL conflicts by rebuilding RimWorld $version and syncing its versioned payload folder."
    }
    else {
        Build-And-CommitPayload `
            -MSBuild $resolvedMSBuild `
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
