[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("Staging", "Production")]
    [string]$Environment,

    [string]$EvidencePath = (Join-Path $PSScriptRoot "external-provider-evidence.local.json"),

    [switch]$AllowStaleEvidence
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $EvidencePath)) {
    throw "Missing provider evidence file: $EvidencePath. Copy scripts/external-provider-evidence.example.json, keep it outside source control, and record approved validation results without credentials."
}

$evidence = Get-Content -Raw $EvidencePath | ConvertFrom-Json
$requiredProviders = @("Azure", "WhatsApp", "GPS", "Payments", "Email")
$failures = [System.Collections.Generic.List[string]]::new()

foreach ($provider in $requiredProviders) {
    $entry = @($evidence.providers | Where-Object { $_.name -eq $provider })
    if ($entry.Count -ne 1) {
        $failures.Add("$provider must have exactly one evidence entry.")
        continue
    }

    $record = $entry[0]
    if ($record.environment -ne $Environment) {
        $failures.Add("$provider evidence is for '$($record.environment)', expected '$Environment'.")
    }
    if ($record.status -ne "Validated") {
        $failures.Add("$provider status must be 'Validated'.")
    }
    if ([string]::IsNullOrWhiteSpace($record.contractReference)) {
        $failures.Add("$provider requires a contract, account, or approval reference.")
    }
    if ([string]::IsNullOrWhiteSpace($record.validatedAtUtc)) {
        $failures.Add("$provider requires validatedAtUtc.")
    }
    else {
        $validatedAt = [DateTimeOffset]::MinValue
        $validDate = [DateTimeOffset]::TryParse(
            [string]$record.validatedAtUtc,
            [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::AssumeUniversal,
            [ref]$validatedAt)
        if (-not $validDate) {
            $failures.Add("$provider validatedAtUtc must be a valid UTC timestamp.")
        }
        elseif (-not $AllowStaleEvidence -and $validatedAt -lt [DateTimeOffset]::UtcNow.AddDays(-90)) {
            $failures.Add("$provider evidence is older than 90 days.")
        }
    }
    if ([string]::IsNullOrWhiteSpace($record.testReference)) {
        $failures.Add("$provider requires a redacted test reference.")
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    throw "External provider readiness failed. No provider credentials were read or printed."
}

Write-Host "External provider readiness passed for $Environment." -ForegroundColor Green
