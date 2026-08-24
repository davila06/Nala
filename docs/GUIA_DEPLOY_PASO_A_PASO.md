# PawTrack CR — Guía de Deploy Paso a Paso

> Ambiente: **PawnTrackBeta** (`PawnTrackBeta` resource group)  
> Cuenta: `davila06@gmail.com` | Suscripción: `Azure subscription 1`  
> **Última actualización: 2026-08-24** — incluye migraciones de adopciones, outbox, audit log y breed references

---

## Prerrequisitos

Antes de empezar, asegúrate de tener instalado:

| Herramienta    | Versión mínima | Verificar                                  |
| -------------- | -------------- | ------------------------------------------ |
| Azure CLI      | 2.60+          | `az --version`                             |
| Docker Desktop | 24+            | `docker --version`                         |
| .NET SDK       | 9.0            | `dotnet --version`                         |
| Node.js        | 20 LTS         | `node --version`                           |
| Azure SWA CLI  | latest         | `npx @azure/static-web-apps-cli --version` |

```powershell
# Login inicial
az login
az account set --subscription "3832b5df-115d-4092-9fc8-2105d7b0af21"
```

---

## PASO 1 — Crear Dockerfile para la API

**Ubicación:** `backend/src/PawTrack.API/Dockerfile`

Crea el archivo con este contenido:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/PawTrack.API/PawTrack.API.csproj", "src/PawTrack.API/"]
COPY ["src/PawTrack.Application/PawTrack.Application.csproj", "src/PawTrack.Application/"]
COPY ["src/PawTrack.Domain/PawTrack.Domain.csproj", "src/PawTrack.Domain/"]
COPY ["src/PawTrack.Infrastructure/PawTrack.Infrastructure.csproj", "src/PawTrack.Infrastructure/"]
RUN dotnet restore "src/PawTrack.API/PawTrack.API.csproj"
COPY . .
WORKDIR "/src/src/PawTrack.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "PawTrack.API.dll"]
```

Verifica que compila localmente:

```powershell
cd backend/
docker build -f src/PawTrack.API/Dockerfile -t pawtrack-api:local .
docker run -p 8080:8080 --rm pawtrack-api:local
# Abre http://localhost:8080/health → debe responder 200
```

---

## PASO 2 — Configurar SQL Firewall

```powershell
az sql server firewall-rule create `
  --resource-group PawnTrackBeta `
  --server pawtrack-dev-sql `
  --name "AllowAzureServices" `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0

# Opcional: agregar tu IP local para correr migraciones desde tu máquina
$myIp = (Invoke-RestMethod https://api.ipify.org)
az sql server firewall-rule create `
  --resource-group PawnTrackBeta `
  --server pawtrack-dev-sql `
  --name "DevMachine" `
  --start-ip-address $myIp `
  --end-ip-address $myIp
```

---

## PASO 3 — Poblar Key Vault con secrets

```powershell
$KV  = "pawtrack-kv-dev"
$RG  = "PawnTrackBeta"
$SQL = "pawtrack-dev-sql"
$STO = "pawtrackstoragdev"
$DB  = "pawtrack"

# 3.1 — Application Insights connection string
$aiConnStr = az monitor app-insights component show `
  --app pawtrack-dev-insights `
  --resource-group $RG `
  --query connectionString -o tsv
az keyvault secret set --vault-name $KV --name "appinsights-connection-string" --value $aiConnStr
Write-Host "✔ appinsights-connection-string guardado"

# 3.2 — SQL connection string
$sqlPwd = "NcoD4~&^F%0B(y<+6gWhsYfq"   # <- reemplaza si usaste otro password
$sqlConnStr = "Server=tcp:$SQL.database.windows.net,1433;Initial Catalog=$DB;Persist Security Info=False;User ID=pawtrackadmin;Password=$sqlPwd;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
az keyvault secret set --vault-name $KV --name "sql-connection-string" --value $sqlConnStr
Write-Host "✔ sql-connection-string guardado"

# 3.3 — Storage connection string
$storageKey = az storage account keys list `
  --account-name $STO `
  --resource-group $RG `
  --query "[0].value" -o tsv
$storageConnStr = "DefaultEndpointsProtocol=https;AccountName=$STO;AccountKey=$storageKey;EndpointSuffix=core.windows.net"
az keyvault secret set --vault-name $KV --name "storage-connection-string" --value $storageConnStr
Write-Host "✔ storage-connection-string guardado"

# 3.4 — JWT signing key (genera uno nuevo, 64 bytes en Base64)
$jwtKey = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
az keyvault secret set --vault-name $KV --name "jwt-signing-key" --value $jwtKey
Write-Host "✔ jwt-signing-key guardado"

# 3.5 — SQL admin password (para referencia)
az keyvault secret set --vault-name $KV --name "sql-admin-password" --value $sqlPwd
# 3.5 — Bot phone hash secret (mínimo 32 chars)
$botHashSecret = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
az keyvault secret set --vault-name $KV --name "bot-phone-hash-secret" --value $botHashSecret
Write-Host "✔ bot-phone-hash-secret guardado"

# 3.6 — SendGrid API Key (obtener desde sendgrid.com → Settings → API Keys)
# az keyvault secret set --vault-name $KV --name "sendgrid-api-key" --value "SG.xxxxx"
Write-Host "⚠ Cargar sendgrid-api-key manualmente desde el dashboard de SendGrid"

# 3.7 — WhatsApp (obtener desde Meta for Developers)
# az keyvault secret set --vault-name $KV --name "whatsapp-phone-number-id" --value "XXXX"
# az keyvault secret set --vault-name $KV --name "whatsapp-access-token" --value "EAAxxxx"
# az keyvault secret set --vault-name $KV --name "whatsapp-verify-token" --value (openssl rand -hex 16)
Write-Host "⚠ Cargar secretos de WhatsApp manualmente (ver docs/operacional.md §7)"
```

---

## PASO 3b — Crear contenedor Blob Storage para adopciones

````powershell
# El Bicep crea pet-photos, sighting-photos, found-pet-photos, lost-pet-photos, whatsapp-avatars.
# El módulo de adopciones necesita un contenedor adicional:
az storage container create `
  --name "adoption-photos" `
  --account-name $STO `
  --public-access off `
  --auth-mode login
Write-Host "✔ Contenedor adoption-photos creado"

# Verificar todos los contenedores
az storage container list `
  --account-name $STO `
  --auth-mode login `
  --query "[].name" -o table

---

## PASO 4 — Conectar Container App a Key Vault

```powershell
# Agregar referencias a Key Vault como secrets del Container App
az containerapp secret set `
  --name pawtrack-dev-api `
  --resource-group PawnTrackBeta `
  --secrets `
    "appinsights-connection-string=keyvaultref:https://pawtrack-kv-dev.vault.azure.net/secrets/appinsights-connection-string,identityref:system" `
    "sql-connection-string=keyvaultref:https://pawtrack-kv-dev.vault.azure.net/secrets/sql-connection-string,identityref:system" `
    "storage-connection-string=keyvaultref:https://pawtrack-kv-dev.vault.azure.net/secrets/storage-connection-string,identityref:system" `
    "jwt-signing-key=keyvaultref:https://pawtrack-kv-dev.vault.azure.net/secrets/jwt-signing-key,identityref:system"

# Mapear secrets a variables de entorno
az containerapp update `
  --name pawtrack-dev-api `
  --resource-group PawnTrackBeta `
  --set-env-vars `
    "APPINSIGHTS_CONNECTIONSTRING=secretref:appinsights-connection-string" `
    "ConnectionStrings__DefaultConnection=secretref:sql-connection-string" `
    "Azure__Storage__ConnectionString=secretref:storage-connection-string" `
    "Jwt__Key=secretref:jwt-signing-key"

Write-Host "✔ Variables de entorno configuradas"
````

---

## PASO 5 — Ejecutar migraciones de EF Core

El proyecto tiene **56 migraciones** en total. Las siguientes son las más recientes y deben estar aplicadas antes del primer deploy:

| Migración                              | Tablas creadas                                        | Cuándo     |
| -------------------------------------- | ----------------------------------------------------- | ---------- |
| `AddPetStores`                         | Stores, StoreProducts, StoreOrders                    | 2026-08-19 |
| `AddRevokedTokens`                     | RevokedTokens                                         | 2026-08-19 |
| `AddBillboards`                        | Billboards                                            | 2026-08-19 |
| `AddAdoptionsModule`                   | AdoptableAnimals, AdoptionApplications, AdoptionFairs | 2026-08-21 |
| `AddWhatsAppIdempotencyTable`          | WhatsAppProcessedMessages (unique idx en Wamid)       | 2026-08-24 |
| `AddAuditLog`                          | AuditLog                                              | 2026-08-24 |
| `AddOutboxAndFosterJsonSpecies`        | OutboxMessages; rename AcceptedSpeciesCsv→JSON        | 2026-08-24 |
| `AddBreedReferenceAndCursorPagination` | BreedReferences                                       | 2026-08-24 |

```powershell
# Opción A: Correr desde tu máquina local (requiere IP en firewall SQL del PASO 2)
cd backend/
$env:ConnectionStrings__DefaultConnection = (az keyvault secret show `
  --vault-name pawtrack-kv-dev --name sql-connection-string --query value -o tsv)
dotnet ef database update `
  --project src/PawTrack.Infrastructure `
  --startup-project src/PawTrack.API

# Verificar que todas las migraciones están aplicadas
dotnet ef migrations list `
  --project src/PawTrack.Infrastructure `
  --startup-project src/PawTrack.API
# Todas deben aparecer con [applied]

# Opción B: Correr via az sql (scripts de migración ya generados)
# dotnet ef migrations script --output migrations.sql --project src/PawTrack.Infrastructure --startup-project src/PawTrack.API
# az sql db execute --server pawtrack-dev-sql --database pawtrack --file migrations.sql

Write-Host "✔ Migraciones aplicadas"
```

---

## PASO 6 — Build y push de imagen al ACR

```powershell
$ACR = "pawtrackacrdev"
$TAG = "v1.0.0"

# Login al ACR
az acr login --name $ACR

# Build de la imagen
cd backend/
docker build -f src/PawTrack.API/Dockerfile -t "$ACR.azurecr.io/pawtrack-api:$TAG" .

# Push al ACR
docker push "$ACR.azurecr.io/pawtrack-api:$TAG"

Write-Host "✔ Imagen subida a $ACR.azurecr.io/pawtrack-api:$TAG"
```

---

## PASO 7 — Actualizar Container App con imagen real

```powershell
$TAG = "v1.0.0"  # mismo tag del paso anterior

az containerapp update `
  --name pawtrack-dev-api `
  --resource-group PawnTrackBeta `
  --image "pawtrackacrdev.azurecr.io/pawtrack-api:$TAG"

# Verificar que el revision esté Running
az containerapp revision list `
  --name pawtrack-dev-api `
  --resource-group PawnTrackBeta `
  --output table

Write-Host "✔ Container App actualizado con imagen real"
```

---

## PASO 8 — Deploy del Frontend

```powershell
# Obtener token de Static Web App
$token = az staticwebapp secrets list `
  --name pawtrack-dev-frontend `
  --resource-group PawnTrackBeta `
  --query properties.apiKey -o tsv

# Build del frontend
cd frontend/
npm install
npm run build

# Deploy al Static Web App
npx @azure/static-web-apps-cli deploy dist `
  --deployment-token $token `
  --output-location "dist"

Write-Host "✔ Frontend desplegado"
```

---

## PASO 9 — Actualizar CORS con URL real del frontend

```powershell
# Obtener URL del Static Web App
$swaDomain = az staticwebapp show `
  --name pawtrack-dev-frontend `
  --resource-group PawnTrackBeta `
  --query defaultHostname -o tsv

Write-Host "URL del frontend: https://$swaDomain"

# Editar infra/parameters.beta.bicepparam:
# param frontendUrl = 'https://<swaDomain>'

# Re-deploy del Bicep
$sqlPwd = az keyvault secret show --vault-name pawtrack-kv-dev --name sql-admin-password --query value -o tsv
az deployment group create `
  --resource-group PawnTrackBeta `
  --template-file infra/main.bicep `
  --parameters infra/parameters.beta.bicepparam `
  --parameters sqlAdminPassword=$sqlPwd `
  --name pawtrack-beta-deploy-cors
```

---

## PASO 10 — Verificación final

```powershell
# URL del API
$apiUrl = "https://$(az containerapp show --name pawtrack-dev-api --resource-group PawnTrackBeta --query properties.configuration.ingress.fqdn -o tsv)"

# Health check
Invoke-RestMethod "$apiUrl/health"

# Swagger (solo en Staging)
Start-Process "$apiUrl/swagger"

# Frontend
$swaUrl = "https://$(az staticwebapp show --name pawtrack-dev-frontend --resource-group PawnTrackBeta --query defaultHostname -o tsv)"
Start-Process $swaUrl

Write-Host "API: $apiUrl"
Write-Host "Frontend: $swaUrl"
```

---

## Flujo de deploy de cambios (día a día)

```
Cambio en código
      │
      ├─ Backend? ──► PASO 6 + PASO 7  (docker build + push + containerapp update)
      │               Si hay nueva migración: correr PASO 5 primero
      │
      ├─ Frontend? ─► PASO 8           (npm build + swa deploy)
      │               Variables de entorno: VITE_API_URL, VITE_VAPID_PUBLIC_KEY
      │
      └─ Infra? ────► Bicep deploy     (az deployment group create)
```

---

## Troubleshooting rápido

| Síntoma                        | Comando diagnóstico                                                                                                                                                          |
| ------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Container App no arranca       | `az containerapp logs show --name pawtrack-dev-api --resource-group PawnTrackBeta --follow`                                                                                  |
| Error 401 / 403 en API         | Verificar Key Vault secrets y RBAC                                                                                                                                           |
| SQL no conecta                 | Verificar firewall SQL (PASO 2)                                                                                                                                              |
| Frontend muestra error CORS    | Verificar `frontendUrl` en Bicep y re-deploy (PASO 9)                                                                                                                        |
| Imagen no se descarga del ACR  | Verificar RBAC AcrPull en Container App identity                                                                                                                             |
| Fotos de adopción no suben     | Verificar que el contenedor `adoption-photos` existe (PASO 3b)                                                                                                               |
| Bot de WhatsApp no responde    | Verificar `whatsapp-verify-token` y que el webhook está activo en Meta                                                                                                       |
| Push notifications no llegan   | Verificar `VITE_VAPID_PUBLIC_KEY` en GitHub Secrets y que `vapid-private-key` está en Key Vault                                                                              |
| Migraciones pendientes en logs | Correr PASO 5 con las nuevas migraciones (AddAdoptionsModule, AddWhatsAppIdempotencyTable, AddAuditLog, AddOutboxAndFosterJsonSpecies, AddBreedReferenceAndCursorPagination) |
