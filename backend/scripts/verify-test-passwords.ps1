$dll = "C:\Nala\backend\out-adoptions\BCrypt.Net-Next.dll"
[System.Reflection.Assembly]::LoadFrom($dll) | Out-Null

$passwordsToCheck = @{
    'admin@pawtrack.cr' = 'Test123!'
    'admin@pawtrack.test' = 'Admin123!'
    'owner_free@test.cr' = 'Test123!'
    'owner_plus@test.cr' = 'Test123!'
    'owner_familia@test.cr' = 'Test123!'
    'owner@pawtrack.test' = 'Test123!'
    'ally@test.cr' = 'Test123!'
    'ally@pawtrack.test' = 'Ally123!'
    'clinica_basica@test.cr' = 'Test123!'
    'clinica_partner@test.cr' = 'Test123!'
    'clinic@pawtrack.test' = 'Clinic123!'
    'municipal_basica@test.cr' = 'Test123!'
    'municipal_full@test.cr' = 'Test123!'
    'municipal_regional@test.cr' = 'Test123!'
    'maria.garcia@pawtrack.test' = 'Test123!'
    'patitas.felices@pawtrack.test' = 'Test123!'
    'refugio.animal.cr@pawtrack.test' = 'Test123!'
    'animal.house@pawtrack.test' = 'Test123!'
    'vet.angeles@pawtrack.test' = 'Test123!'
    'provider@pawtrack.test' = 'Test123!'
    'tienda_activa@test.cr' = 'Test123!'
    'soporte_bienestar@test.cr' = 'Test123!'
}

$sqlcmdOutput = sqlcmd -S "(localdb)\MSSQLLocalDB" -d PawTrackDev -Q "SET NOCOUNT ON; SELECT Email, PasswordHash FROM dbo.Users;" -W -s "|"
$lines = $sqlcmdOutput | Where-Object { $_ -match '\|' -and $_ -notmatch '^-' -and $_ -notmatch '^Email' }

foreach ($line in $lines) {
    $parts = $line.Split('|')
    $email = $parts[0].Trim()
    $hash = $parts[1].Trim()
    if ($passwordsToCheck.ContainsKey($email)) {
        $expectedPass = $passwordsToCheck[$email]
        try {
            $valid = [BCrypt.Net.BCrypt]::Verify($expectedPass, $hash)
            Write-Host "$email : $(if ($valid) { 'OK' } else { 'FAIL' })"
        } catch {
            Write-Host "$email : ERROR ($($_.Exception.Message))"
        }
    }
}
