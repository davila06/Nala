# PawTrack CR — Pre-requisitos de Producción

> Checklist exhaustivo de todas las cuentas, servicios, secretos y configuraciones
> que deben estar en orden **antes** de ir a producción.
>
> **Última actualización: 2026-09-10** — auditado línea por línea contra
> `appsettings.json`, `Program.cs`, las migraciones EF actuales y los workflows
> reales de `.github/workflows/`. Se encontraron 2 gaps críticos nuevos no
> documentados antes (Redis, persistencia de Data Protection — ver §2.5) y la
> tabla de migraciones estaba fuertemente desactualizada (ver §7).
> Versión anterior: 2026-08-19
> Cuenta Azure: `davila06@gmail.com`
> Subscription: `Azure subscription 1` (`3832b5df-115d-4092-9fc8-2105d7b0af21`)
> Resource Group: `PawnTrackBeta`
> Región: `eastus`

### Nuevos secretos requeridos (agosto 2026)

| Secret en Key Vault          | Descripción                                                  |
| ---------------------------- | ------------------------------------------------------------ |
| `bot-phone-hash-secret`      | Mínimo 32 chars; HMAC-SHA256 para hash de teléfonos del bot  |
| `billboard-images-container` | Nombre del contenedor Blob (por defecto: `billboard-images`) |

Agregar en Container App:

```powershell
az keyvault secret set --vault-name pawtrack-kv --name bot-phone-hash-secret --value "VALOR_MINIMO_32_CHARS"
```

Luego referenciar en `appsettings.json`:

```json
"Bot": {
  "PhoneHashSecret": "@Microsoft.KeyVault(VaultName=pawtrack-kv;SecretName=bot-phone-hash-secret)"
}
```

---

## Tabla de estado rápido

| Categoría                      | Items | ✅ Listo | ⚠️ Parcial | ❌ Pendiente                                           |
| ------------------------------ | ----- | -------- | ---------- | ------------------------------------------------------ |
| Azure — infraestructura        | 10    | 9        | 0          | **1** (Redis)                                          |
| Azure Key Vault — secretos     | 18    | 0        | 0          | **18**                                                 |
| Data Protection (persistencia) | 1     | 0        | 0          | **1** 🔴 crítico, no documentado antes                 |
| DNS y dominio                  | 4     | 0        | 0          | **4** (incluye estabilidad de RP ID WebAuthn)          |
| GitHub — CI/CD                 | 17    | 0        | 0          | **17** (9 → 17 tras revisar los workflows reales)      |
| Frontend — variables Vite      | 5     | 0        | 0          | **5**                                                  |
| Servicios externos             | 8     | 0        | 2          | **6**                                                  |
| EF Migrations en Azure SQL     | —     | —        | —          | ver §7 (tabla exhaustiva reemplazada — quedó obsoleta) |
| Configuración post-deploy      | 5     | 0        | 0          | **5**                                                  |
| Verificación final             | 7     | 0        | 0          | **7**                                                  |

---

## 1. Azure — Infraestructura (ya desplegada)

Todos los recursos están creados en el resource group `PawnTrackBeta`.

| Recurso                   | Nombre                                    | Estado                                                     |
| ------------------------- | ----------------------------------------- | ---------------------------------------------------------- |
| Log Analytics Workspace   | `pawtrack-dev-logs`                       | ✅                                                         |
| Application Insights      | `pawtrack-dev-insights`                   | ✅                                                         |
| SQL Server                | `pawtrack-dev-sql`                        | ✅                                                         |
| SQL Database              | `pawtrack-dev-sql/pawtrack` (GP_S_Gen5_1) | ✅                                                         |
| Storage Account           | `pawtrackstoragdev`                       | ✅                                                         |
| Key Vault                 | `pawtrack-kv-dev`                         | ✅                                                         |
| Container Registry (ACR)  | `pawtrackacrdev`                          | ✅                                                         |
| Container Apps Env        | `pawtrack-dev-env`                        | ✅                                                         |
| Container App (API)       | `pawtrack-dev-api`                        | ✅                                                         |
| Static Web App            | `pawtrack-dev-frontend`                   | ✅                                                         |
| **Azure Cache for Redis** | _(no creado)_                             | ❌ **nuevo, no estaba en la versión anterior de este doc** |

> **⚠️ Redis es requerido para comportamiento correcto en producción, no solo
> "deseable".** El código ya soporta Redis (`Redis:ConnectionString` en
> `InfrastructureServiceCollectionExtensions.cs`) y cae de vuelta a caché
> distribuida en memoria si no está configurado — pero esa caída **rompe la
> corrección multi-instancia** de: rate limiting de notificaciones, estado
> "escribiendo…" del chat (`DistributedTypingStateService`), y el throttle de
> ubicación de `SearchCoordinationHub`. Con más de 1 réplica del Container App
> (`--min-replicas 1 --max-replicas 3`, ya configurado en §8.4), cada réplica
> tendría su propia caché en memoria — el rate limit se podría burlar
> repartiendo requests entre réplicas, y el throttle de typing/ubicación
> dejaría de funcionar de forma consistente. **Crear el recurso y cargar el
> secreto antes de escalar a más de 1 réplica.**
>
> ```powershell
> az redis create --name pawtrack-redis --resource-group PawnTrackBeta \
>   --location eastus --sku Basic --vm-size c0
> az redis list-keys --name pawtrack-redis --resource-group PawnTrackBeta
> # Cargar el connection string (host:port,password=...,ssl=True) como secreto:
> az keyvault secret set --vault-name pawtrack-kv-dev --name redis-connection-string --value "<CONN_STRING>"
> ```

**Blob containers requeridos en `pawtrackstoragdev`:**

```powershell
az storage container create --name pet-photos       --account-name pawtrackstoragdev --public-access blob
az storage container create --name sighting-photos  --account-name pawtrackstoragdev --public-access blob
az storage container create --name medical-docs     --account-name pawtrackstoragdev --public-access off
az storage container create --name municipal-photos --account-name pawtrackstoragdev --public-access off
az storage container create --name clinic-logos     --account-name pawtrackstoragdev --public-access blob
az storage container create --name vet-certificates --account-name pawtrackstoragdev --public-access off
az storage container create --name dataprotection-keys --account-name pawtrackstoragdev --public-access off
```

> El contenedor `dataprotection-keys` es nuevo — ver §2.5, es parte del fix
> pendiente de persistencia de Data Protection, no solo un contenedor de blobs
> más de la app.

---

## 2. Azure Key Vault — Secretos

Todos los secretos de producción van en `pawtrack-kv-dev`.  
El Container App lee las referencias `@Microsoft.KeyVault(VaultName=pawtrack-kv;SecretName=...)` automáticamente vía Managed Identity.

> **Generar el JWT signing key:**  
> `openssl rand -base64 48`  
> (mínimo 32 chars; la app falla al inicio si no cumple)

### 2.1 Secretos requeridos (17)

| Nombre del secreto              | Clave en `appsettings.json`            | Descripción                                                                                          | Cómo obtenerlo                                     |
| ------------------------------- | -------------------------------------- | ---------------------------------------------------------------------------------------------------- | -------------------------------------------------- |
| `sql-connection-string`         | `ConnectionStrings:DefaultConnection`  | Cadena de conexión a Azure SQL                                                                       | Azure Portal → SQL Server → Connection strings     |
| `jwt-signing-key`               | `Jwt:Key`                              | Clave HMAC-SHA256, mínimo 32 chars                                                                   | `openssl rand -base64 48`                          |
| `storage-connection-string`     | `Azure:Storage:ConnectionString`       | Conexión a Blob Storage                                                                              | Azure Portal → Storage Account → Access keys       |
| `vision-endpoint`               | `Azure:Vision:Endpoint`                | URL del recurso Azure Computer Vision                                                                | Azure Portal → Computer Vision → Keys and Endpoint |
| `vision-key`                    | `Azure:Vision:Key`                     | API Key de Azure Computer Vision                                                                     | Mismo lugar                                        |
| `appinsights-connection-string` | `ApplicationInsights:ConnectionString` | Telemetría                                                                                           | Azure Portal → Application Insights → Properties   |
| `sendgrid-api-key`              | `SendGrid:ApiKey`                      | Email transaccional                                                                                  | sendgrid.com → Settings → API Keys → Create        |
| `whatsapp-phone-number-id`      | `Broadcast:WhatsApp:PhoneNumberId`     | ID del número en Meta                                                                                | Meta Business → WhatsApp → Getting Started         |
| `whatsapp-access-token`         | `Broadcast:WhatsApp:AccessToken`       | Token permanente de sistema Meta                                                                     | Meta Business → System Users → Generate token      |
| `whatsapp-app-secret`           | `WhatsApp:AppSecret`                   | Secret de la app Meta (validar webhooks)                                                             | Meta for Developers → App → Settings → Basic       |
| `whatsapp-verify-token`         | `WhatsApp:VerifyToken`                 | Token que tú defines para verificar el webhook                                                       | Inventarlo tú (ej. `openssl rand -hex 16`)         |
| `telegram-bot-token`            | `Broadcast:Telegram:BotToken`          | Token del bot de Telegram                                                                            | @BotFather en Telegram → `/newbot`                 |
| `facebook-page-access-token`    | `Broadcast:Facebook:PageAccessToken`   | Token de la página de FB                                                                             | Meta Business → Page Access Token                  |
| `facebook-page-id`              | `Broadcast:Facebook:PageId`            | ID de la página de FB                                                                                | Configuración de la página de Facebook             |
| `tractive-client-id`            | `Tractive:ClientId`                    | OAuth2 client para Tractive GPS                                                                      | developers.tractive.com → My Applications → Create |
| `tractive-client-secret`        | `Tractive:ClientSecret`                | OAuth2 secret para Tractive GPS                                                                      | Mismo lugar                                        |
| `tractive-encrypt-key`          | `Tractive:EncryptKey`                  | Clave AES-256 para cifrar tokens OAuth (32 bytes)                                                    | `openssl rand -base64 32`                          |
| `redis-connection-string`       | `Redis:ConnectionString`               | Azure Cache for Redis — requerido para rate limiting/chat/throttle correctos con >1 réplica (ver §1) | `az redis list-keys` tras crear el recurso         |

### 2.2 Secretos opcionales (pueden quedar vacíos en MVP)

| Nombre del secreto            | Clave                       | Descripción                                                                                    |
| ----------------------------- | --------------------------- | ---------------------------------------------------------------------------------------------- |
| `avatartoken-signing-key`     | `AvatarToken:SigningKey`    | HMAC para tokens de avatar WhatsApp. Si está vacío, los tokens no expiran.                     |
| `webhooks-sinpe-secret`       | `Webhooks:SinpeSecret`      | Webhook de SINPE (si se integra pasarela real). No requerido para MVP con verificación manual. |
| `azure-maps-subscription-key` | `AzureMaps:SubscriptionKey` | Geocodificación y lookup de IP. Si está vacío, no se muestra el cantón en las alertas.         |

### 2.3 Comandos para cargar secretos

```powershell
# Autenticar
az login
az keyvault secret set --vault-name pawtrack-kv-dev --name "jwt-signing-key" --value "<VALOR>"
az keyvault secret set --vault-name pawtrack-kv-dev --name "sql-connection-string" --value "<CONN_STRING>"
# ... (repetir para cada secreto)
```

### 2.4 Managed Identity — permisos requeridos

```powershell
# Obtener el principal ID del Container App
$principalId = az containerapp show \
  --name pawtrack-dev-api \
  --resource-group PawnTrackBeta \
  --query "identity.principalId" -o tsv

# Dar acceso al Key Vault
az keyvault set-policy \
  --name pawtrack-kv-dev \
  --object-id $principalId \
  --secret-permissions get list
```

---

## 2.5 Data Protection — persistencia de claves (🔴 crítico, no documentado antes)

**Hallazgo (2026-09-10):** `Program.cs` llama `builder.Services.AddDataProtection();`
sin `.PersistKeysToAzureBlobStorage(...)` ni `.ProtectKeysWithAzureKeyVault(...)`.
Esta API ya se usa activamente por dos features enterprise agregadas esta
sesión:

- `TotpMfaService` (`backend/src/PawTrack.Infrastructure/Auth/TotpMfaService.cs`) — cifra el secreto TOTP de cada usuario con MFA activado.
- `WebhookFanout` / `OutboundWebhookHostedService` — cifra el secreto HMAC de cada webhook saliente configurado.

**Por qué es crítico:** sin persistencia explícita, Azure Container Apps no
garantiza que las claves de Data Protection sobrevivan un reinicio del
contenedor, ni que sean las mismas entre réplicas (`--min-replicas 1
--max-replicas 3` ya está configurado en §8.4). Esto significa:

- Un secreto TOTP cifrado por la réplica A puede fallar al descifrarse si la
  siguiente request de ese usuario la atiende la réplica B — el login con MFA
  falla de forma intermitente e impredecible.
- Tras cualquier redeploy/reinicio, **todos** los secretos TOTP y de webhooks
  ya cifrados quedan permanentemente indescifrables (equivalente a pérdida de
  datos) — los usuarios con MFA activado quedan bloqueados de su cuenta y hay
  que forzar un reset manual de MFA.

**Fix pendiente (código, no solo infraestructura):**

```csharp
// Program.cs
builder.Services.AddDataProtection()
    .PersistKeysToAzureBlobStorage(blobUri) // contenedor dataprotection-keys, ver §1
    .ProtectKeysWithAzureKeyVault(keyVaultKeyUri, credential);
```

Requiere: el contenedor `dataprotection-keys` ya agregado en §1, y una clave
RSA en Key Vault dedicada para envolver las claves de Data Protection (no
reutilizar `jwt-signing-key` ni ninguna otra). **No confirmado aún si este fix
de código ya se implementó en otra rama — verificar antes de asumir que sigue
pendiente.**

## 2.6 WebAuthn / Passkeys — estabilidad de dominio

`appsettings.json` ya trae `WebAuthn:ServerDomain = "pawtrack.cr"` y
`WebAuthn:Origins = ["https://pawtrack.cr", "https://www.pawtrack.cr"]`
hardcodeados (no son referencias de Key Vault, van directo en el archivo).
Esto es correcto **siempre que el dominio ya esté configurado antes de que el
primer usuario registre una passkey** — el "Relying Party ID" de WebAuthn
queda atado al dominio en el momento del registro; si el dominio cambia
después (ej. lanzar primero en `*.azurestaticapps.net` y migrar luego a
`pawtrack.cr`), **todas las passkeys registradas antes del cambio dejan de
funcionar** y los usuarios deben volver a registrarlas. Ver §3 (DNS y
dominio) — agregado como ítem #4 de esa sección.

---

## 3. DNS y dominio

| Tarea                                                                       | Proveedor sugerido                                                         | Estado        |
| --------------------------------------------------------------------------- | -------------------------------------------------------------------------- | ------------- |
| Comprar dominio `pawtrack.cr`                                               | [NIC.cr](https://nic.cr) (requiere cuenta de persona jurídica o física CR) | ❌            |
| CNAME `pawtrack.cr` → FQDN del Container App                                | Nameservers de NIC.cr o Cloudflare                                         | ❌            |
| Custom domain en Azure Container App                                        | `az containerapp hostname add`                                             | ❌            |
| Confirmar dominio final ANTES de que usuarios registren passkeys (WebAuthn) | N/A — decisión de producto/timing                                          | ❌ — ver §2.6 |

```powershell
# Una vez el CNAME propague:
az containerapp hostname add \
  --name pawtrack-dev-api \
  --resource-group PawnTrackBeta \
  --hostname api.pawtrack.cr

# Para el Static Web App del frontend:
az staticwebapp hostname set \
  --name pawtrack-dev-frontend \
  --resource-group PawnTrackBeta \
  --hostname pawtrack.cr
```

**Variables que cambian al activar el dominio:**

| Variable                       | Valor actual                                  | Valor prod                                     |
| ------------------------------ | --------------------------------------------- | ---------------------------------------------- |
| `App:BaseUrl`                  | `https://pawtrack.azurestaticapps.net`        | `https://pawtrack.cr`                          |
| `Cors:AllowedOrigins[0]`       | `https://pawtrack.azurestaticapps.net`        | `https://pawtrack.cr`                          |
| `VITE_API_URL` (GitHub Secret) | FQDN del Container App                        | `https://api.pawtrack.cr`                      |
| Tractive OAuth redirect URI    | `{App:BaseUrl}/api/collars/tractive/callback` | Auto (usa `App:BaseUrl`)                       |
| WhatsApp webhook URL           | —                                             | `https://api.pawtrack.cr/api/whatsapp/webhook` |

---

## 4. GitHub — Secrets para CI/CD

Ruta: `github.com/usuario/PawTrack-CR → Settings → Secrets and variables → Actions`

> **Auditado 2026-09-10 contra los workflows reales** (`backend.yml`,
> `frontend.yml`, `infra.yml`, `smoke-tests.yml`) — la lista anterior (9
> secrets) estaba incompleta, faltaban 8: 4 usados por `infra.yml`/
> `smoke-tests.yml` y 4 variables `VITE_*` que **sí son secrets individuales**
> del repo (se confirmó que no existe un solo `VITE_ENV_VARS` — cada una se
> referencia por su propio nombre en `frontend.yml`).

**Usados por `backend.yml` (deploy del API):**

| Secret                  | Descripción                                               | Valor                                          |
| ----------------------- | --------------------------------------------------------- | ---------------------------------------------- |
| `AZURE_CLIENT_ID`       | App Registration Client ID (Workload Identity Federation) | Azure Portal → App registrations               |
| `AZURE_TENANT_ID`       | Azure AD Tenant ID                                        | `ab810006-3d9f-431f-aabd-52c4a26340af`         |
| `AZURE_SUBSCRIPTION_ID` | Subscription ID                                           | `3832b5df-115d-4092-9fc8-2105d7b0af21`         |
| `ACR_NAME`              | Nombre del Container Registry                             | `pawtrackacrdev`                               |
| `CONTAINER_APP_NAME`    | Nombre del Container App                                  | `pawtrack-dev-api`                             |
| `CONTAINER_APP_FQDN`    | FQDN del Container App (sin `https://`)                   | Ver Azure Portal                               |
| `AZURE_RESOURCE_GROUP`  | Resource Group                                            | `PawnTrackBeta`                                |
| `SQL_CONNECTION_STRING` | Connection string para migraciones en CI                  | Igual que `sql-connection-string` de Key Vault |

**Usados por `frontend.yml` (deploy del Static Web App):**

| Secret                            | Descripción                        | Valor                                                   |
| --------------------------------- | ---------------------------------- | ------------------------------------------------------- |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Token de deploy del Static Web App | Azure Portal → Static Web App → Manage deployment token |
| `VITE_API_URL`                    | URL base del backend API           | Igual que §5                                            |
| `VITE_VAPID_PUBLIC_KEY`           | Clave pública VAPID                | Igual que §5                                            |
| `VITE_COLLAR_WHATSAPP_NUMBER`     | Número WA para ventas de collar    | Igual que §5                                            |
| `VITE_SINPE_PHONE`                | Número SINPE Móvil                 | Igual que §5                                            |

> ⚠️ **Gap detectado en CI:** `frontend.yml` NO pasa
> `VITE_APPINSIGHTS_CONNECTION_STRING` al build, aunque el código
> (`frontend/src/shared/lib/telemetry.ts`) sí la lee. Application Insights del
> **frontend** queda efectivamente deshabilitado en cualquier build hecho por
> este workflow hasta que se agregue ese secret al step de build.

**Usados por `infra.yml` / `smoke-tests.yml` (no estaban documentados antes):**

| Secret                | Descripción                                            | Valor                                              |
| --------------------- | ------------------------------------------------------ | -------------------------------------------------- |
| `SQL_ADMIN_PASSWORD`  | Password del admin de Azure SQL, usado por Bicep/infra | Definir uno fuerte, no reutilizar el de desarrollo |
| `ALERT_EMAIL_ADDRESS` | Correo destino de alertas de Azure Monitor             | Correo del equipo de operaciones                   |
| `SMOKE_FRONTEND_URL`  | URL del frontend para smoke tests post-deploy          | `https://pawtrack.cr` (o el FQDN actual)           |
| `SMOKE_API_URL`       | URL del API para smoke tests post-deploy               | `https://api.pawtrack.cr` (o el FQDN actual)       |

**Configurar Workload Identity Federation (sin secretos de service principal):**

```powershell
# 1. Crear App Registration
az ad app create --display-name "pawtrack-github-actions"

# 2. Crear Service Principal
$appId=$(az ad app list --display-name "pawtrack-github-actions" --query "[0].appId" -o tsv)
az ad sp create --id $appId

# 3. Asignar rol Contributor en el Resource Group
$spId=$(az ad sp show --id $appId --query id -o tsv)
az role assignment create \
  --role Contributor \
  --assignee $spId \
  --scope "/subscriptions/3832b5df-115d-4092-9fc8-2105d7b0af21/resourceGroups/PawnTrackBeta"

# 4. Asignar AcrPush al ACR
az role assignment create \
  --role AcrPush \
  --assignee $spId \
  --scope $(az acr show --name pawtrackacrdev --query id -o tsv)

# 5. Federated credential para GitHub Actions
az ad app federated-credential create \
  --id $appId \
  --parameters '{
    "name": "github-main",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:TU_USUARIO/PawTrack-CR:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

---

## 5. Frontend — Variables de entorno Vite

Estas variables se confirmaron como **secrets individuales del repositorio**
(no un solo `VITE_ENV_VARS` — se verificó contra `frontend.yml`, cada una se
referencia por su propio nombre), inyectadas en el build del Static Web App.

| Variable                             | Descripción                                 | Valor prod                                                                                            |
| ------------------------------------ | ------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| `VITE_API_URL`                       | URL base del backend API                    | `https://api.pawtrack.cr` (o FQDN del Container App)                                                  |
| `VITE_VAPID_PUBLIC_KEY`              | Clave pública VAPID para push notifications | Igual que `Notifications:Push:VapidPublicKey` en el backend                                           |
| `VITE_SINPE_PHONE`                   | Número de SINPE Móvil para recibir pagos    | Número real de la cuenta SINPE de PawTrack CR                                                         |
| `VITE_COLLAR_WHATSAPP_NUMBER`        | Número de WA para ventas de collar físico   | Número de WA de atención al cliente (opcional, oculta el CTA si está vacío)                           |
| `VITE_APPINSIGHTS_CONNECTION_STRING` | Telemetría frontend                         | Igual que `appinsights-connection-string` de Key Vault — **falta agregarla a `frontend.yml`, ver §4** |

**Generar las claves VAPID** (una sola vez; deben ser las mismas en backend y frontend):

```powershell
# Instalar web-push globalmente
npm install -g web-push

# Generar par de claves VAPID
web-push generate-vapid-keys
# Output:
#   Public Key:  BPvjWk...  → VITE_VAPID_PUBLIC_KEY y Notifications:Push:VapidPublicKey
#   Private Key: QKA8rx...  → Notifications:Push:VapidPrivateKey (solo en Key Vault)
```

> ⚠️ Las claves VAPID de `appsettings.Local.json` (`BPvjWk...` / `QKA8rx...`) son de desarrollo.
> **Genera nuevas para producción** y no reutilices las de local.

---

## 6. Servicios externos — Cuentas y configuración

### 6.1 SendGrid (email transaccional) ✅ Cuenta creada, pendiente producción

**Costo:** gratis hasta 100 emails/día, USD $19.95/mes para 40,000.

**Pasos:**

1. [sendgrid.com](https://sendgrid.com) → Create Account
2. Settings → API Keys → Create API Key (Full Access) → copiar en Key Vault
3. Settings → Sender Authentication → Verify a Domain → `pawtrack.cr`
   - Agregar registros DNS: CNAME `em####.pawtrack.cr`, `s1._domainkey.pawtrack.cr`, `s2._domainkey.pawtrack.cr`
4. Verificar que se puede enviar con `From: noreply@pawtrack.cr`

---

### 6.2 Meta / WhatsApp Business API ❌ Pendiente

**Costo:** USD $0.01-0.04 por mensaje enviado (template) / gratis para ventana de 24h.

**Pasos:**

1. [developers.facebook.com](https://developers.facebook.com) → My Apps → Create App → Business
2. Add Product → WhatsApp
3. Getting Started: anotar `Phone Number ID` y `Access Token temporal`
4. Configuration → Webhooks → Add Callback URL:
   - URL: `https://api.pawtrack.cr/api/whatsapp/webhook`
   - Verify Token: el mismo valor que pondrás en `WhatsApp:VerifyToken` en Key Vault
   - Subscribe to: `messages`
5. Business Settings → System Users → Add System User → Generate Token permanente
6. Subir el número del negocio a producción (proceso de revisión Meta, ~24-48h)
7. Crear template de mensaje para difusión de mascotas perdidas:
   - Template name: `lost_pet_broadcast`
   - Language: `es_CR`
   - Category: `UTILITY`

---

### 6.3 Azure Computer Vision (IA visual matching) ❌ Pendiente

**Costo:** USD $1/1,000 vectorizaciones. Sin configurar, la búsqueda visual por foto regresa error gracioso.

**Pasos:**

1. Azure Portal → Create Resource → Computer Vision (plan F0 gratis: 5,000/mes o S1 USD $1/1k)
2. Keys and Endpoint → copiar Endpoint y Key1 en Key Vault
3. Verificar en logs: `"Azure Vision is not configured"` desaparece

---

### 6.4 Azure Maps ❌ Pendiente (opcional para MVP)

**Costo:** USD $4.50/1,000 geocodificaciones. Sin configurar, las alertas no muestran el nombre del cantón.

**Pasos:**

1. Azure Portal → Create Resource → Azure Maps Account (S0 gratis: 25,000 geocodificaciones/mes en el primer año)
2. Authentication → Subscription key → copiar en Key Vault como `azure-maps-subscription-key`

---

### 6.5 Tractive GPS ❌ Pendiente

**Costo:** sin costo para la app; el usuario paga su suscripción Tractive.

**Pasos:**

1. [developers.tractive.com](https://developers.tractive.com) → Log in con cuenta de Tractive
2. My Applications → Create Application:
   - App Name: `PawTrack CR`
   - Redirect URI: `https://api.pawtrack.cr/api/collars/tractive/callback`
   - Scopes: `activity device_info`
3. Copiar `Client ID` y `Client Secret` en Key Vault
4. Generar clave de cifrado: `openssl rand -base64 32` → Key Vault `tractive-encrypt-key`

---

### 6.6 Telegram Bot ❌ Pendiente (opcional)

**Costo:** gratuito.

**Pasos:**

1. Buscar `@BotFather` en Telegram → `/newbot` → elegir nombre `PawTrack CR Bot` y username `pawtrack_cr_bot`
2. Copiar token en Key Vault como `telegram-bot-token`
3. El bot envía alertas a canales configurados en `Broadcast:Telegram:RecipientChatId` (a agregar)

---

### 6.7 Facebook Page ❌ Pendiente (opcional)

**Costo:** gratuito para publicaciones orgánicas.

**Pasos:**

1. Crear/tener una Facebook Page de PawTrack CR
2. Graph API Explorer → Get Page Access Token (long-lived)
3. Copiar `Page Access Token` y `Page ID` en Key Vault

---

### 6.8 SINPE Móvil — Cuenta bancaria para pagos ❌ Pendiente

**Requisito:** cuenta bancaria costarricense con SINPE habilitado.

**Pasos:**

1. Abrir cuenta en Banco Nacional, BCR, BAC u otro banco CR vinculado a SINPE
2. Anotar el número de teléfono asociado → cargar en `VITE_SINPE_PHONE`
3. Nombre de cuenta debe coincidir con el nombre legal de PawTrack CR (para validación manual)

---

## 7. Migraciones EF Core en Azure SQL

Las migraciones están solo en el entorno local. Antes del primer deploy:

```powershell
# Ejecutar todas las migraciones en la BD de Azure
dotnet ef database update \
  --project backend/src/PawTrack.Infrastructure \
  --startup-project backend/src/PawTrack.API \
  --connection "<SQL_CONNECTION_STRING_DE_AZURE>"
```

**⚠️ Ya NO se mantiene una tabla exhaustiva de migraciones aquí.** La versión
anterior de este doc (agosto 2026) listaba 11 migraciones como "pendientes",
pero para 2026-09-10 hay **más de 40 migraciones nuevas** encima de esas (compliance,
MFA, WebAuthn/passkeys, webhooks salientes, exportaciones regulatorias,
gobernanza de billboards, collares, etc.) — mantener esta lista fila por fila
quedó demostrado como insostenible y es la razón por la que este documento
completo se había puesto obsoleto. En su lugar:

```powershell
# Ver cuántas migraciones locales existen vs. cuáles ya están aplicadas en Azure SQL
dotnet ef migrations list --project backend/src/PawTrack.Infrastructure --startup-project backend/src/PawTrack.API

# Aplicar TODAS las pendientes de una vez (dotnet ef ya sabe cuáles faltan, no requiere lista manual)
dotnet ef database update \
  --project backend/src/PawTrack.Infrastructure \
  --startup-project backend/src/PawTrack.API \
  --connection "<SQL_CONNECTION_STRING_DE_AZURE>"

# Verificar después del deploy: la última fila debe coincidir con la migración más reciente en el repo
sqlcmd -S <servidor> -d <bd> -Q "SELECT TOP 5 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC"
```

Snapshot de referencia (2026-09-10): la migración más reciente en el repo es
`20260910002809_AddScheduledSubscriptionChanges` — si `dotnet ef migrations
list` muestra una más nueva que esa, esta nota también está desactualizada;
no confiar en el nombre exacto, confiar en el comando.

---

## 8. Configuración post-deploy (Azure Portal / CLI)

### 8.1 Content Security Policy del frontend

Actualizar el CSP en `frontend/index.html` para reemplazar dominios `localhost` y `azurecontainerapps.io` con el FQDN definitivo de producción.

```html
<!-- Cambiar en connect-src: -->
<!-- De: https://*.azurecontainerapps.io -->
<!-- A:  https://api.pawtrack.cr -->
```

### 8.2 CORS del backend

En Key Vault / Container App env vars:

```
Cors__AllowedOrigins__0 = https://pawtrack.cr
```

(Actualmente está como `https://pawtrack.azurestaticapps.net`)

### 8.3 SignalR — habilitar sticky sessions

El `SearchCoordinationHub` y `ChatHub` requieren sticky sessions en producción:

```powershell
az containerapp update \
  --name pawtrack-dev-api \
  --resource-group PawnTrackBeta \
  --sticky-sessions-affinity "sticky"
```

### 8.4 Containers — ajustar recursos mínimos

```powershell
# Aumentar CPU/RAM para producción con carga real
az containerapp update \
  --name pawtrack-dev-api \
  --resource-group PawnTrackBeta \
  --min-replicas 1 \
  --max-replicas 3 \
  --cpu 0.5 \
  --memory "1Gi"
```

### 8.5 Notificaciones push — VapidSubject

Configurar en Key Vault / env vars:

```
Notifications__Push__VapidSubject = mailto:ops@pawtrack.cr
```

---

## 9. Verificación final antes de go-live

| Verificación         | Comando / Método                                                                                      | Esperado                                                                                                                                             |
| -------------------- | ----------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| API healthcheck      | `curl https://api.pawtrack.cr/health`                                                                 | `{"status":"Healthy"}`                                                                                                                               |
| Autenticación JWT    | `POST /api/auth/login` con admin@pawtrack.cr                                                          | Token válido                                                                                                                                         |
| Blob Storage         | Subir foto de mascota desde la app                                                                    | URL pública en `*.blob.core.windows.net`                                                                                                             |
| Email (SendGrid)     | Registrar cuenta nueva → verificar que llega email                                                    | Email recibido en < 2 min                                                                                                                            |
| Push notification    | Activar push en la app y crear alerta de prueba                                                       | Notificación en el dispositivo                                                                                                                       |
| Visual matching      | Subir foto de mascota → buscar coincidencias                                                          | Resultados sin error 500                                                                                                                             |
| Migrations aplicadas | `dotnet ef migrations list` (o `SELECT TOP 5 MigrationId FROM __EFMigrationsHistory ORDER BY 1 DESC`) | La fila más reciente coincide con la última carpeta en `backend/src/PawTrack.Infrastructure/Migrations/` — no comparar contra un nombre fijo, ver §7 |

---

## 10. Resumen de pasos ordenados por prioridad

### 🔴 Bloquean el funcionamiento básico (hacer primero)

1. ☐ Cargar todos los secretos en Key Vault (especialmente `jwt-signing-key`, `sql-connection-string`, `storage-connection-string`, `sendgrid-api-key`)
2. ☐ Aplicar migraciones EF en Azure SQL (`dotnet ef database update`, ver §7)
3. ☐ Configurar `Cors:AllowedOrigins` con el dominio final
4. ☐ Configurar `VITE_API_URL` en GitHub Secrets → redeploy del frontend
5. ☐ Configurar `VITE_SINPE_PHONE` con el número real de SINPE
6. ☐ **Crear Azure Cache for Redis y cargar `redis-connection-string`** — nuevo, ver §1/§2.1. Sin esto, rate limiting/chat/throttle fallan de forma inconsistente en cuanto haya >1 réplica.
7. ☐ **Configurar persistencia de Data Protection** (`.PersistKeysToAzureBlobStorage` + `.ProtectKeysWithAzureKeyVault` en `Program.cs`) — nuevo, ver §2.5. Sin esto, MFA y webhooks salientes se rompen tras cualquier reinicio/redeploy.

### 🟠 Bloquean features clave (hacer antes del launch público)

8. ☐ Configurar SendGrid + verificar dominio de email
9. ☐ Registrar app OAuth en Meta → configurar WhatsApp webhook
10. ☐ Configurar GitHub Secrets para CI/CD (17 en total, ver §4)
11. ☐ Comprar y configurar dominio `pawtrack.cr` **antes de que cualquier usuario registre una passkey/WebAuthn** (ver §2.6)
12. ☐ Generar claves VAPID de producción → cargar en Key Vault + GitHub Secrets
13. ☐ Agregar `VITE_APPINSIGHTS_CONNECTION_STRING` al step de build en `frontend.yml` (falta, ver §4)

### 🟡 Mejoran el producto pero no bloquean el launch

14. ☐ Configurar Azure Computer Vision (búsqueda visual por foto)
15. ☐ Registrar app Tractive (collar GPS)
16. ☐ Configurar Azure Maps (geocodificación de cantones en alertas)
17. ☐ Crear bot de Telegram y bot de Facebook (canales de difusión alternativos)
18. ☐ Ajustar CPU/RAM del Container App para carga real

---

_PawTrack CR · Documento de prerrequisitos de producción · Actualizado 2026-09-10_
