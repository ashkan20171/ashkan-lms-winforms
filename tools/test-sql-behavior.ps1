param(
    [Parameter(Mandatory=$true)][string]$Server,
    [Parameter(Mandatory=$true)][string]$Database,
    [switch]$TrustServerCertificate,
    [switch]$ConfirmDisposableDatabase
)
$ErrorActionPreference = 'Stop'
if (-not $ConfirmDisposableDatabase) { throw 'Pass -ConfirmDisposableDatabase only for a dedicated disposable test database.' }
if ($Database -match '^(?i:master|model|msdb|tempdb)$') { throw 'System databases are not allowed.' }
if ($Database -notmatch '^[A-Za-z][A-Za-z0-9_]{0,127}$') { throw 'Use a simple test database name.' }
if ($Database -notmatch '(?i:test|dev|sandbox)') { throw 'Database name must contain test, dev, or sandbox.' }
$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if (-not $sqlcmd) { throw 'sqlcmd is required.' }
$script = Join-Path (Split-Path -Parent $PSScriptRoot) 'database\008_transactional_behavior_tests.sql'
$args = @('-S', $Server, '-d', $Database, '-E', '-b', '-i', $script)
if ($TrustServerCertificate) { $args += '-C' }
& $sqlcmd.Source @args
if ($LASTEXITCODE -ne 0) { throw 'SQL behavioral integration tests failed.' }
Write-Host 'Stage 76 SQL behavioral checks passed; test rows rolled back.'
