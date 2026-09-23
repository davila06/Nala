[CmdletBinding()]
param(
    [string]$MatrixPath = (Join-Path $PSScriptRoot "..\docs\CLAIM_EVIDENCE_MATRIX.md")
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $MatrixPath)) {
    throw "Missing claim evidence matrix: $MatrixPath"
}

$lines = Get-Content -Path $MatrixPath
$headerIndex = $lines.IndexOf(($lines | Where-Object { $_ -like "| Claim *" } | Select-Object -First 1))
if ($headerIndex -lt 0) {
    throw "Claim matrix header was not found."
}

$header = $lines[$headerIndex]
foreach ($requiredColumn in @("Responsable", "Vence evidencia", "Referencia aprobación")) {
    if ($header -notlike "*$requiredColumn*") {
        throw "Claim matrix is missing required column: $requiredColumn"
    }
}

$failures = [System.Collections.Generic.List[string]]::new()
foreach ($line in $lines[($headerIndex + 2)..($lines.Count - 1)]) {
    if ($line -notmatch '^\|[^|]+\|') { continue }
    $columns = @($line.Trim('|').Split('|') | ForEach-Object { $_.Trim() })
    if ($columns.Count -lt 9) { continue }

    $claim = $columns[0]
    if ([string]::IsNullOrWhiteSpace($columns[5])) { $failures.Add("$claim has no responsible owner.") }
    if ([string]::IsNullOrWhiteSpace($columns[6])) { $failures.Add("$claim has no evidence expiry.") }
    if ([string]::IsNullOrWhiteSpace($columns[7])) { $failures.Add("$claim has no approval reference.") }
    if ($columns[8] -eq "Aprobado" -and $columns[7] -eq "Pendiente") {
        $failures.Add("$claim is marked Aprobado without an approval reference.")
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    throw "Claim governance validation failed."
}

Write-Host "Claim governance validation passed." -ForegroundColor Green
