[CmdletBinding()]
param(
    [switch]$RequireAllSqlScripts,
    [switch]$Build,
    [string]$Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'AshkanLMS.sln'
$project = Join-Path $root 'AshkanLMS\AshkanLMS.csproj'
$readme = Join-Path $root 'README.md'
$about = Join-Path $root 'ABOUT.md'
$security = Join-Path $root 'SECURITY.md'
$failures = New-Object System.Collections.Generic.List[string]
function Check-Path([string]$Path, [string]$Label) {
    if (Test-Path -LiteralPath $Path) { Write-Host "PASS  $Label" -ForegroundColor Green }
    else { Write-Host "FAIL  $Label ($Path)" -ForegroundColor Red; $script:failures.Add($Label) }
}
Check-Path $solution 'Solution file exists'
Check-Path $project 'C# project file exists'
Check-Path $readme 'Recruiter README exists'
Check-Path $about 'About file exists'
Check-Path $security 'Security policy exists'
if (Test-Path -LiteralPath $project) {
    [xml]$xml = Get-Content -LiteralPath $project -Raw
    $target = $xml.Project.PropertyGroup | Where-Object { $_.TargetFrameworkVersion } | Select-Object -First 1
    if ($target -and $target.TargetFrameworkVersion -eq 'v4.8') { Write-Host 'PASS  Target framework is .NET Framework 4.8' -ForegroundColor Green }
    else { Write-Host 'FAIL  Target framework is not confirmed as v4.8' -ForegroundColor Red; $failures.Add('Target framework') }
    $includes = @($xml.Project.ItemGroup.Compile | ForEach-Object { $_.Include } | Where-Object { $_ })
    $missing = @($includes | Where-Object { -not (Test-Path -LiteralPath (Join-Path (Split-Path $project -Parent) $_)) })
    if ($missing.Count -eq 0) { Write-Host "PASS  All $($includes.Count) Compile Include paths exist" -ForegroundColor Green }
    else { Write-Host "FAIL  Missing compile entries: $($missing -join ', ')" -ForegroundColor Red; $failures.Add('Compile Include paths') }
}
$sqlFiles = @(Get-ChildItem (Join-Path $root 'database') -Filter '*.sql' -File -ErrorAction SilentlyContinue)
if ($sqlFiles.Count -gt 0) { Write-Host "PASS  Found $($sqlFiles.Count) SQL scripts" -ForegroundColor Green }
else { Write-Host 'FAIL  No SQL scripts found' -ForegroundColor Red; $failures.Add('SQL scripts') }
if ($RequireAllSqlScripts) {
    foreach ($name in @('001_initial_schema.sql','001_relational_schema.sql','002_sql_state.sql','002_rowversion_concurrency.sql','003_relational_audit.sql','004_audit_reviews.sql','005_audit_review_history.sql','006_deletion_threshold_rules.sql')) {
        if (Test-Path (Join-Path $root "database\$name")) { Write-Host "PASS  $name" -ForegroundColor Green }
        else { Write-Host "FAIL  Missing $name" -ForegroundColor Red; $failures.Add($name) }
    }
}
if ($Build) {
    $msbuild = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if (-not $msbuild) { $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'; if (Test-Path $vswhere) { $install = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath; if ($install) { $candidate = Join-Path $install 'MSBuild\Current\Bin\MSBuild.exe'; if (Test-Path $candidate) { $msbuild = Get-Item $candidate } } } }
    if (-not $msbuild) { Write-Host 'FAIL  MSBuild not found; install Visual Studio Build Tools or run without -Build' -ForegroundColor Red; $failures.Add('MSBuild not found') }
    else { & $msbuild.Source $solution /t:Rebuild "/p:Configuration=$Configuration" /p:Platform='Any CPU' /m; if ($LASTEXITCODE -ne 0) { $failures.Add('MSBuild failed') } else { Write-Host 'PASS  MSBuild rebuild succeeded' -ForegroundColor Green } }
}
if ($failures.Count -gt 0) { Write-Host "`nValidation completed with $($failures.Count) failure(s): $($failures -join '; ')" -ForegroundColor Red; exit 1 }
Write-Host "`nValidation passed. Note: static checks do not replace runtime or security testing." -ForegroundColor Green
