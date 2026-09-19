$requiredApprovals = @(
    'PRODHAB_REGISTRATION_CONFIRMED',
    'AZURE_DPA_CONFIRMED',
    'B2B_CONTRACTS_APPROVED',
    'SLA_APPROVED',
    'PRICING_APPROVED'
)

$missing = $requiredApprovals | Where-Object {
    [Environment]::GetEnvironmentVariable($_) -ne 'true'
}

$evidenceReference = [Environment]::GetEnvironmentVariable('LEGAL_APPROVAL_REFERENCE')
if ([string]::IsNullOrWhiteSpace($evidenceReference)) {
    $missing += 'LEGAL_APPROVAL_REFERENCE'
}

if ($missing.Count -gt 0) {
    Write-Error "Production governance gate failed. Missing approvals: $($missing -join ', ')."
    exit 1
}

Write-Host "Production governance gate passed. Evidence: $evidenceReference"
