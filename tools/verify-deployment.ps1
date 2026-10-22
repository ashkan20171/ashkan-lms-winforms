# Stage 58: read-only predeployment verification; no data or database modifications.
param([string]$InstallDir = '', [switch]$RequireSqlScripts)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$failed = 0
function Check([bool]$ok, [string]$message) {
  if ($ok) { Write-Host "PASS $message" }
  else { Write-Host "FAIL $message"; $script:failed++ }
}
Check (Test-Path (Join-Path $root 'AshkanLMS.sln')) 'Solution exists'
Check (Test-Path (Join-Path $root 'installer\AshkanLMS.iss')) 'Installer definition exists'
Check (Test-Path (Join-Path $root 'deployment\FIRST-RUN-GUIDE.txt')) 'First-run guide exists'
if ($RequireSqlScripts) {
  foreach ($n in 1..6) {
    $files = @(Get-ChildItem -Path (Join-Path $root 'database') -Filter ('{0:D3}_*.sql' -f $n) -ErrorAction SilentlyContinue)
    Check ($files.Count -eq 1) ('Migration {0:D3} exists once' -f $n)
  }
}
if ($InstallDir) {
  Check (Test-Path (Join-Path $InstallDir 'AshkanLMS.exe')) 'Installed executable exists'
  $cfg = Join-Path $InstallDir 'AshkanLMS.exe.config'
  Check (Test-Path $cfg) 'Installed configuration exists'
  if (Test-Path $cfg) {
    try {
      [xml]$xml = Get-Content -LiteralPath $cfg -Raw
      $mode = @($xml.configuration.appSettings.add | Where-Object { $_.key -eq 'PersistenceMode' })
      Check ($mode.Count -eq 1 -and $mode[0].value -in @('Json','SqlServer')) 'PersistenceMode is recognized'
    } catch { Check $false 'Installed config XML is valid' }
  }
  Check (Test-Path (Join-Path $InstallDir 'FIRST-RUN-GUIDE.txt')) 'Installed guide exists'
}
if ($failed -gt 0) { Write-Error "$failed deployment checks failed."; exit 1 }
Write-Host 'Deployment checks passed. This does not prove that the application launches or SQL Server is accessible.'
exit 0
