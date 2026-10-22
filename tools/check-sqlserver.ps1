# AshkanLMS Stage 52 - read-only SQL Server readiness check.
# Run on Windows PowerShell 5.1 or PowerShell 7 with System.Data.SqlClient available.
[CmdletBinding()]
param(
    [string]$ConfigPath = (Join-Path $PSScriptRoot '..\AshkanLMS\App.config'),
    [switch]$RequireRelational,
    [switch]$RequireAudit
)
$ErrorActionPreference = 'Stop'
$failures = 0
function Result([string]$Name, [bool]$Ok, [string]$Message) {
    $script:failures += [int](-not $Ok)
    $tag = if ($Ok) { 'PASS' } else { 'FAIL' }
    Write-Host ('[{0}] {1}: {2}' -f $tag, $Name, $Message)
}
try {
    $resolved = (Resolve-Path -LiteralPath $ConfigPath).Path
    [xml]$config = Get-Content -LiteralPath $resolved -Raw -Encoding UTF8
    $modeNode = @($config.configuration.appSettings.add) | Where-Object { $_.key -eq 'PersistenceMode' } | Select-Object -First 1
    $mode = if ($modeNode) { [string]$modeNode.value } else { 'Json' }
    Write-Host ('PersistenceMode: {0}' -f $mode)
    if ($mode -ne 'SqlServer') {
        Write-Host '[INFO] Main LMS data is stored in JSON mode; SQL connectivity is optional for relational workspaces.'
    }
    $connectionNode = @($config.configuration.connectionStrings.add) | Where-Object { $_.name -eq 'AshkanLMS' } | Select-Object -First 1
    if (-not $connectionNode -or [string]::IsNullOrWhiteSpace([string]$connectionNode.connectionString)) {
        Result 'Configuration' $false 'AshkanLMS connection string entry is missing.'
    } else {
        Add-Type -AssemblyName System.Data
        $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder([string]$connectionNode.connectionString)
        Result 'Configuration' $true ('Server={0}; Database={1}; IntegratedSecurity={2}; Encrypt={3}; TrustServerCertificate={4}' -f $builder.DataSource, $builder.InitialCatalog, $builder.IntegratedSecurity, $builder.Encrypt, $builder.TrustServerCertificate)
        $connection = New-Object System.Data.SqlClient.SqlConnection($builder.ConnectionString)
        try {
            $connection.Open()
            Result 'Connection' $true 'SQL Server connection opened successfully.'
            $cmd = $connection.CreateCommand()
            $cmd.CommandText = 'SELECT DB_NAME()'
            $db = [string]$cmd.ExecuteScalar()
            Result 'Database' (-not [string]::IsNullOrWhiteSpace($db)) ('Connected database: {0}' -f $db)
            $tables = @('LmsState')
            if ($RequireRelational) { $tables += @('LmsStudents','LmsInstructors','LmsCourses','LmsEnrollments') }
            if ($RequireAudit) { $tables += @('LmsRelationalAudit','LmsAuditReviews','LmsAuditReviewHistory','LmsDeletionThresholdRules') }
            foreach ($table in $tables) {
                $check = $connection.CreateCommand()
                $check.CommandText = "SELECT CASE WHEN OBJECT_ID(@tableName, N'U') IS NOT NULL THEN 1 ELSE 0 END"
                $null = $check.Parameters.Add('@tableName', [System.Data.SqlDbType]::NVarChar, 256)
                $check.Parameters['@tableName'].Value = 'dbo.' + $table
                $exists = ([int]$check.ExecuteScalar() -eq 1)
                Result ('Table dbo.' + $table) $exists $(if ($exists) { 'Present' } else { 'Missing or inaccessible' })
                $check.Dispose()
            }
            $cmd.Dispose()
        } catch [System.Data.SqlClient.SqlException] {
            # Avoid emitting connection strings, passwords, server responses or SQL error details.
            Result 'Connection / query' $false ('SQL error number {0}. Check server availability, database name, Windows identity and TLS certificate trust.' -f $_.Exception.Number)
        } finally {
            if ($connection) { $connection.Dispose() }
        }
    }
} catch {
    Result 'Diagnostic setup' $false ('Unable to read configuration or initialize SQL client ({0}).' -f $_.Exception.GetType().Name)
}
if ($failures -gt 0) { Write-Host ('Result: {0} failed check(s).' -f $failures); exit 1 }
Write-Host 'Result: all requested checks passed.'
exit 0
