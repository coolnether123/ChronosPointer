[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Configuration
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repository = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$toolingRoot = [Environment]::GetEnvironmentVariable('RWT_CASCADE_TOOLING_ROOT')
$outputRoot = [Environment]::GetEnvironmentVariable('RWT_CASCADE_BUILD_OUTPUT_ROOT')
if ([string]::IsNullOrWhiteSpace($toolingRoot) -or
    [string]::IsNullOrWhiteSpace($outputRoot))
{
    throw 'ChronosPointer cascade build environment is missing.'
}

$buildTool = Join-Path ([System.IO.Path]::GetFullPath($toolingRoot)) `
    'tools\Invoke-RimWorldBuild.ps1'
$project = Join-Path $repository 'Source\ChronosPointer.csproj'

function Invoke-ChronosBuild
{
    param(
        [Parameter(Mandatory = $true)][string]$ProjectConfiguration,
        [Parameter(Mandatory = $true)][string]$GameVersion,
        [Parameter(Mandatory = $true)][string]$DependencyList,
        [Parameter(Mandatory = $true)][string]$ResultName
    )

    $resultPath = Join-Path $outputRoot $ResultName
    & powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass `
        -File $buildTool `
        -Project $project `
        -Configuration $ProjectConfiguration `
        -Version $GameVersion `
        -OutputRoot $outputRoot `
        -Engine MSBuild `
        -Dependency $DependencyList `
        -ResultPath $resultPath | Out-Null
    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf))
    {
        throw "No build result was returned for $ProjectConfiguration."
    }
    $result = Get-Content -Raw -LiteralPath $resultPath | ConvertFrom-Json
    if (-not [bool]$result.Succeeded)
    {
        throw "RimWorld $GameVersion build failed with exit code $($result.ExitCode)."
    }
    return $result
}

$embedded = Invoke-ChronosBuild `
    -ProjectConfiguration $Configuration `
    -GameVersion $Configuration `
    -DependencyList 'harmony' `
    -ResultName 'embedded-build-result.json'
$built = Join-Path $outputRoot 'build\ChronosPointer.dll'
if (-not (Test-Path -LiteralPath $built -PathType Leaf))
{
    throw "Expected embedded assembly was not produced: $built"
}
$embeddedPayload = Join-Path $repository "$Configuration\Assemblies"
[System.IO.Directory]::CreateDirectory($embeddedPayload) | Out-Null
[System.IO.File]::Copy(
    $built,
    (Join-Path $embeddedPayload 'ChronosPointer.dll'),
    $true)

if ($Configuration -eq '1.6')
{
    $external = Invoke-ChronosBuild `
        -ProjectConfiguration 'External1.6' `
        -GameVersion '1.6' `
        -DependencyList 'harmony,spine' `
        -ResultName 'external-build-result.json'
    $externalBuilt = Join-Path $outputRoot 'build\ChronosPointer.dll'
    if (-not (Test-Path -LiteralPath $externalBuilt -PathType Leaf))
    {
        throw "Expected external-Spine assembly was not produced: $externalBuilt"
    }
    $externalPayload = Join-Path $repository '1.6\ExternalSpine\Assemblies'
    [System.IO.Directory]::CreateDirectory($externalPayload) | Out-Null
    [System.IO.File]::Copy(
        $externalBuilt,
        (Join-Path $externalPayload 'ChronosPointer.dll'),
        $true)
}

Write-Host "ChronosPointer $Configuration build completed; embedded payload refreshed."
