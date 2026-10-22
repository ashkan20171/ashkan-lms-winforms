# AshkanLMS Stage 61 - privacy-preserving, read-only Windows diagnostics
# Windows PowerShell 5.1+; no network requests, no database writes.
param(
    [string]$InstallDir = '',
    [string]$OutputPath = '',
    [switch]$IncludeMachineInfo
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$checks = New-Object 'System.Collections.Generic.List[object]'
function Add-Check([string]$name, [string]$status, [string]$guidance) {
    $checks.Add([pscustomobject]@{ Name=$name; Status=$status; Guidance=$guidance })
}
if (-not $InstallDir) { $InstallDir = Join-Path $env:ProgramFiles 'AshkanLMS' }
if (-not [IO.Path]::IsPathRooted($InstallDir)) { throw 'InstallDir must be an absolute path.' }
$exe = Join-Path $InstallDir 'AshkanLMS.exe'
$cfg = Join-Path $InstallDir 'AshkanLMS.exe.config'
Add-Check 'Executable' $(if(Test-Path -LiteralPath $exe -PathType Leaf){'PASS'}else{'FAIL'}) 'Install or rebuild the Release executable.'
if (Test-Path -LiteralPath $cfg -PathType Leaf) {
    try {
        [xml]$doc = Get-Content -LiteralPath $cfg -Raw
        $mode = @($doc.configuration.appSettings.add | Where-Object { $_.key -eq 'PersistenceMode' })
        if ($mode.Count -eq 1 -and $mode[0].value -in @('Json','SqlServer')) {
            Add-Check 'Configuration XML and mode' 'PASS' 'Configuration is readable. Database connectivity is not tested.'
        } else { Add-Check 'Configuration XML and mode' 'WARN' 'Check PersistenceMode in the installed config.' }
        # Deliberately never serialize connection strings, file content, server names, or exceptions.
    } catch { Add-Check 'Configuration XML and mode' 'FAIL' 'Check installed XML configuration syntax.' }
} else { Add-Check 'Configuration XML and mode' 'FAIL' 'Restore the installed application config.' }
try {
    $release = Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -Name Release -ErrorAction Stop
    Add-Check '.NET Framework 4.8+' $(if([int]$release.Release -ge 528040){'PASS'}else{'FAIL'}) 'Install .NET Framework 4.8 or newer.'
} catch { Add-Check '.NET Framework 4.8+' 'WARN' 'Verify the .NET Framework 4.8 runtime is installed.' }
try {
    $dir = Join-Path $env:LOCALAPPDATA 'AshkanLMS'
    Add-Check 'User data directory' $(if(Test-Path -LiteralPath $dir -PathType Container){'PASS'}else{'WARN'}) 'Directory may be created after first launch; verify permissions if startup fails.'
} catch { Add-Check 'User data directory' 'WARN' 'Check the user profile and local application data access.' }
$report = [ordered]@{ SchemaVersion=1; Product='AshkanLMS'; Stage=61; TimestampUtc=[DateTime]::UtcNow.ToString('o'); Checks=@($checks.ToArray()); Disclaimer='Read-only static diagnostics. No app launch, database connectivity or installer validation.' }
if ($IncludeMachineInfo) { $report['OSVersion'] = [Environment]::OSVersion.Version.ToString() }
$json = ConvertTo-Json -InputObject $report -Depth 6
if ($OutputPath) {
    $full = [IO.Path]::GetFullPath($OutputPath)
    $parent = [IO.Path]::GetDirectoryName($full)
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) { throw 'Output directory does not exist.' }
    if (Test-Path -LiteralPath $full) { throw 'Output file already exists; refusing to overwrite.' }
    # Write using CreateNew to avoid a race with another process.
    $bytes = New-Object System.Text.UTF8Encoding($false)
    $stream = New-Object System.IO.FileStream($full, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $writer = New-Object System.IO.StreamWriter($stream, $bytes); try { $writer.Write($json); $writer.Flush() } finally { $writer.Dispose() } } finally { $stream.Dispose() }
    Write-Host 'Sanitized diagnostic report saved.'
} else { Write-Output $json }
if (@($checks | Where-Object { $_.Status -eq 'FAIL' }).Count -gt 0) { exit 1 }
exit 0
