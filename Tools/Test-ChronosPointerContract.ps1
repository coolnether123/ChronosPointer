[CmdletBinding()]
param(
    [string]$RepositoryRoot = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($RepositoryRoot))
{
    $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}

function Assert-Contract
{
    param(
        [Parameter(Mandatory = $true)][bool]$Condition,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if (-not $Condition)
    {
        throw "CHRONOS-CONTRACT: $Message"
    }
}

$sourcePath = Join-Path $RepositoryRoot 'Source\ChronosPointerMod.cs'
$loadFoldersPath = Join-Path $RepositoryRoot 'LoadFolders.xml'
$aboutPath = Join-Path $RepositoryRoot 'About\About.xml'
$source = Get-Content -Raw -LiteralPath $sourcePath
[xml]$loadFolders = Get-Content -Raw -LiteralPath $loadFoldersPath
[xml]$about = Get-Content -Raw -LiteralPath $aboutPath

$spineBranch = [regex]::Match(
    $source,
    '(?s)private static void InstallSpinePatches.*?#else').Value
$embeddedBranch = [regex]::Match(
    $source,
    '(?s)private static void InstallEmbeddedPatches.*?#endif').Value

Assert-Contract (
    $source -match 'SpineApi\.Patching\.CreateInstaller' -and
    $spineBranch -match 'PatchAllOnce\(Assembly\.GetExecutingAssembly\(\)\)' -and
    $spineBranch -notmatch 'new\s+(?:HarmonyLib\.)?Harmony\s*\(') `
    'external-Spine builds must install through the Spine installer and may not construct Harmony.'
Assert-Contract (
    $embeddedBranch -match 'embedded/legacy' -and
    $embeddedBranch -match 'new\s+HarmonyLib\.Harmony\(HarmonyId\)' -and
    $embeddedBranch -match 'PatchAll\(Assembly\.GetExecutingAssembly\(\)\)') `
    'embedded/legacy fallback must remain explicit and use the stable assembly owner.'
Assert-Contract (
    $source -match 'private const string HarmonyId = "com\.coolnether123\.ChronosPointer"' -and
    $source -match 'PatchInstaller\.PatchAllOnce') `
    'the production owner ID and idempotent installer call must remain present.'
Assert-Contract (
    $source -match 'Harmony is unavailable' -and
    $source -match 'installation failed') `
    'dependency and installer failures must be observable rather than silently degraded.'

$versionNode = $loadFolders.SelectSingleNode('/loadFolders/v1.6')
$entries = @($versionNode.SelectNodes('./li'))
Assert-Contract (
    @($entries | Where-Object {
        $_.GetAttribute('IfModNotActive') -eq 'CoolNether123.Spine' -and
        $_.InnerText.Trim() -eq '1.6'
    }).Count -gt 0) `
    'embedded 1.6 payload selection must remain conditional on Spine being absent.'
Assert-Contract (
    @($entries | Where-Object {
        $_.GetAttribute('IfModActive') -eq 'CoolNether123.Spine' -and
        $_.InnerText.Trim() -eq '1.6/ExternalSpine'
    }).Count -gt 0) `
    'external-Spine 1.6 payload selection must remain conditional on Spine being active.'

foreach ($version in @('1.3', '1.4', '1.5', '1.6'))
{
    Assert-Contract (
        @($about.ModMetaData.supportedVersions.li | ForEach-Object { [string]$_ }) -contains $version -and
        (Test-Path -LiteralPath (Join-Path $RepositoryRoot "$version\Assemblies\ChronosPointer.dll") -PathType Leaf)
    ) "supported version $version must be declared and have a tracked payload."
}

Write-Host 'ChronosPointer ownership and variant contract tests passed.'
