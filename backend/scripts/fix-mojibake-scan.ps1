<#
.SYNOPSIS
    Scans PawTrackDev for mojibake (UTF-8 bytes mis-decoded as Latin-1/Windows-1252,
    e.g. "SiamÃ©s" instead of "Siamés") across every nvarchar/varchar column, and
    optionally repairs it in place.

.DESCRIPTION
    Root cause: seed .sql files contain real UTF-8 accented characters, but sqlcmd
    was invoked without -f 65001, so it read the file using the console/OEM codepage
    and stored the mis-decoded bytes verbatim. This script detects the corruption
    signature (Ã/Â/â lead bytes) and repairs it via the standard Latin1->UTF8
    mojibake-fix round trip, generating NCHAR()-safe UPDATE statements (same pattern
    already used in fix-encoding.sql) so the fix itself can't be re-corrupted.

.PARAMETER Apply
    Without this switch, the script only reports what it WOULD fix (dry run).

.EXAMPLE
    .\fix-mojibake-scan.ps1                # dry run, prints a report
    .\fix-mojibake-scan.ps1 -Apply          # applies the fixes
#>
param(
    [string]$ServerInstance = "(localdb)\MSSQLLocalDB",
    [string]$Database = "PawTrackDev",
    [switch]$Apply
)

Import-Module SQLPS -DisableNameChecking -ErrorAction SilentlyContinue
$cp1252 = [System.Text.Encoding]::GetEncoding(1252)
$latin1 = [System.Text.Encoding]::GetEncoding("ISO-8859-1")
$utf8 = New-Object System.Text.UTF8Encoding($false, $true) # throw on invalid bytes

function Get-MojibakeFix([string]$value) {
    # Different seed runs mis-decoded the original UTF-8 bytes using different
    # legacy codepages (Windows-1252 "ANSI" vs true ISO-8859-1) — try both and
    # keep whichever round-trips to a shorter, validly-decoded UTF-8 string.
    $candidates = @()
    foreach ($enc in @($cp1252, $latin1)) {
        try {
            $bytes = $enc.GetBytes($value)
            $candidates += $utf8.GetString($bytes)
        } catch { }
    }
    $best = $candidates | Where-Object { $_ -ne $value -and $_.Length -lt $value.Length } | Select-Object -First 1
    return $best
}

function ConvertTo-SqlNCharExpression([string]$value) {
    if ([string]::IsNullOrEmpty($value)) { return "N''" }
    $parts = New-Object System.Collections.Generic.List[string]
    $asciiRun = New-Object System.Text.StringBuilder
    foreach ($ch in $value.ToCharArray()) {
        if ([int]$ch -le 127) {
            [void]$asciiRun.Append($ch)
        } else {
            if ($asciiRun.Length -gt 0) {
                $parts.Add("N'" + ($asciiRun.ToString() -replace "'", "''") + "'")
                $asciiRun.Clear() | Out-Null
            }
            $parts.Add("NCHAR($([int]$ch))")
        }
    }
    if ($asciiRun.Length -gt 0) {
        $parts.Add("N'" + ($asciiRun.ToString() -replace "'", "''") + "'")
    }
    return ($parts -join " + ")
}

# ── Discover every string column + its single-column PK ──────────────────────
$columns = Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $Database -Query @"
SELECT s.name AS SchemaName, t.name AS TableName, c.name AS ColumnName
FROM sys.columns c
JOIN sys.tables t ON t.object_id = c.object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE ty.name IN ('nvarchar','nchar','varchar','char')
ORDER BY t.name, c.column_id;
"@

$pks = Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $Database -Query @"
SELECT tc.TABLE_NAME, kcu.COLUMN_NAME
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
  ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME AND tc.TABLE_NAME = kcu.TABLE_NAME
WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY';
"@
$pkByTable = @{}
foreach ($pk in $pks) { $pkByTable[$pk.TABLE_NAME] = $pk.COLUMN_NAME }

$totalFixed = 0
$totalFound = 0

foreach ($col in $columns) {
    $table = "[$($col.SchemaName)].[$($col.TableName)]"
    $column = $col.ColumnName
    $pkColumn = $pkByTable[$col.TableName]
    if (-not $pkColumn) { continue } # skip tables without a single-column PK (composite keys)

    $signatureQuery = @"
SELECT [$pkColumn] AS Pk, [$column] AS Val
FROM $table
WHERE [$column] LIKE N'%[^ -~]%'
"@
    $rows = Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $Database -Query $signatureQuery -ErrorAction SilentlyContinue
    if (-not $rows) { continue }

    foreach ($row in $rows) {
        $original = $row.Val
        if ([string]::IsNullOrEmpty($original)) { continue }
        $fixedValue = Get-MojibakeFix $original
        if (-not $fixedValue) { continue }

        $totalFound++
        Write-Host "[$($col.TableName)].[$column] Pk=$($row.Pk)" -ForegroundColor Yellow
        Write-Host "  before: $original"
        Write-Host "  after : $fixedValue"

        if ($Apply) {
            $expr = ConvertTo-SqlNCharExpression $fixedValue
            $updateQuery = "UPDATE $table SET [$column] = $expr WHERE [$pkColumn] = '$($row.Pk)';"
            Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $Database -Query $updateQuery
            $totalFixed++
        }
    }
}

Write-Host ""
if ($Apply) {
    Write-Host "Fixed $totalFixed corrupted value(s)." -ForegroundColor Green
} else {
    Write-Host "Found $totalFound corrupted value(s) (dry run — re-run with -Apply to fix)." -ForegroundColor Cyan
}
