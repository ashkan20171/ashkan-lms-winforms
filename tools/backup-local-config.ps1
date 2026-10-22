# Stage 60: explicit, non-destructive backup of local user configuration.
# Does not back up the LMS data store or SQL database. Windows PowerShell 5.1+.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$InstallDir,
    [Parameter(Mandatory=$true)][string]$BackupDir,
    [switch]$IncludeSensitiveConfig
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$source = Join-Path $InstallDir 'AshkanLMS.exe.config'
if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw 'Installed application config not found.' }
if (-not (Test-Path -LiteralPath (Join-Path $InstallDir 'AshkanLMS.exe') -PathType Leaf)) { throw 'Installation executable not found.' }
$raw = [IO.File]::ReadAllText($source)
try { [xml]$null = $raw } catch { throw 'Config is not valid XML.' }
if (-not $IncludeSensitiveConfig -and $raw -match '(?i)(password|pwd)\s*=') {
    throw 'Configuration may contain a SQL password. For a private backup, rerun with -IncludeSensitiveConfig and secure the destination.'
}
$srcFull = [IO.Path]::GetFullPath($source)
$targetFull = [IO.Path]::GetFullPath((Join-Path $BackupDir 'AshkanLMS.exe.config'))
if ([string]::Equals($srcFull,$targetFull,[StringComparison]::OrdinalIgnoreCase)) { throw 'Backup destination must differ from source.' }
New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
# Refuse overwrite to preserve earlier snapshots.
if (Test-Path -LiteralPath $targetFull) { throw 'Backup file already exists. Use a new, empty backup directory.' }
Copy-Item -LiteralPath $source -Destination $targetFull -ErrorAction Stop
$srcHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$dstHash = (Get-FileHash -LiteralPath $targetFull -Algorithm SHA256).Hash
if ($srcHash -ne $dstHash) { Remove-Item -LiteralPath $targetFull -Force; throw 'Backup hash verification failed.' }
Write-Host 'Config backup verified with SHA256. This does NOT include user data or SQL databases.'
Write-Host ('Backup file: ' + $targetFull)
