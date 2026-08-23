[CmdletBinding()]
param(
    [string]$OutputPath,
    [string]$PlanPath,
    [switch]$Execute,
    [string]$JournalPath,
    [string]$ConfirmPlanSha256
)

$ErrorActionPreference = 'Stop'
$repository = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$toolingRoot = [Environment]::GetEnvironmentVariable('RWT_CASCADE_TOOLING_ROOT')
if ([string]::IsNullOrWhiteSpace($toolingRoot))
{
    $toolingRoot = 'A:\Dev\RimWorld\Worktrees\RimWorld-Tooling\phase-a'
}
$arguments = @(
    '-Manifest', (Join-Path $repository 'Tools\CascadeManifest.json'),
    '-Repository', $repository
)
if (-not [string]::IsNullOrWhiteSpace($OutputPath))
{
    $arguments += @('-OutputPath', $OutputPath)
}
if (-not [string]::IsNullOrWhiteSpace($PlanPath))
{
    $arguments += @('-PlanPath', $PlanPath)
}
if ($Execute)
{
    $arguments += @('-Execute', '-JournalPath', $JournalPath,
        '-ConfirmPlanSha256', $ConfirmPlanSha256)
}
& powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass `
    -File (Join-Path $toolingRoot 'tools\Invoke-RimWorldCascade.ps1') `
    @arguments
exit $LASTEXITCODE
