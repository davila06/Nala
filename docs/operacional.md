# PawTrack CR — Guía Operacional para Ir a Producción

> **Versión:** 1.0 | **Fecha:** 2026-08-24  
> **Audiencia:** Operador de infraestructura / founder  
> **Pre-requisitos:** Azure CLI 2.60+, Docker 24+, .NET SDK 9, Node.js 20 LTS, acceso a la suscripción Azure

---

## Tabla de contenidos

1. [Estado actual](#1-estado-actual)
2. [GitHub Secrets — CI/CD](#2-github-secrets--cicd)
3. [Azure Key Vault — Secretos de aplicación](#3-azure-key-vault--secretos-de-aplicación)
4. [Dominio y DNS](#4-dominio-y-dns)
5. [Contenedores Azure Blob Storage faltantes](#5-contenedores-azure-blob-storage-faltantes)
6. [Migraciones EF Core en producción](#6-migraciones-ef-core-en-producción)
7. [WhatsApp — Meta Cloud API webhook](#7-whatsapp--meta-cloud-api-webhook)
8. [VAPID Keys para Push Notifications](#8-vapid-keys-para-push-notifications)
9. [Broadcast — Facebook y Telegram](#9-broadcast--facebook-y-telegram)
10. [Redis — Rate limiter distribuido](#10-redis--rate-limiter-distribuido)
11. [SendGrid — Email transaccional](#11-sendgrid--email-transaccional)
12. [Azure Computer Vision](#12-azure-computer-vision)
13. [Tractive GPS — OAuth2](#13-tractive-gps--oauth2)
14. [Verificación final antes de abrir el tráfico](#14-verificación-final-antes-de-abrir-el-tráfico)

---

## 1. Estado actual

### Recursos Azure ya desplegados (via Bicep `infra/main.bicep`)

| Recurso                  | Nombre                   | URL                                         |
| ------------------------ | ------------------------ | ------------------------------------------- |
| App Service (B3 Linux)   | `pawtrack-prod-api`      | `https://api.pawtrack.cr`                   |
| Static Web App           | `pawtrack-swa-prod`      | `https://pawtrack.azurestaticapps.net`      |
| Azure SQL Server         | `pawtrack-prod-sql`      | —                                           |
| Base de datos            | `pawtrack`               | —                                           |
| Key Vault                | `pawtrack-kv-prod`       | `https://pawtrack-kv-prod.vault.azure.net/` |
| Blob Storage             | `pawtrackstorprod`       | —                                           |
| Application Insights     | `pawtrack-prod-insights` | —                                           |
| Log Analytics            | `pawtrack-prod-logs`     | —                                           |
| ACR (Container Registry) | `pawtrackacrprod`        | `pawtrackacrprod.azurecr.io`                |

### Contenedores Blob ya creados por Bicep

- `pet-photos` ✅
- `sighting-photos` ✅
- `found-pet-photos` ✅
- `lost-pet-photos` ✅
- `whatsapp-avatars` ✅
- `adoption-photos` ❌ **Falta crear (ver sección 5)**

---

## 2. GitHub Secrets — CI/CD

Los workflows en `.github/workflows/` (backend.yml, frontend.yml, infra.yml, smoke-tests.yml) requieren los siguientes secrets configurados en **GitHub → Settings → Secrets and variables → Actions → Repository secrets**.

### Secrets requeridos

| Secret                            | Descripción                                                       | Cómo obtenerlo                                    |
| --------------------------------- | ----------------------------------------------------------------- | ------------------------------------------------- |
| `AZURE_CLIENT_ID`                 | Client ID de la App Registration con Workload Identity Federation | `az ad app list --display-name "pawtrack-github"` |
| `AZURE_TENANT_ID`                 | Tenant ID de Azure AD                                             | `az account show --query tenantId -o tsv`         |
| `AZURE_SUBSCRIPTION_ID`           | ID de la suscripción                                              | `az account show --query id -o tsv`               |
| `AZURE_RESOURCE_GROUP`            | Nombre del resource group                                         | `pawtrack-prod-rg`                                |
| `ACR_NAME`                        | Nombre del Azure Container Registry (sin `.azurecr.io`)           | `pawtrackacrprod`                                 |
| `CONTAINER_APP_NAME`              | Nombre del Container App / App Service                            | `pawtrack-prod-api`                               |
| `CONTAINER_APP_FQDN`              | FQDN del API sin `https://`                                       | `api.pawtrack.cr`                                 |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Deployment token del SWA                                          | Ver abajo                                         |
| `SQL_CONNECTION_STRING`           | Connection string completo de Azure SQL                           | Ver abajo                                         |
| `SQL_ADMIN_PASSWORD`              | Password del admin SQL                                            | El que usaste al correr el Bicep                  |
| `ALERT_EMAIL_ADDRESS`             | Email para alertas de Azure Monitor                               | `ops@pawtrack.cr`                                 |
| `VITE_API_URL`                    | URL del API usada por el frontend en build                        | `https://api.pawtrack.cr`                         |
| `VITE_VAPID_PUBLIC_KEY`           | Clave VAPID pública para push notifications                       | Generada en sección 8                             |
| `VITE_COLLAR_WHATSAPP_NUMBER`     | Número de WhatsApp del soporte GPS                                | Ej: `+50600000000`                                |
| `VITE_SINPE_PHONE`                | Número SINPE Móvil para pagos                                     | Número de la cuenta                               |
| `SMOKE_FRONTEND_URL`              | URL del frontend para smoke tests                                 | `https://pawtrack.cr`                             |

### Comandos para obtener los secrets

```powershell
# Login
az login
az account set --subscription "3832b5df-115d-4092-9fc8-2105d7b0af21"

# AZURE_TENANT_ID
az account show --query tenantId -o tsv

# AZURE_SUBSCRIPTION_ID
az account show --query id -o tsv

# AZURE_STATIC_WEB_APPS_API_TOKEN
az staticwebapp secrets list `
  --name pawtrack-swa-prod `
  --resource-group pawtrack-prod-rg `
  --query "properties.apiKey" -o tsv

# SQL_CONNECTION_STRING (construirlo desde los outputs del Bicep)
$sqlFqdn = az sql server show `
  --name pawtrack-prod-sql `
  --resource-group pawtrack-prod-rg `
  --query fullyQualifiedDomainName -o tsv
"Server=tcp:$sqlFqdn,1433;Initial Catalog=pawtrack;Persist Security Info=False;User ID=pawtrackadmin;Password=TU_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

### Setup de Workload Identity Federation (reemplaza contraseña de service principal)

```powershell
# 1. Crear App Registration
az ad app create --display-name "pawtrack-github-actions"
$appId = az ad app list --display-name "pawtrack-github-actions" --query "[0].appId" -o tsv
$objectId = az ad app list --display-name "pawtrack-github-actions" --query "[0].id" -o tsv

# 2. Crear Service Principal
az ad sp create --id $appId

# 3. Federated credential para el repo GitHub
az ad app federated-credential create --id $objectId --parameters '{
  "name": "pawtrack-github-main",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:TU_USUARIO/Nala:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'

# 4. Asignar roles en el resource group
az role assignment create `
  --assignee $appId `
  --role "Contributor" `
  --scope "/subscriptions/3832b5df-115d-4092-9fc8-2105d7b0af21/resourceGroups/pawtrack-prod-rg"

az role assignment create `
  --assignee $appId `
  --role "AcrPush" `
  --scope "/subscriptions/3832b5df-115d-4092-9fc8-2105d7b0af21/resourceGroups/pawtrack-prod-rg"
```

---

## 3. Azure Key Vault — Secretos de aplicación

Todos los secretos del backend se inyectan vía Key Vault references en `appsettings.json` con el patrón `@Microsoft.KeyVault(VaultName=pawtrack-kv-prod;SecretName=xxx)`. El App Service necesita tener una **Managed Identity** con acceso `Key Vault Secrets User`.

### Habilitar Managed Identity y acceso al Key Vault

```powershell
# Habilitar system-assigned identity en el App Service
az webapp identity assign `
  --name pawtrack-prod-api `
  --resource-group pawtrack-prod-rg

# Obtener el Principal ID de la identity
$principalId = az webapp identity show `
  --name pawtrack-prod-api `
  --resource-group pawtrack-prod-rg `
  --query principalId -o tsv

# Asignar "Key Vault Secrets User" a la identity
az keyvault set-policy `
  --name pawtrack-kv-prod `
  --object-id $principalId `
  --secret-permissions get list
```

### Secretos a cargar en Key Vault

Usar `az keyvault secret set --vault-name pawtrack-kv-prod --name NOMBRE --value "VALOR"` para cada uno:

| Secret Name en Key Vault        | Descripción                                                         | Cómo obtener el valor                                                                                        |
| ------------------------------- | ------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------ |
| `sql-connection-string`         | Connection string completo de Azure SQL                             | Ver sección 2                                                                                                |
| `jwt-signing-key`               | Clave de firma JWT (mínimo 32 chars, aleatoria)                     | `openssl rand -base64 48`                                                                                    |
| `storage-connection-string`     | Connection string del Storage Account                               | `az storage account show-connection-string --name pawtrackstorprod --resource-group pawtrack-prod-rg -o tsv` |
| `appinsights-connection-string` | InstrumentationKey del App Insights                                 | Portal Azure → Application Insights → Connection String                                                      |
| `sendgrid-api-key`              | API Key de SendGrid                                                 | SendGrid dashboard → Settings → API Keys                                                                     |
| `bot-phone-hash-secret`         | Secret HMAC-SHA256 para hashear teléfonos del bot (mínimo 32 chars) | `openssl rand -base64 48`                                                                                    |
| `whatsapp-phone-number-id`      | Phone Number ID de Meta Cloud API                                   | Meta for Developers → WhatsApp → Getting Started                                                             |
| `whatsapp-access-token`         | Access Token permanente de Meta                                     | Meta for Developers → System Users                                                                           |
| `whatsapp-verify-token`         | Token de verificación del webhook (cualquier string aleatorio)      | `openssl rand -hex 16`                                                                                       |
| `whatsapp-app-secret`           | App Secret de la aplicación Meta                                    | Meta for Developers → Settings → Basic                                                                       |
| `vision-endpoint`               | Endpoint de Azure Computer Vision                                   | Portal Azure → Cognitive Services → Overview                                                                 |
| `vision-key`                    | Key de Azure Computer Vision                                        | Portal Azure → Cognitive Services → Keys                                                                     |
| `vapid-private-key`             | Clave privada VAPID para push (generada en sección 8)               | Ver sección 8                                                                                                |
| `telegram-bot-token`            | Token del bot de Telegram                                           | @BotFather en Telegram → /newbot                                                                             |
| `facebook-page-access-token`    | Page Access Token de Facebook                                       | Meta for Developers → Graph API Explorer                                                                     |
| `facebook-page-id`              | Page ID de la página de Facebook                                    | Configuración de la página → Acerca de → ID                                                                  |

### Cargar todos los secretos en batch

```powershell
$vault = "pawtrack-kv-prod"

# JWT Key (generar una clave aleatoria fuerte)
$jwtKey = [System.Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
az keyvault secret set --vault-name $vault --name "jwt-signing-key" --value $jwtKey

# Bot phone hash secret
$botSecret = [System.Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
az keyvault secret set --vault-name $vault --name "bot-phone-hash-secret" --value $botSecret

# Storage connection string
$storageConn = az storage account show-connection-string `
  --name pawtrackstorprod `
  --resource-group pawtrack-prod-rg `
  --query connectionString -o tsv
az keyvault secret set --vault-name $vault --name "storage-connection-string" --value $storageConn

# SQL connection string (completar TU_PASSWORD)
az keyvault secret set --vault-name $vault `
  --name "sql-connection-string" `
  --value "Server=tcp:pawtrack-prod-sql.database.windows.net,1433;Initial Catalog=pawtrack;User ID=pawtrackadmin;Password=TU_PASSWORD;Encrypt=True;"

# SendGrid (obtener desde dashboard de SendGrid)
az keyvault secret set --vault-name $vault --name "sendgrid-api-key" --value "SG.XXXXX"

# App Insights (obtener desde Portal Azure)
az keyvault secret set --vault-name $vault --name "appinsights-connection-string" --value "InstrumentationKey=XXXX..."
```

---

## 4. Dominio y DNS

### Configuración del dominio `pawtrack.cr`

```powershell
# Paso 1: Verificar que el dominio esté registrado en tu registrar
# Paso 2: Obtener el hostname del Static Web App
$swaHostname = az staticwebapp show `
  --name pawtrack-swa-prod `
  --resource-group pawtrack-prod-rg `
  --query "defaultHostname" -o tsv
Write-Host "SWA hostname: $swaHostname"
# Ejemplo: random-name-xyz.eastus.azurestaticapps.net

# Paso 3: En tu registrar (ej: NICCR, GoDaddy, Cloudflare):
# Agregar registro CNAME:
#   Nombre: www (o @)
#   Valor: random-name-xyz.eastus.azurestaticapps.net

# Paso 4: Agregar dominio customizado al SWA
az staticwebapp hostname set `
  --name pawtrack-swa-prod `
  --resource-group pawtrack-prod-rg `
  --hostname "pawtrack.cr"

# Paso 5: Para el API — configurar dominio en App Service
az webapp config hostname add `
  --webapp-name pawtrack-prod-api `
  --resource-group pawtrack-prod-rg `
  --hostname "api.pawtrack.cr"

# Paso 6: Certificado SSL gratuito (App Service Managed Certificate)
az webapp config ssl create `
  --resource-group pawtrack-prod-rg `
  --name pawtrack-prod-api `
  --hostname "api.pawtrack.cr"
```

### Registros DNS requeridos

| Tipo  | Nombre  | Valor                                 | TTL  |
| ----- | ------- | ------------------------------------- | ---- |
| CNAME | `www`   | `random-name.azurestaticapps.net`     | 3600 |
| CNAME | `api`   | `pawtrack-prod-api.azurewebsites.net` | 3600 |
| TXT   | `asuid` | (valor de verificación del SWA)       | 3600 |

> **Nota NICCR:** Costa Rica (.cr) tiene propagación de hasta 24 horas. Configurar DNS al menos 1 día antes del lanzamiento.

### Actualizar CORS después de configurar el dominio

```powershell
# Actualizar variable de entorno en el App Service
az webapp config appsettings set `
  --name pawtrack-prod-api `
  --resource-group pawtrack-prod-rg `
  --settings "Cors__AllowedOrigins__0=https://pawtrack.cr" `
             "Cors__AllowedOrigins__1=https://www.pawtrack.cr"
```

---

## 5. Contenedores Azure Blob Storage faltantes

El Bicep crea los contenedores de la app original, pero el módulo de adopciones requiere un contenedor adicional que **no está en el Bicep actual**.

### Crear el contenedor `adoption-photos`

```powershell
az storage container create `
  --name "adoption-photos" `
  --account-name pawtrackstorprod `
  --public-access off `
  --auth-mode login
```

> **Importante:** El acceso es `off` (privado) — las fotos se sirven a través de URLs de Blob Storage con SAS token o acceso anónimo habilitado solo a nivel de contenedor si se requiere acceso público. Verificar la política actual del storage account.

### Verificar todos los contenedores activos

```powershell
az storage container list `
  --account-name pawtrackstorprod `
  --auth-mode login `
  --query "[].name" -o table
```

Deben aparecer: `pet-photos`, `sighting-photos`, `found-pet-photos`, `lost-pet-photos`, `whatsapp-avatars`, `adoption-photos`, y opcionalmente `store-product-images`, `billboard-images`.

---

## 6. Migraciones EF Core en producción

### Migraciones pendientes de aplicar en Azure SQL

Las siguientes migraciones han sido generadas pero **solo están aplicadas en desarrollo local**:

| Migración                     | Tablas que crea                                             | Cuándo se generó     |
| ----------------------------- | ----------------------------------------------------------- | -------------------- |
| `AddAdoptionsModule`          | `AdoptableAnimals`, `AdoptionApplications`, `AdoptionFairs` | Sprint 1 adopciones  |
| `AddWhatsAppIdempotencyTable` | `WhatsAppProcessedMessages` (índice único en `Wamid`)       | Idempotency fix      |
| `AddAuditLog`                 | `AuditLog`                                                  | Audit log enterprise |

### Aplicar todas las migraciones pendientes

```powershell
# Opción A: Con .NET EF Tool y connection string directo
$connStr = az keyvault secret show `
  --vault-name pawtrack-kv-prod `
  --name sql-connection-string `
  --query value -o tsv

cd C:\Nala\backend
dotnet ef database update `
  --project src/PawTrack.Infrastructure `
  --startup-project src/PawTrack.API `
  --connection $connStr

# Opción B: Vía el pipeline de CI/CD (recomendado para producción)
# El workflow backend.yml incluye el paso de migración automático
# cuando se hace push a main después de un deploy exitoso.
```

### Verificar que las migraciones se aplicaron

```powershell
dotnet ef migrations list `
  --project src/PawTrack.Infrastructure `
  --startup-project src/PawTrack.API `
  --connection $connStr
# Todas deben aparecer con [applied]
```

---

## 7. WhatsApp — Meta Cloud API webhook

### Prerrequisitos

1. Cuenta de **Meta for Developers** en `developers.facebook.com`
2. Una **Meta Business Account** verificada
3. **WhatsApp Business Account** asociada
4. Número de teléfono configurado en WhatsApp Business

### Configurar el webhook

```powershell
# 1. Obtener el Verify Token (o generar uno nuevo)
$verifyToken = [System.Convert]::ToHexString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(16)).ToLower()
az keyvault secret set `
  --vault-name pawtrack-kv-prod `
  --name "whatsapp-verify-token" `
  --value $verifyToken

# 2. Actualizar la app del backend con el verify token
az webapp config appsettings set `
  --name pawtrack-prod-api `
  --resource-group pawtrack-prod-rg `
  --settings "WhatsApp__VerifyToken=@Microsoft.KeyVault(VaultName=pawtrack-kv-prod;SecretName=whatsapp-verify-token)"
```

En el portal de Meta for Developers:

1. Ir a **WhatsApp → Configuration → Webhook**
2. **Callback URL:** `https://api.pawtrack.cr/api/whatsapp/webhook`
3. **Verify Token:** el valor guardado en Key Vault
4. **Webhook Fields:** seleccionar `messages`
5. Hacer clic en **Verify and Save**

### Variables de entorno del App Service para WhatsApp

```powershell
az webapp config appsettings set `
  --name pawtrack-prod-api `
  --resource-group pawtrack-prod-rg `
  --settings `
    "WhatsApp__PhoneNumberId=@Microsoft.KeyVault(VaultName=pawtrack-kv-prod;SecretName=whatsapp-phone-number-id)" `
    "WhatsApp__AccessToken=@Microsoft.KeyVault(VaultName=pawtrack-kv-prod;SecretName=whatsapp-access-token)" `
    "WhatsApp__AppSecret=@Microsoft.KeyVault(VaultName=pawtrack-kv-prod;SecretName=whatsapp-app-secret)" `
    "WhatsApp__VerifyToken=@Microsoft.KeyVault(VaultName=pawtrack-kv-prod;SecretName=whatsapp-verify-token)"
```

### Verificar que el webhook funciona

```powershell
# El endpoint de verificación debe responder con el hub.challenge
curl "https://api.pawtrack.cr/api/whatsapp/webhook?hub.mode=subscribe&hub.verify_token=TU_VERIFY_TOKEN&hub.challenge=12345"
# Respuesta esperada: 12345
```

---

## 8. VAPID Keys para Push Notifications

Las push notifications web requieren un par de claves VAPID. La **clave pública** va en el frontend (build time), la **clave privada** en el backend (Key Vault).

### Generar y configurar las claves VAPID

```powershell
# 1. Instalar web-push si no está instalado
npm install -g web-push

# 2. Generar el par de claves
npx web-push generate-vapid-keys
# Output:
#   Public Key: BxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxUm4=
#   Private Key: Yxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx0=

# 3. Guardar la clave privada en Key Vault
az keyvault secret set `
  --vault-name pawtrack-kv-prod `
  --name "vapid-private-key" `
  --value "TU_CLAVE_PRIVADA"

# 4. La clave pública va como GitHub Secret (VITE_VAPID_PUBLIC_KEY)
# y se inyecta en el build de Vite automáticamente.

# 5. Configurar el App Service con la referencia al Key Vault
az webapp config appsettings set `
  --name pawtrack-prod-api `
  --resource-group pawtrack-prod-rg `
  --settings "Vapid__PrivateKey=@Microsoft.KeyVault(VaultName=pawtrack-kv-prod;SecretName=vapid-private-key)"

# 6. También necesitas configurar el email de contacto VAPID
az webapp config appsettings set `
  --name pawtrack-prod-api `
  --resource-group pawtrack-prod-rg `
  --settings "Vapid__Subject=mailto:ops@pawtrack.cr"
```

---

## 9. Broadcast — Facebook y Telegram

El código de broadcast para Facebook y Telegram está implementado. Solo requieren credenciales en Key Vault.

### Facebook Page Broadcasting

```powershell
# Prerrequisitos:
# 1. Página de Facebook activa para PawTrack CR
# 2. App de Meta con permiso pages_manage_posts
# 3. Page Access Token (no expira si es System User Token)

# En Graph API Explorer (developers.facebook.com):
# GET /me/accounts → obtener page_id y access_token

az keyvault secret set `
  --vault-name pawtrack-kv-prod `
  --name "facebook-page-access-token" `
  --value "EAAxxxxx"  # Token de la página

az keyvault secret set `
  --vault-name pawtrack-kv-prod `
  --name "facebook-page-id" `
  --value "123456789012345"  # ID numérico de la página
```

### Telegram Channel Broadcasting

```powershell
# Prerrequisitos:
# 1. Bot de Telegram creado vía @BotFather (/newbot)
# 2. Canal de Telegram donde el bot es administrador
# 3. Chat ID del canal (empieza con -100)

# Obtener el Chat ID del canal:
# 1. Agregar el bot al canal como administrador
# 2. Enviar un mensaje al canal
# 3. Abrir: https://api.telegram.org/botTU_BOT_TOKEN/getUpdates
# 4. El chat.id del canal es el ChatId

az keyvault secret set `
  --vault-name pawtrack-kv-prod `
  --name "telegram-bot-token" `
  --value "1234567890:ABCxxxxx"  # Token del bot

az webapp config appsettings set `
  --name pawtrack-prod-api `
  --resource-group pawtrack-prod-rg `
  --settings "Broadcast__Telegram__ChatId=-100123456789"  # Chat ID del canal
```

### Verificar que los broadcasts funcionan

```powershell
# Enviar un broadcast de prueba manualmente (requiere un LostPetEventId real)
curl -X POST https://api.pawtrack.cr/api/broadcast/{lostPetEventId} `
  -H "Authorization: Bearer TU_ADMIN_JWT"
```

---

## 10. Redis — Rate limiter distribuido

El rate limiter distribuido (`DistributedNotificationRateLimitService`) funciona con in-memory fallback si Redis no está configurado. En producción con múltiples instancias, Redis es **necesario** para consistencia.

### Crear Azure Cache for Redis (Basic C0 — suficiente para MVP)

```powershell
# Crear la instancia Redis (Basic C0 ~$16/mes)
az redis create `
  --name pawtrack-redis-prod `
  --resource-group pawtrack-prod-rg `
  --location eastus `
  --sku Basic `
  --vm-size C0

# Obtener el connection string
$redisPrimary = az redis list-keys `
  --name pawtrack-redis-prod `
  --resource-group pawtrack-prod-rg `
  --query primaryKey -o tsv

$redisHost = az redis show `
  --name pawtrack-redis-prod `
  --resource-group pawtrack-prod-rg `
  --query hostName -o tsv

$redisConnStr = "${redisHost}:6380,password=${redisPrimary},ssl=True,abortConnect=False"

# Guardar en Key Vault
az keyvault secret set `
  --vault-name pawtrack-kv-prod `
  --name "redis-connection-string" `
  --value $redisConnStr

# Configurar en el App Service
az webapp config appsettings set `
  --name pawtrack-prod-api `
  --resource-group pawtrack-prod-rg `
  --settings "Redis__ConnectionString=@Microsoft.KeyVault(VaultName=pawtrack-kv-prod;SecretName=redis-connection-string)"
```

> **Nota:** Si la app corre con una sola instancia (sin auto-scale), Redis es opcional. El `AddDistributedMemoryCache()` es suficiente y no hay costo adicional.

---

## 11. SendGrid — Email transaccional

### Crear cuenta y API Key

1. Registrarse en `sendgrid.com` (plan Free: 100 emails/día)
2. Verificar el dominio `pawtrack.cr` en **Settings → Sender Authentication**
3. Ir a **Settings → API Keys → Create API Key**
4. Permisos: `Mail Send` (restricted access)

```powershell
az keyvault secret set `
  --vault-name pawtrack-kv-prod `
  --name "sendgrid-api-key" `
  --value "SG.xxxxxxxxxxxxxxxx"
```

### Verificar autenticación de dominio

En tu registrar de DNS, añadir los registros CNAME que SendGrid especifica (DKIM + SPF):

| Tipo  | Nombre                      | Valor                                   |
| ----- | --------------------------- | --------------------------------------- |
| CNAME | `em1234.pawtrack.cr`        | `u1234567.wl.sendgrid.net`              |
| CNAME | `s1._domainkey.pawtrack.cr` | `s1.domainkey.uXXXXXXX.wl.sendgrid.net` |
| CNAME | `s2._domainkey.pawtrack.cr` | `s2.domainkey.uXXXXXXX.wl.sendgrid.net` |

> Los valores exactos los entrega el wizard de SendGrid. Sin esto, los emails pueden caer en spam.

---

## 12. Azure Computer Vision

La visión computacional para matching de fotos de mascotas ya está configurada en el Bicep. Verificar que las credenciales están en Key Vault:

```powershell
# Obtener endpoint y key del recurso
$visionEndpoint = az cognitiveservices account show `
  --name pawtrack-prod-vision `
  --resource-group pawtrack-prod-rg `
  --query "properties.endpoint" -o tsv

$visionKey = az cognitiveservices account keys list `
  --name pawtrack-prod-vision `
  --resource-group pawtrack-prod-rg `
  --query "key1" -o tsv

az keyvault secret set --vault-name pawtrack-kv-prod --name "vision-endpoint" --value $visionEndpoint
az keyvault secret set --vault-name pawtrack-kv-prod --name "vision-key" --value $visionKey
```

---

## 13. Tractive GPS — OAuth2

Para que el collar GPS funcione, necesitas credenciales de desarrollador de Tractive.

### Obtener credenciales

1. Registrarse en `developers.tractive.com`
2. Crear una aplicación con:
   - **Redirect URI:** `https://api.pawtrack.cr/api/collars/tractive/callback`
   - **Scopes:** `activity`, `device_info`
3. Guardar `Client ID` y `Client Secret`

```powershell
az keyvault secret set --vault-name pawtrack-kv-prod --name "tractive-client-id" --value "TU_CLIENT_ID"
az keyvault secret set --vault-name pawtrack-kv-prod --name "tractive-client-secret" --value "TU_CLIENT_SECRET"
az keyvault secret set --vault-name pawtrack-kv-prod --name "tractive-encrypt-key" --value $(openssl rand -base64 32)
```

---

## 14. Verificación final antes de abrir el tráfico

Ejecutar en orden. **No pasar al siguiente paso si el anterior falla.**

```powershell
# ── 1. Health check del API ──────────────────────────────────────────────────
$apiResp = Invoke-WebRequest "https://api.pawtrack.cr/health" -SkipCertificateCheck
if ($apiResp.StatusCode -ne 200) { throw "API health check FAILED" }
Write-Host "✅ API health OK"

# ── 2. Frontend carga ────────────────────────────────────────────────────────
$swaResp = Invoke-WebRequest "https://pawtrack.cr" -SkipCertificateCheck
if ($swaResp.StatusCode -ne 200) { throw "Frontend load FAILED" }
Write-Host "✅ Frontend OK"

# ── 3. HTTPS activo (sin advertencias de certificado) ───────────────────────
# Verificar en browser: https://pawtrack.cr — debe mostrar candado verde

# ── 4. CORS funcional ────────────────────────────────────────────────────────
# Abrir devtools del browser → Network → verificar que /api/auth/register
# no tiene errores de CORS

# ── 5. Registro de usuario ───────────────────────────────────────────────────
$regResp = Invoke-RestMethod "https://api.pawtrack.cr/api/auth/register" `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"name":"Test User","email":"smoke@pawtrack.cr","password":"SmokePa$$1!"}'
Write-Host "✅ Registro OK: $($regResp.StatusCode)"

# ── 6. Email de verificación enviado (revisar inbox) ─────────────────────────
Write-Host "⚠️  Verificar manualmente que llegó el email de verificación a smoke@pawtrack.cr"

# ── 7. Migraciones aplicadas ─────────────────────────────────────────────────
# Verificar en Azure Portal → SQL Database → Query Editor:
# SELECT COUNT(*) FROM [__EFMigrationsHistory]
# Debe mostrar el número correcto de migraciones

# ── 8. Bot de WhatsApp responde ──────────────────────────────────────────────
Write-Host "⚠️  Enviar 'hola' al número de WhatsApp de la app y verificar respuesta automática"

# ── 9. Application Insights recibe datos ────────────────────────────────────
Write-Host "⚠️  Ir a Azure Portal → Application Insights → Live Metrics y verificar telemetría"

# ── 10. Contenedor adoption-photos existe ───────────────────────────────────
$containers = az storage container list --account-name pawtrackstorprod --auth-mode login --query "[].name" -o tsv
if ($containers -notcontains "adoption-photos") { throw "adoption-photos container MISSING" }
Write-Host "✅ adoption-photos container OK"
```

### Checklist final de apertura

- [ ] API responde 200 en `/health`
- [ ] Frontend carga en `https://pawtrack.cr`
- [ ] HTTPS sin advertencias en ambos dominios
- [ ] Registro de usuario funciona
- [ ] Email de verificación llega (SendGrid funcional)
- [ ] Login funciona
- [ ] Crear mascota funciona
- [ ] Subir foto de mascota funciona (Blob Storage)
- [ ] Bot de WhatsApp responde "Hola" con el menú
- [ ] Application Insights muestra telemetría
- [ ] Migraciones: las 3 nuevas (Adoptions, WhatsApp idempotency, AuditLog) están aplicadas
- [ ] Contenedor `adoption-photos` existe en Blob Storage

---

## Resumen de costos estimados (producción MVP)

| Servicio                             | SKU                   | Costo/mes     |
| ------------------------------------ | --------------------- | ------------- |
| App Service (B3 Linux)               | 2 vCPU, 7GB RAM       | ~$80          |
| Azure SQL (Standard S2)              | 50 DTU                | ~$75          |
| Blob Storage (LRS)                   | ~100 GB               | ~$5           |
| Application Insights + Log Analytics | Pay-as-you-go         | ~$15          |
| Key Vault                            | ~100 operaciones/día  | ~$5           |
| Static Web App                       | Standard              | Gratis        |
| Azure Cache for Redis (Basic C0)     | 250 MB                | ~$16          |
| SendGrid                             | Free (100 emails/día) | $0            |
| **Total estimado**                   |                       | **~$196/mes** |

> Redis es opcional para una sola instancia. Sin Redis el total es ~$180/mes.

---

_PawTrack CR — Guía operacional actualizada al 2026-08-24 · Commit 742be91_
