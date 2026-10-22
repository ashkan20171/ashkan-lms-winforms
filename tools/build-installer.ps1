# Stage 57: Windows installer build. Requires Inno Setup 6 and VS/MSBuild.
param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$pack = Join-Path $PSScriptRoot 'package-release.ps1'
$stage = Join-Path $root 'dist\AshkanLMS-Release'
if (-not $SkipBuild) {
    & $pack
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw 'Release packaging failed.' }
} elseif (-not (Test-Path (Join-Path $stage 'AshkanLMS.exe'))) {
    throw 'Release executable missing. Run without -SkipBuild first.'
}
# Installer must contain a usable safe default config; never ship a developer-local secret.
$cfg = Join-Path $stage 'AshkanLMS.exe.config'
$defaultCfg = Join-Path $root 'AshkanLMS\App.config'
if (-not (Test-Path $defaultCfg)) { throw 'Default App.config missing.' }
[xml]$xml = Get-Content -LiteralPath $defaultCfg -Raw
$mode = @($xml.configuration.appSettings.add | Where-Object { $_.key -eq 'PersistenceMode' })
if ($mode.Count -ne 1 -or $mode[0].value -ne 'Json') { throw 'Default configuration must use Json mode.' }
$connection = @($xml.configuration.connectionStrings.add)
foreach ($item in $connection) {
    if ($item.connectionString -match '(?i)(password|pwd)\s*=') { throw 'Default configuration contains SQL credentials.' }
}
Copy-Item -LiteralPath $defaultCfg -Destination $cfg -Force
$compiler = Get-Command ISCC.exe -ErrorAction SilentlyContinue
$iscc = if ($compiler) { $compiler.Source } else { $null }
if (-not $iscc) {
    foreach ($path in @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "${env:ProgramFiles}\Inno Setup 6\ISCC.exe")) {
        if (Test-Path $path) { $iscc = $path; break }
    }
}
if (-not $iscc) { throw 'Inno Setup 6 compiler (ISCC.exe) not found.' }
$script = Join-Path $root 'installer\AshkanLMS.iss'
& $iscc $script
if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed: $LASTEXITCODE" }
$setup = Join-Path $root 'dist\installer\AshkanLMS-Setup-Stage66.exe'
if (-not (Test-Path $setup)) { throw 'Installer output missing.' }
& (Join-Path $PSScriptRoot 'test-release.ps1') -ReleaseDir $stage -RequireSqlScripts -CheckInstaller
if ($LASTEXITCODE -ne 0) { throw 'Installer smoke checks failed.' }
Write-Host "Installer created: $setup"
