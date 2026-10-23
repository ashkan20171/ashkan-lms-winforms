$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'tests\Authentication.Tests\Authentication.Tests.csproj'
$msbuild = Get-Command msbuild.exe -ErrorAction SilentlyContinue
if (-not $msbuild) {
  $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
  if (Test-Path $vswhere) {
    $found = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    if ($found) { $msbuild = @{ Source = $found } }
  }
}
if (-not $msbuild) { throw 'MSBuild not found.' }
& $msbuild.Source $project /t:Rebuild /p:Configuration=Release /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'Authentication test build failed.' }
$exe = Join-Path $root 'tests\Authentication.Tests\bin\Release\AshkanLMS.AuthenticationTests.exe'
& $exe
if ($LASTEXITCODE -ne 0) { throw 'Authentication tests failed.' }
