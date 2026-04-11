param(
    [switch]$NoBackend,
    [switch]$NoFrontend,
    [switch]$RestartAzurite,
    [switch]$Mobile        # Expone Vite en la red local para probar desde el celular
)

$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string]$Message)
    Write-Host "[start-dev] $Message" -ForegroundColor Cyan
}

function Stop-PawTrackApiProcesses {
    $procs = Get-Process -Name "PawTrack.API" -ErrorAction SilentlyContinue
    if ($null -eq $procs) {
        Write-Step "No hay procesos PawTrack.API bloqueando binarios."
        return
    }

    foreach ($proc in $procs) {
        try {
            Stop-Process -Id $proc.Id -Force
            Write-Step "Proceso PawTrack.API detenido (PID $($proc.Id))."
        }
        catch {
            Write-Warning "No se pudo detener PawTrack.API PID $($proc.Id): $($_.Exception.Message)"
        }
    }
}

function Get-AzuritePortOwnerProcess {
    $conn = Get-NetTCPConnection -LocalPort 10000 -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $conn) {
        return $null
    }

    return Get-CimInstance Win32_Process -Filter "ProcessId = $($conn.OwningProcess)" -ErrorAction SilentlyContinue
}

function Ensure-AzuriteRunning {
    $owner = Get-AzuritePortOwnerProcess

    if ($RestartAzurite -and $null -ne $owner) {
        Write-Step "Restart de Azurite solicitado. Deteniendo proceso en puerto 10000 (PID $($owner.ProcessId))."
        try {
            Stop-Process -Id $owner.ProcessId -Force
            Start-Sleep -Seconds 1
            $owner = $null
        }
        catch {
            Write-Warning "No se pudo detener el proceso del puerto 10000: $($_.Exception.Message)"
        }
    }

    if ($null -ne $owner) {
        Write-Step "Puerto 10000 ya está en uso por '$($owner.Name)' (PID $($owner.ProcessId))."
        Write-Step "Se reutiliza la instancia actual de Azurite."
        return
    }

    Write-Step "Iniciando Azurite en background..."
    Start-Process -FilePath "cmd.exe" -ArgumentList @(
        "/c",
        "azurite --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0 --loose --location C:\Nala\.azurite --silent"
    ) -WindowStyle Normal | Out-Null

    for ($i = 0; $i -lt 30; $i++) {
        Start-Sleep -Seconds 1
        $owner = Get-AzuritePortOwnerProcess
        if ($null -ne $owner) {
            break
        }
    }

    if ($null -eq $owner) {
        throw "Azurite no quedó escuchando en el puerto 10000."
    }

    Write-Step "Azurite activo en puerto 10000 (PID $($owner.ProcessId))."
}

function Start-Backend {
    $backendPath = "C:\Nala\backend"
    Write-Step "Iniciando backend..."
    Start-Process -FilePath "dotnet" -WorkingDirectory $backendPath -ArgumentList @(
        "run",
        "--project", "src/PawTrack.API",
        "--launch-profile", "http"
    ) -WindowStyle Normal | Out-Null

    $ok = $false
    for ($i = 0; $i -lt 180; $i++) {
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:5199/health" -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -eq 200) {
                $ok = $true
                break
            }
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    if (-not $ok) {
        Write-Warning "El backend no confirmó salud en http://localhost:5199/health dentro del tiempo esperado."
        return
    }

    Write-Step "Backend listo en http://localhost:5199"
}

function Start-Frontend {
    $frontendPath = "C:\Nala\frontend"
    Write-Step "Iniciando frontend..."

    if ($Mobile) {
        # Detectar IP LAN (primera IPv4 no-loopback)
        $lanIp = (Get-NetIPAddress -AddressFamily IPv4 |
            Where-Object { $_.InterfaceAlias -notmatch 'Loopback|Hyper-V|WSL' -and $_.IPAddress -notmatch '^169\.' } |
            Select-Object -First 1).IPAddress

        if (-not $lanIp) {
            Write-Warning "No se detectó IP LAN. Usando 0.0.0.0 sin IP conocida."
            $lanIp = "<IP-de-tu-PC>"
        }

        # Crear .env.mobile con VITE_API_URL apuntando a la IP LAN
        $envPath = "$frontendPath\.env.mobile"
        "VITE_API_URL=http://${lanIp}:5199" | Set-Content -Path $envPath -Encoding UTF8
        Write-Step ".env.mobile creado: VITE_API_URL=http://${lanIp}:5199"

        Start-Process -FilePath "cmd.exe" -WorkingDirectory $frontendPath -ArgumentList @(
            "/c", "set VITE_API_URL=http://${lanIp}:5199 && npm run dev -- --host 0.0.0.0 --port 5173"
        ) -WindowStyle Normal | Out-Null

        Write-Host ""
        Write-Host "  ┌─────────────────────────────────────────────────────┐" -ForegroundColor Yellow
        Write-Host "  │   MODO MÓVIL ACTIVADO                               │" -ForegroundColor Yellow
        Write-Host "  │                                                     │" -ForegroundColor Yellow
        Write-Host "  │   Abre en el celular (misma WiFi):                  │" -ForegroundColor Yellow
        Write-Host "  │   http://${lanIp}:5173                   │" -ForegroundColor Green
        Write-Host "  │                                                     │" -ForegroundColor Yellow
        Write-Host "  │   Para instalar la PWA en Android:                  │" -ForegroundColor Yellow
        Write-Host "  │   chrome://flags → 'Insecure origins' → agrega:     │" -ForegroundColor Yellow
        Write-Host "  │   http://${lanIp}:5173                   │" -ForegroundColor Cyan
        Write-Host "  └─────────────────────────────────────────────────────┘" -ForegroundColor Yellow
        Write-Host ""
    }
    else {
        Start-Process -FilePath "npm.cmd" -WorkingDirectory $frontendPath -ArgumentList @(
            "run", "dev", "--", "--host", "localhost", "--port", "5173"
        ) -WindowStyle Normal | Out-Null

        Write-Step "Frontend iniciado en http://localhost:5173"
    }
}

Write-Step "Preparando entorno local PawTrack..."
Stop-PawTrackApiProcesses
Ensure-AzuriteRunning

if (-not $NoBackend) {
    Start-Backend
}
else {
    Write-Step "Backend omitido por bandera -NoBackend"
}

if (-not $NoFrontend) {
    Start-Frontend
}
else {
    Write-Step "Frontend omitido por bandera -NoFrontend"
}

Write-Step "Proceso finalizado."
