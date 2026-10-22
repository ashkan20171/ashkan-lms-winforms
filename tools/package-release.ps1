# Stage 56: reproducible Windows Release package, PowerShell 5.1+
# Requires Visual Studio Build Tools / .NET Framework 4.8 targeting pack.
param([switch]$SkipBuild, [switch]$IncludeLocalConfig)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$solution = Join-Path $root 'AshkanLMS.sln'
$bin = Join-Path $root 'AshkanLMS\bin\Release'
$out = Join-Path $root 'dist'
$stage = Join-Path $out 'AshkanLMS-Release'
$archive = Join-Path $out 'AshkanLMS-Release.zip'
if (-not (Test-Path $solution)) { throw 'Solution file missing.' }
if (-not $SkipBuild) {
    $msbuild = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    $exe = if ($msbuild) { $msbuild.Source } else { $null }
    if (-not $exe) {
        $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
        if (Test-Path $vswhere) {
            $exe = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
        }
    }
    if (-not $exe) { throw 'MSBuild not found. Install Visual Studio .NET desktop development workload.' }
    & $exe $solution /t:Rebuild /p:Configuration=Release /v:minimal
    if ($LASTEXITCODE -ne 0) { throw "Release build failed with exit code $LASTEXITCODE." }
}
if (-not (Test-Path (Join-Path $bin 'AshkanLMS.exe'))) { throw 'Release executable not found. Run without -SkipBuild first.' }
# Never package local config silently. Config may contain SQL credentials.
$config = Join-Path $bin 'AshkanLMS.exe.config'
if ($IncludeLocalConfig -and (Test-Path $config)) {
    $raw = [IO.File]::ReadAllText($config)
    if ($raw -match '(?i)(password|pwd)\s*=') { throw 'Local config contains a password. Remove secrets before packaging.' }
}
New-Item -ItemType Directory -Force -Path $out | Out-Null
if (Test-Path $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage | Out-Null
Get-ChildItem -LiteralPath $bin -File | Where-Object {
    $_.Extension -in @('.exe','.dll','.pdb') -or ($IncludeLocalConfig -and $_.Name -eq 'AshkanLMS.exe.config')
} | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $stage }
if (Test-Path (Join-Path $bin 'Assets')) { Copy-Item (Join-Path $bin 'Assets') (Join-Path $stage 'Assets') -Recurse }
# Always include a safe, portable default config; never copy developer-local SQL credentials.
$defaultConfig = Join-Path $root 'AshkanLMS\App.config'
[xml]$safeConfig = Get-Content -LiteralPath $defaultConfig -Raw
$modeEntries = @($safeConfig.configuration.appSettings.add | Where-Object { $_.key -eq 'PersistenceMode' })
if ($modeEntries.Count -ne 1 -or $modeEntries[0].value -ne 'Json') { throw 'Default config must use Json persistence.' }
if ([IO.File]::ReadAllText($defaultConfig) -match '(?i)(password|pwd)\s*=') { throw 'Default config contains SQL credentials.' }
Copy-Item -LiteralPath $defaultConfig -Destination (Join-Path $stage 'AshkanLMS.exe.config') -Force
Copy-Item (Join-Path $root 'database') (Join-Path $stage 'database') -Recurse
@'
AshkanLMS - Windows deployment notes
- Requires Windows and .NET Framework 4.8 runtime.
- Configure the application's persistence mode and SQL connection locally.
- A safe default JSON configuration is included; developer-local secrets are not included.
- Review database scripts and back up before applying migrations.
- This is a portable release ZIP, NOT an MSI/EXE installer.
- Run the executable only after configuring its environment.
'@ | Set-Content -LiteralPath (Join-Path $stage 'DEPLOYMENT.txt') -Encoding UTF8
if (Test-Path $archive) { Remove-Item -LiteralPath $archive -Force }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $archive -CompressionLevel Optimal
# Fail packaging if basic artifact invariants do not hold.
& (Join-Path $PSScriptRoot 'test-release.ps1') -ReleaseDir $stage -RequireSqlScripts
if ($LASTEXITCODE -ne 0) { throw 'Release smoke checks failed.' }
Write-Host "Created $archive"
