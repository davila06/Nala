# PawTrack CR — Datos de Despliegue Beta

> Generado originalmente: 2026-04-13 | Actualizado: 2026-08-19  
> Ambiente: **Beta**  
> Cuenta Azure: `davila06@gmail.com`

> ⚠️ **Nota agosto 2026:** La arquitectura evolucionó de Container Apps a App Service Linux (B3). Los nombres de recursos pueden diferir si se redesplegó. Usar `GUIA_DEPLOY_PASO_A_PASO.md` para despliegue fresco.

---

## Suscripción y Resource Group

| Campo             | Valor                                  |
| ----------------- | -------------------------------------- |
| Subscription Name | Azure subscription 1                   |
| Subscription ID   | `3832b5df-115d-4092-9fc8-2105d7b0af21` |
| Tenant ID         | `ab810006-3d9f-431f-aabd-52c4a26340af` |
| Resource Group    | `PawnTrackBeta`                        |
| Región            | `eastus`                               |
| Deployment Name   | `pawtrack-beta-deploy`                 |

---

## Recursos desplegados

| Recurso                  | Nombre en Azure             | Tipo                                       |
| ------------------------ | --------------------------- | ------------------------------------------ |
| Log Analytics Workspace  | `pawtrack-dev-logs`         | `Microsoft.OperationalInsights/workspaces` |
| Application Insights     | `pawtrack-dev-insights`     | `Microsoft.Insights/components`            |
| SQL Server               | `pawtrack-dev-sql`          | `Microsoft.Sql/servers`                    |
| SQL Database             | `pawtrack-dev-sql/pawtrack` | `Microsoft.Sql/servers/databases`          |
| Storage Account          | `pawtrackstoragdev`         | `Microsoft.Storage/storageAccounts`        |
| Blob container           | `pet-photos`                | público (lectura anónima)                  |
| Blob container           | `sighting-photos`           | público (lectura anónima)                  |
| Key Vault                | `pawtrack-kv-dev`           | `Microsoft.KeyVault/vaults`                |
| Container Registry (ACR) | `pawtrackacrdev`            | `Microsoft.ContainerRegistry/registries`   |
| Container Apps Env       | `pawtrack-dev-env`          | `Microsoft.App/managedEnvironments`        |
| Container App (API)      | `pawtrack-dev-api`          | `Microsoft.App/containerApps`              |
| Static Web App           | `pawtrack-dev-frontend`     | `Microsoft.Web/staticSites`                |
| Action Group (alertas)   | `pawtrack-dev-alerts-ag`    | `Microsoft.Insights/actionGroups`          |

---

## Comandos de deploy de cambios

### Backend (API) — push de nueva imagen

```powershell
# 1. Login al ACR con managed identity (desde pipeline) o con az acr login
az acr login --name pawtrackacrdev

# 2. Build y push de imagen
cd backend
docker build -f src/PawTrack.API/Dockerfile -t pawtrackacrdev.azurecr.io/pawtrack-api:latest .
docker push pawtrackacrdev.azurecr.io/pawtrack-api:latest

# 3. Actualizar Container App con la nueva imagen
az containerapp update \
  --name pawtrack-dev-api \
  --resource-group PawnTrackBeta \
  --image pawtrackacrdev.azurecr.io/pawtrack-api:<TAG>
```

### Frontend — deploy a Static Web App

```powershell
# Obtener deployment token
$token = az staticwebapp secrets list \
  --name pawtrack-dev-frontend \
  --resource-group PawnTrackBeta \
  --query properties.apiKey -o tsv

# Build del frontend
cd frontend
npm run build

# Deploy
npx @azure/static-web-apps-cli deploy dist \
  --deployment-token $token \
  --env production
```

### Infraestructura — re-deploy de Bicep

```powershell
# Siempre requiere pasar el password SQL como variable de entorno
$env:SQL_ADMIN_PASSWORD = "<password-desde-keyvault>"

az deployment group create \
  --resource-group PawnTrackBeta \
  --template-file infra/main.bicep \
  --parameters infra/parameters.beta.bicepparam \
  --parameters sqlAdminPassword=$env:SQL_ADMIN_PASSWORD \
  --name pawtrack-beta-deploy
```

---

## Variables de entorno requeridas por el Container App

Una vez que los secrets estén en Key Vault, actualizar el Container App:

```powershell
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

## Secrets requeridos en Key Vault (`pawtrack-kv-dev`)

| Secret Name                     | Descripción                                  |
| ------------------------------- | -------------------------------------------- |
| `appinsights-connection-string` | Connection string de Application Insights    |
| `sql-connection-string`         | Connection string completo de Azure SQL      |
| `storage-connection-string`     | Connection string de Storage Account         |
| `jwt-signing-key`               | Clave de firma JWT (mínimo 64 chars, base64) |

Ver: `docs/PENDIENTES_BETA.md` para instrucciones detalladas.

---

## SQL Admin

| Campo      | Valor                                                 |
| ---------- | ----------------------------------------------------- |
| SQL Server | `pawtrack-dev-sql.database.windows.net`               |
| Login      | `pawtrackadmin`                                       |
| Password   | _(guardado en Key Vault secret `sql-admin-password`)_ |

> ⚠️ El password SQL generado durante este despliegue es: `NcoD4~&^F%0B(y<+6gWhsYfq`  
> **Guárdalo en tu gestor de contraseñas y elimina esta línea después de leerla.**

---

## URLs del ambiente beta (post-deploy)

```powershell
# Obtener URL del Container App (API)
az containerapp show \
  --name pawtrack-dev-api \
  --resource-group PawnTrackBeta \
  --query properties.configuration.ingress.fqdn -o tsv

# Obtener URL del Static Web App (Frontend)
az staticwebapp show \
  --name pawtrack-dev-frontend \
  --resource-group PawnTrackBeta \
  --query defaultHostname -o tsv
```
