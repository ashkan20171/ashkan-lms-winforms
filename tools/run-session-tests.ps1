param([string]$Configuration='Release')
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot '..\tests\SessionLifetime.Tests\SessionLifetime.Tests.csproj'
$msbuild=(Get-Command msbuild.exe -ErrorAction SilentlyContinue).Source
if (-not $msbuild) { throw 'MSBuild not found. Open Visual Studio Developer PowerShell.' }
& $msbuild $project /t:Rebuild /p:Configuration=$Configuration /v:minimal
if ($LASTEXITCODE -ne 0) { throw 'Session test build failed.' }
$exe=Join-Path $PSScriptRoot "..\tests\SessionLifetime.Tests\bin\$Configuration\AshkanLMS.SessionLifetimeTests.exe"
& $exe
if ($LASTEXITCODE -ne 0) { throw 'Session lifetime tests failed.' }
