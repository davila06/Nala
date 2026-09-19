param(
    [Parameter(Mandatory = $true)]
    [string]$BaseRef,
    [Parameter(Mandatory = $true)]
    [string]$HeadRef
)

$migrationFiles = git diff --name-only $BaseRef $HeadRef -- `
    'backend/src/PawTrack.Infrastructure/Migrations/*.cs' `
    'backend/src/PawTrack.Infrastructure/Persistence/Migrations/*.cs' |
    Where-Object { $_ -notmatch '\.Designer\.cs$' }

$forbiddenOperations = @(
    'DropColumn',
    'DropTable',
    'RenameColumn',
    'RenameTable',
    'AlterColumn'
)

$violations = @()
foreach ($file in $migrationFiles) {
    if (-not (Test-Path $file)) { continue }

    $content = Get-Content $file -Raw
    $upStart = $content.IndexOf('protected override void Up')
    $downStart = $content.IndexOf('protected override void Down')
    if ($upStart -lt 0) { continue }

    $upBody = if ($downStart -gt $upStart) {
        $content.Substring($upStart, $downStart - $upStart)
    } else {
        $content.Substring($upStart)
    }

    foreach ($operation in $forbiddenOperations) {
        if ($upBody -match "migrationBuilder\.$operation\s*\(") {
            $violations += "$file uses $operation in Up()."
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Error ("Destructive migration blocked. Use expand-contract and move cleanup to a later release:`n" + ($violations -join "`n"))
    exit 1
}

Write-Host "Expand-contract migration check passed."
