# Read-only release readiness check for Windows PowerShell 5.1+.
param([switch]$Build)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $root 'AshkanLMS\AshkanLMS.csproj'
$solution = Join-Path $root 'AshkanLMS.sln'
$issues = 0
function Check($name, $ok, $detail) {
    if ($ok) { Write-Host "PASS: $name $detail" }
    else { Write-Host "FAIL: $name $detail"; $script:issues++ }
}
Check 'Solution' (Test-Path $solution) ''
Check 'Project' (Test-Path $project) ''
try {
    [xml]$xml = Get-Content -LiteralPath $project -Raw
    $framework = [string]$xml.Project.PropertyGroup.TargetFrameworkVersion
    Check 'Target framework' ($framework -eq 'v4.8') "($framework)"
    $compiled = @($xml.Project.ItemGroup.Compile | Where-Object { $_ -ne $null })
    # Compile entries can be distributed across multiple ItemGroup nodes.
    $includes = @($xml.SelectNodes('//*[local-name()="Compile"]') | ForEach-Object { $_.GetAttribute('Include') })
    $missing = @($includes | Where-Object { -not (Test-Path (Join-Path (Split-Path $project) $_)) })
    Check 'C# source references' ($missing.Count -eq 0) "($($includes.Count) entries; $($missing.Count) missing)"
} catch { Check 'Project XML' $false 'cannot parse csproj' }
$expected = @('001_relational_schema.sql','002_rowversion_concurrency.sql','003_relational_audit.sql','004_audit_reviews.sql','005_audit_review_history.sql','006_deletion_threshold_rules.sql')
foreach ($file in $expected) { Check "SQL migration $file" (Test-Path (Join-Path $root "database\$file")) '' }
if ($Build) {
    $msbuild = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if (-not $msbuild) {
        $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
        if (Test-Path $vswhere) {
            $found = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
            if ($found) { $msbuild = @{ Source = $found } }
        }
    }
    if (-not $msbuild) { Check 'MSBuild' $false 'not found; install Visual Studio with .NET desktop development' }
    elseif ($issues -eq 0) {
        & $msbuild.Source $solution /t:Rebuild /p:Configuration=Release /v:minimal
        Check 'Release build' ($LASTEXITCODE -eq 0) "(exit code $LASTEXITCODE)"
    }
}
if ($issues -gt 0) { Write-Host "FAILED: $issues issue(s)."; exit 1 }
Write-Host 'PASS: readiness checks completed. Database connectivity and runtime behavior were NOT tested.'
exit 0
