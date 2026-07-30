# PawTrack CR — Pendientes de Configuración Beta

> Última actualización: 2026-04-14  
> Ambiente: **PawnTrackBeta** (`davila06@gmail.com`)

---

## 🔴 CRÍTICOS — el app no funcionará sin estos

### P-01: Crear Dockerfile para la API

**Estado:** ✅ Completado — `backend/Dockerfile` (multi-stage .NET 9, EXPOSE 8080)  
**Impacto:** El Container App actualmente usa una imagen placeholder (`mcr.microsoft.com/dotnet/samples:aspnetapp`). La API real no está corriendo.

Crear `backend/src/PawTrack.API/Dockerfile`:

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

Luego hacer build y push:

```powershell
az acr login --name pawtrackacrdev
docker build -f backend/src/PawTrack.API/Dockerfile -t pawtrackacrdev.azurecr.io/pawtrack-api:v1.0.0 backend/
docker push pawtrackacrdev.azurecr.io/pawtrack-api:v1.0.0
az containerapp update --name pawtrack-dev-api --resource-group PawnTrackBeta --image pawtrackacrdev.azurecr.io/pawtrack-api:v1.0.0
```

---

### P-02: Poblar Key Vault con secrets reales

**Estado:** ✅ Completado — 5 secrets: `sql-connection-string`, `jwt-signing-key`, `storage-connection-string`, `appinsights-connection-string`, `sql-admin-password`  
**Impacto:** La API no puede conectarse a SQL ni Storage sin estas credenciales.

```powershell
$KV = "pawtrack-kv-dev"
$RG = "PawnTrackBeta"
$SQL_SERVER = "pawtrack-dev-sql.database.windows.net"
$STORAGE = "pawtrackstoragdev"

# 1. AppInsights connection string
$aiConnStr = az monitor app-insights component show `
  --app pawtrack-dev-insights --resource-group $RG `
  --query connectionString -o tsv
az keyvault secret set --vault-name $KV --name "appinsights-connection-string" --value $aiConnStr

# 2. SQL connection string
$sqlConnStr = "Server=tcp:$SQL_SERVER,1433;Database=pawtrack;User ID=pawtrackadmin;Password=NcoD4~&^F%0B(y<+6gWhsYfq;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
az keyvault secret set --vault-name $KV --name "sql-connection-string" --value $sqlConnStr

# 3. Storage connection string
$storageKey = az storage account keys list --account-name $STORAGE --resource-group $RG --query [0].value -o tsv
$storageConnStr = "DefaultEndpointsProtocol=https;AccountName=$STORAGE;AccountKey=$storageKey;EndpointSuffix=core.windows.net"
az keyvault secret set --vault-name $KV --name "storage-connection-string" --value $storageConnStr

# 4. JWT signing key (genera uno nuevo seguro)
$jwtKey = [Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Maximum 256 } | ForEach-Object { [byte]$_ }))
az keyvault secret set --vault-name $KV --name "jwt-signing-key" --value $jwtKey

# 5. SQL admin password (para referencia futura)
az keyvault secret set --vault-name $KV --name "sql-admin-password" --value "NcoD4~&^F%0B(y<+6gWhsYfq"
```

---

### P-03: Configurar env vars en Container App con referencias a Key Vault

**Estado:** ✅ Completado — secrets KV + env vars (`ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Azure__Storage__ConnectionString`, `ApplicationInsights__ConnectionString`, `ASPNETCORE_URLS`, `Cors__AllowedOrigins__0`)

```powershell
# Agregar secrets con referencia a Key Vault al Container App
az containerapp secret set \
  --name pawtrack-dev-api \
  --resource-group PawnTrackBeta \
  --secrets \
    "appinsights-connection-string=keyvaultref:https://pawtrack-kv-dev.vault.azure.net/secrets/appinsights-connection-string,identityref:system" \
    "sql-connection-string=keyvaultref:https://pawtrack-kv-dev.vault.azure.net/secrets/sql-connection-string,identityref:system" \
    "storage-connection-string=keyvaultref:https://pawtrack-kv-dev.vault.azure.net/secrets/storage-connection-string,identityref:system" \
    "jwt-signing-key=keyvaultref:https://pawtrack-kv-dev.vault.azure.net/secrets/jwt-signing-key,identityref:system"

# Actualizar env vars para que usen los secrets
az containerapp update \
  --name pawtrack-dev-api \
  --resource-group PawnTrackBeta \
  --set-env-vars \
    "APPINSIGHTS_CONNECTIONSTRING=secretref:appinsights-connection-string" \
    "ConnectionStrings__DefaultConnection=secretref:sql-connection-string" \
    "Azure__Storage__ConnectionString=secretref:storage-connection-string" \
    "Jwt__Key=secretref:jwt-signing-key"
```

---

### P-04: Ejecutar migraciones de EF Core en la base de datos

**Estado:** ✅ Completado — todas las migraciones aplicadas exitosamente en Azure SQL  
**Impacto:** La base de datos está vacía; sin schema el app fallará en cualquier operación.

```powershell
# Desde el directorio backend/, con conexión al SQL de Azure:
cd backend/
$env:ConnectionStrings__DefaultConnection = "<sql-connection-string-del-keyvault>"
dotnet ef database update --project src/PawTrack.Infrastructure --startup-project src/PawTrack.API
```

> Alternativa: Agregar un migration runner al startup del Container App (ver `GUIA_DEPLOY_PASO_A_PASO.md`).

---

### P-05: Actualizar `frontendUrl` con dominio real del Static Web App

**Estado:** ✅ Completado — CORS configurado con `https://green-mushroom-0156b8c0f.7.azurestaticapps.net`  
**Impacto:** CORS rechazará peticiones del frontend si el dominio no coincide.

```powershell
# Obtener URL real
$swaDomain = az staticwebapp show \
  --name pawtrack-dev-frontend \
  --resource-group PawnTrackBeta \
  --query defaultHostname -o tsv

# Actualizar el parámetro en infra/parameters.beta.bicepparam
# Cambiar: param frontendUrl = 'https://pawtrack.azurestaticapps.net'
# Por:     param frontendUrl = "https://$swaDomain"

# Re-deploy de Bicep
az deployment group create \
  --resource-group PawnTrackBeta \
  --template-file infra/main.bicep \
  --parameters infra/parameters.beta.bicepparam \
  --parameters sqlAdminPassword=(az keyvault secret show --vault-name pawtrack-kv-dev --name sql-admin-password --query value -o tsv)
```

---

## 🟡 IMPORTANTES — para una beta funcional

### P-06: Configurar firewall de Azure SQL para acceso desde Container Apps

**Estado:** ✅ Completado — reglas `AllowAzureServices` y `LocalDev` aplicadas  
**Impacto:** La API no puede conectarse a SQL si el firewall lo bloquea.

```powershell
# Permitir acceso desde servicios Azure (opción más simple para beta)
az sql server firewall-rule create \
  --resource-group PawnTrackBeta \
  --server pawtrack-dev-sql \
  --name "AllowAzureServices" \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

---

### P-07: Subir primer build del Frontend al Static Web App

**Estado:** ✅ Completado — frontend desplegado con `VITE_API_URL` apuntando al Container App

```powershell
$token = az staticwebapp secrets list \
  --name pawtrack-dev-frontend \
  --resource-group PawnTrackBeta \
  --query properties.apiKey -o tsv

cd frontend/
npm install
npm run build

npx @azure/static-web-apps-cli deploy dist \
  --deployment-token $token \
  --app-location "." \
  --output-location "dist"
```

---

### P-08: Solicitar aumento de cuota de App Service (para producción futura)

**Estado:** ⛔ Pendiente  
**Nota:** Esta suscripción tiene cuota 0 para App Service (Basic/Standard/Free). Para producción se necesitará migrar a App Service o solicitar cuota.

Ir a: `Portal Azure > Subscriptions > Azure subscription 1 > Usage + quotas > Microsoft.Web > Filter: eastus`  
Solicitar al menos 1 vCore Basic o Standard.

---

### P-09: Agregar regla DNS / dominio personalizado al Container App (opcional beta)

**Estado:** ⛔ Pendiente (opcional para beta, requerido para producción)

Comprar dominio en Route53/Namecheap/GoDaddy → configurar CNAME apuntando al FQDN del Container App → agregar custom domain en Azure.

---

### P-10: Configurar bot de WhatsApp (Meta Cloud API)

**Estado:** ⛔ Pendiente — el backend está implementado, falta configuración en Meta y Container App.

**Pasos:**

1. Crear app en [Meta for Developers](https://developers.facebook.com/) → agregar producto WhatsApp Business.
2. Obtener `Phone Number ID`, `WhatsApp Business Account ID`, y generar un `Permanent Token`.
3. Registrar el webhook en Meta apuntando a `https://<container-app-fqdn>/api/whatsapp/webhook`.
4. Agregar los siguientes secrets al Container App:

```powershell
$KV = "pawtrack-kv-dev"
$RG = "PawnTrackBeta"
$APP = "pawtrack-dev-api"

# Guardar en Key Vault
az keyvault secret set --vault-name $KV --name "whatsapp-bearer-token" --value "<PERMANENT_TOKEN_DE_META>"
az keyvault secret set --vault-name $KV --name "whatsapp-verify-token" --value "<TOKEN_ALEATORIO_QUE_TU_DEFINES>"

# Agregar referencias al Container App
az containerapp secret set \
  --name $APP --resource-group $RG \
  --secrets \
    "whatsapp-bearer-token=keyvaultref:https://$KV.vault.azure.net/secrets/whatsapp-bearer-token,identityref:system" \
    "whatsapp-verify-token=keyvaultref:https://$KV.vault.azure.net/secrets/whatsapp-verify-token,identityref:system"

az containerapp update \
  --name $APP --resource-group $RG \
  --set-env-vars \
    "WhatsApp__BearerToken=secretref:whatsapp-bearer-token" \
    "WhatsApp__VerifyToken=secretref:whatsapp-verify-token" \
    "WhatsApp__PhoneNumberId=<PHONE_NUMBER_ID_DE_META>"
```

5. Verificar que el webhook responde correctamente con el `hub.challenge` en Meta Developer Console.

---

### P-11: Configurar GitHub Secrets para CI/CD

**Estado:** ⛔ Pendiente — los workflows existen pero los secrets no están en el repo.

Ir a `GitHub repo → Settings → Secrets and variables → Actions` y agregar:

| Secret                            | Valor                                                     |
| --------------------------------- | --------------------------------------------------------- |
| `AZURE_CLIENT_ID`                 | App registration Client ID (Workload Identity Federation) |
| `AZURE_TENANT_ID`                 | Azure AD Tenant ID                                        |
| `AZURE_SUBSCRIPTION_ID`           | Subscription ID                                           |
| `ACR_NAME`                        | `pawtrackacrdev`                                          |
| `CONTAINER_APP_NAME`              | `pawtrack-dev-api`                                        |
| `CONTAINER_APP_FQDN`              | FQDN del Container App (sin `https://`)                   |
| `AZURE_RESOURCE_GROUP`            | `PawnTrackBeta`                                           |
| `SQL_CONNECTION_STRING`           | Connection string completo de Azure SQL                   |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Token del Static Web App                                  |
| `VITE_API_URL`                    | `https://<container-app-fqdn>`                            |

---

## 🟢 LISTOS

- [x] Resource Group `PawnTrackBeta` creado
- [x] Log Analytics Workspace
- [x] Application Insights
- [x] Azure SQL Server + Database (serverless)
- [x] Storage Account + containers `pet-photos` y `sighting-photos`
- [x] Key Vault con RBAC habilitado + 5 secrets
- [x] Container Registry (ACR Basic)
- [x] Container Apps Environment
- [x] Container App con Managed Identity + AcrPull
- [x] RBAC: Container App → Key Vault (Secrets User)
- [x] RBAC: Container App → ACR (AcrPull)
- [x] Static Web App
- [x] Azure Monitor Action Group + Alertas
- [x] Availability Test (Application Insights)
- [x] **Dockerfile** (`backend/Dockerfile`) — multi-stage .NET 9
- [x] **Imagen API** `pawtrackacrdev.azurecr.io/pawtrack-api:v1.0.0-beta` — pushed
- [x] **EF Core migrations** — todas aplicadas en `pawtrack-dev-sql.database.windows.net`
- [x] **Container App env vars** — KV secrets + Jwt\_\_Issuer/Audience + ASPNETCORE_URLS + CORS
- [x] **Container App image** — `pawtrackacrdev.azurecr.io/pawtrack-api:v1.0.0-beta` (Running)
- [x] **Frontend** desplegado en `https://green-mushroom-0156b8c0f.7.azurestaticapps.net`
- [x] **API health** respondiendo `{"status":"Healthy"}`
