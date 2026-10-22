# Stage 59: read-only release smoke tests; Windows PowerShell 5.1+
param([string]$ReleaseDir = '', [switch]$RequireSqlScripts, [switch]$CheckInstaller)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not $ReleaseDir) { $ReleaseDir = Join-Path $root 'dist\AshkanLMS-Release' }
$failed = 0
function Assert-Check([bool]$ok, [string]$message) {
    if ($ok) { Write-Host ('PASS ' + $message) }
    else { Write-Host ('FAIL ' + $message); $script:failed++ }
}
Assert-Check (Test-Path -LiteralPath (Join-Path $ReleaseDir 'AshkanLMS.exe') -PathType Leaf) 'Release EXE exists'
Assert-Check (Test-Path -LiteralPath (Join-Path $ReleaseDir 'AshkanLMS.exe.config') -PathType Leaf) 'Release config exists'
$cfg = Join-Path $ReleaseDir 'AshkanLMS.exe.config'
if (Test-Path -LiteralPath $cfg -PathType Leaf) {
    try {
        [xml]$doc = Get-Content -LiteralPath $cfg -Raw
        $modes = @($doc.configuration.appSettings.add | Where-Object { $_.key -eq 'PersistenceMode' })
        Assert-Check ($modes.Count -eq 1 -and $modes[0].value -eq 'Json') 'Packaged default persistence is Json'
        $raw = [IO.File]::ReadAllText($cfg)
        Assert-Check ($raw -notmatch '(?i)(password|pwd)\s*=') 'No SQL password in packaged config'
    } catch { Assert-Check $false 'Release config is readable XML' }
}
if ($RequireSqlScripts) {
    foreach ($n in 1..6) {
        $folder = Join-Path $ReleaseDir 'database'
        $scripts = if (Test-Path -LiteralPath $folder) { @(Get-ChildItem -LiteralPath $folder -Filter ('{0:D3}_*.sql' -f $n) -File) } else { @() }
        Assert-Check ($scripts.Count -eq 1) ('SQL migration {0:D3} packaged exactly once' -f $n)
    }
}
if ($CheckInstaller) {
    $setup = Join-Path $root 'dist\installer\AshkanLMS-Setup-Stage66.exe'
    Assert-Check (Test-Path -LiteralPath $setup -PathType Leaf) 'Stage 60 installer exists'
}
if ($failed -gt 0) { Write-Host "$failed release check(s) failed."; exit 1 }
Write-Host 'Release artifact checks passed. Application launch, upgrade and SQL access still require Windows integration testing.'
exit 0
