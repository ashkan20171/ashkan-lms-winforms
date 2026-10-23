param(
    [Parameter(Mandatory=$true)][string]$Server,
    [Parameter(Mandatory=$true)][string]$Database,
    [switch]$TrustServerCertificate
)
$ErrorActionPreference = 'Stop'
if ($Database -match '^(master|model|msdb|tempdb)$') { throw 'System databases are not allowed.' }
if ($Database -notmatch '^[A-Za-z][A-Za-z0-9_]{0,127}$') { throw 'Use a simple test database name (letters, digits, underscore).' }
$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if (-not $sqlcmd) { throw 'sqlcmd is required. Install SQL Server command-line utilities.' }
$script = Join-Path (Split-Path -Parent $PSScriptRoot) 'database\007_integration_smoke.sql'
$args = @('-S', $Server, '-d', $Database, '-E', '-b', '-i', $script)
if ($TrustServerCertificate) { $args += '-C' }
& $sqlcmd.Source @args
if ($LASTEXITCODE -ne 0) { throw 'SQL integration smoke checks failed.' }
Write-Host 'SQL integration smoke checks completed.'
