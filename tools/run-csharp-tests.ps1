$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'tests\DiagnosticSummary.Tests\DiagnosticSummary.Tests.csproj'
$msbuild = Get-Command msbuild.exe -ErrorAction SilentlyContinue
if (-not $msbuild) {
  $vswhere = '${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe'
  $vswhere = [Environment]::ExpandEnvironmentVariables($vswhere)
  if (Test-Path $vswhere) {
    $found = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    if ($found) { $msbuild = @{ Source = $found } }
  }
}
if (-not $msbuild) { throw 'MSBuild not found. Install Visual Studio with .NET desktop development.' }
& $msbuild.Source $project /t:Rebuild /p:Configuration=Release /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'C# tests build failed' }
$exe = Join-Path $root 'tests\DiagnosticSummary.Tests\bin\Release\AshkanLMS.DiagnosticTests.exe'
& $exe
if ($LASTEXITCODE -ne 0) { throw 'C# tests failed' }
