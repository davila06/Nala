# Pasos para ir live en Azure para PawTrack CR

## 1. Objetivo de este documento

Este documento explica, paso por paso, como llevar PawTrack CR a produccion en Azure de la forma mas segura y simple posible.

Esta guia esta escrita para una persona con poco conocimiento tecnico. Aun asi, no oculta los riesgos reales. Si algo no esta listo, el documento lo marca como bloqueo o como tarea opcional.

La meta final es dejar funcionando:

- Frontend publico en `https://pawtrack.cr` o `https://www.pawtrack.cr`
- API backend en `https://api.pawtrack.cr`
- Base de datos SQL en Azure
- Fotos en Blob Storage
- Secretos en Key Vault
- Monitoreo y alertas en Azure Monitor y Application Insights

## 2. Resumen ejecutivo

### Arquitectura recomendada para este repo

Segun el estado actual del proyecto, la ruta recomendada es esta:

1. Frontend en Azure Static Web Apps
2. Backend en Azure App Service Linux
3. Base de datos en Azure SQL Database
4. Fotos en Azure Blob Storage
5. Secretos en Azure Key Vault
6. Telemetria en Application Insights + Log Analytics

### Lo que ya existe en la infraestructura del repo

El archivo `infra/main.bicep` ya crea o configura:

- Log Analytics Workspace
- Application Insights
- Azure SQL Server
- Azure SQL Database
- Storage Account y contenedores `pet-photos` y `sighting-photos`
- Key Vault
- App Service Plan Linux
- App Service del backend
- Permiso del App Service para leer secretos del Key Vault
- Alertas basicas de 5xx, latencia y disponibilidad

### Lo que NO esta claramente automatizado en el repo actual

El frontend no aparece claramente creado dentro de `infra/main.bicep`. Por eso, para el go-live, asume que el frontend se creara manualmente en Azure Static Web Apps.

## 3. Recomendacion de nombres y dominios

Usa esta convencion desde el inicio:

- Dominio frontend principal: `pawtrack.cr`
- Dominio frontend alterno: `www.pawtrack.cr`
- Dominio API: `api.pawtrack.cr`
- Resource group: `pawtrack-prod-rg`
- Azure Static Web App: `pawtrack-prod-frontend`
- App Service API: `pawtrack-prod-api`

Si ya tienes otros nombres, puedes cambiarlos, pero debes mantener consistencia en:

- DNS
- CORS
- `App__BaseUrl`
- `VITE_API_URL`

## 4. Lista de acceso y prerrequisitos

Antes de tocar Azure, confirma que tienes esto:

- Una suscripcion activa de Azure con permiso para crear recursos
- Acceso al registrador DNS del dominio `pawtrack.cr`
- Acceso al correo que recibira alertas operativas
- Acceso a este repo localmente
- .NET SDK instalado
- Node.js instalado
- Azure CLI instalado
- Permisos para crear secretos en Key Vault

### Herramientas minimas recomendadas

- Azure Portal
- Azure CLI
- PowerShell
- Git

### Comandos para verificar herramientas

```powershell
az --version
dotnet --version
node --version
npm --version
```

Si alguno falla, instalalo antes de seguir.

## 5. Ruta recomendada de despliegue

Usa esta secuencia exacta:

1. Crear o validar el Resource Group
2. Desplegar la infraestructura base con Bicep
3. Crear el frontend en Azure Static Web Apps
4. Cargar secretos en Key Vault
5. Configurar variables de App Service y del frontend
6. Ejecutar migraciones de base de datos
7. Publicar backend
8. Publicar frontend
9. Configurar dominios y HTTPS
10. Probar punta a punta
11. Encender monitoreo real y checklist final

## 6. Variables y secretos que debes preparar

Esta es la parte mas importante. Si las variables no estan bien, el despliegue parece exitoso, pero la app falla despues.

### 6.1 Variables obligatorias para el primer go-live

Estas son obligatorias para que la app base funcione:

| Nombre | Donde se configura | Obligatoria | Para que sirve |
|---|---|---|---|
| `SQL_ADMIN_PASSWORD` | Solo durante despliegue de Bicep | Si | Password del admin de Azure SQL al crear infraestructura |
| `sql-connection-string` | Key Vault secret | Si | Conexion real del backend a SQL |
| `jwt-signing-key` | Key Vault secret | Si | Firma de tokens JWT |
| `storage-connection-string` | Key Vault secret | Si | Subida de fotos a Blob Storage |
| `appinsights-connection-string` | Key Vault secret | Si | Telemetria de aplicacion |
| `App__BaseUrl` | App Service setting | Si | URL publica base del frontend, por ejemplo `https://pawtrack.cr` |
| `VITE_API_URL` | Build del frontend | Si | URL publica de la API, por ejemplo `https://api.pawtrack.cr` |
| `Cors__AllowedOrigins__0` | App Service setting | Si | Origen permitido del frontend, por ejemplo `https://pawtrack.cr` |
| `ASPNETCORE_ENVIRONMENT` | App Service setting | Si | Debe quedar en `Production` |
| `Azure__KeyVaultUri` | App Service setting | Si | URI del Key Vault |

### 6.2 Variables recomendadas para un go-live completo

Estas no siempre bloquean el arranque, pero son importantes si usas funciones asociadas:

| Nombre | Donde se configura | Obligatoria | Para que sirve |
|---|---|---|---|
| `vision-endpoint` | Key Vault secret | Depende | Integracion de vision/computer vision |
| `vision-key` | Key Vault secret | Depende | Integracion de vision/computer vision |
| `Azure__Maps__SubscriptionKey` | App Service setting o Key Vault ref | Depende | Azure Maps |
| `ApplicationInsights__ConnectionString` | App Service setting | Opcional | Solo si quieres duplicar la forma clasica de config; Bicep ya inyecta `APPINSIGHTS_CONNECTIONSTRING` |

### 6.3 Variables opcionales para canales e integraciones

Estas pueden dejarse apagadas en un primer lanzamiento si no estan listas:

| Nombre | Donde se configura | Obligatoria | Para que sirve |
|---|---|---|---|
| `whatsapp-phone-number-id` | Key Vault secret | Opcional | Canal WhatsApp |
| `whatsapp-access-token` | Key Vault secret | Opcional | Canal WhatsApp |
| `WhatsApp__AppSecret` | App Service setting o Key Vault ref | Opcional | Validacion HMAC del webhook de Meta |
| `WhatsApp__VerifyToken` | App Service setting o Key Vault ref | Opcional | Validacion inicial del webhook de Meta |
| `telegram-bot-token` | Key Vault secret | Opcional | Canal Telegram |
| `facebook-page-access-token` | Key Vault secret | Opcional | Canal Facebook |
| `facebook-page-id` | Key Vault secret | Opcional | Canal Facebook |
| `Email__SmtpHost` | App Service setting o Key Vault ref | Opcional | Envio email SMTP |
| `Email__SmtpPort` | App Service setting o Key Vault ref | Opcional | Envio email SMTP |
| `Email__SmtpUser` | App Service setting o Key Vault ref | Opcional | Envio email SMTP |
| `Email__SmtpPassword` | App Service setting o Key Vault ref | Opcional | Envio email SMTP |
| `Notifications__Push__Enabled` | App Service setting | Opcional | Habilita proveedor externo de push |
| `Notifications__Push__ProviderUrl` | App Service setting o Key Vault ref | Opcional | URL del proveedor push |
| `Notifications__Push__ApiKey` | App Service setting o Key Vault ref | Opcional | Api key del proveedor push |
| `VITE_VAPID_PUBLIC_KEY` | Build del frontend | Opcional | Necesaria si habilitas suscripcion push en navegador |

### 6.4 Observacion importante sobre push notifications

El frontend usa `VITE_VAPID_PUBLIC_KEY`, pero el backend actual envia push a traves de un proveedor HTTP externo (`Notifications:Push:ProviderUrl`).

Eso significa que para activar push real en produccion necesitas ambas cosas:

1. Un proveedor o servicio real de push web
2. La `VITE_VAPID_PUBLIC_KEY` correspondiente a ese proveedor

Si no tienes ese proveedor listo, deja push apagado en el primer go-live. La app podra salir sin esa funcionalidad.

## 7. Valores sugeridos para produccion

Usa estos valores como base:

| Clave | Valor sugerido |
|---|---|
| `environment` | `prod` |
| `appName` | `pawtrack` |
| `location` | `eastus` |
| `frontendUrl` | `https://pawtrack.cr` o `https://www.pawtrack.cr` |
| `alertEmailAddress` | correo real de operaciones |
| `App__BaseUrl` | `https://pawtrack.cr` |
| `VITE_API_URL` | `https://api.pawtrack.cr` |
| `Cors__AllowedOrigins__0` | `https://pawtrack.cr` |

## 8. Paso 1 - Iniciar sesion en Azure

Abre PowerShell y ejecuta:

```powershell
az login
```

Si tienes varias suscripciones:

```powershell
az account list --output table
az account set --subscription "NOMBRE_O_ID_DE_LA_SUSCRIPCION"
```

Verifica la suscripcion activa:

```powershell
az account show --output table
```

## 9. Paso 2 - Crear el Resource Group

```powershell
az group create --name pawtrack-prod-rg --location eastus
```

Si el grupo ya existe, Azure solo lo confirmara.

## 10. Paso 3 - Desplegar infraestructura base con Bicep

Desde la raiz del repo, define primero el password temporal de SQL para el despliegue:

```powershell
$env:SQL_ADMIN_PASSWORD = "PON_AQUI_UN_PASSWORD_MUY_FUERTE"
```

Ahora despliega la infraestructura:

```powershell
az deployment group create `
  --resource-group pawtrack-prod-rg `
  --template-file infra/main.bicep `
  --parameters infra/parameters.prod.bicepparam `
  --parameters frontendUrl="https://pawtrack.cr" `
  --parameters alertEmailAddress="ops@pawtrack.cr"
```

### Resultado esperado

Al final debes obtener salidas parecidas a estas:

- `appServiceUrl`
- `keyVaultUri`
- `appInsightsConnectionString`
- `storageAccountName`
- `sqlServerFqdn`

Guardalas en un archivo temporal porque las usaras en pasos siguientes.

## 11. Paso 4 - Obtener datos reales creados por Azure

Ejecuta estos comandos para recuperar nombres utiles:

```powershell
az webapp list --resource-group pawtrack-prod-rg --output table
az keyvault list --resource-group pawtrack-prod-rg --output table
az storage account list --resource-group pawtrack-prod-rg --output table
az sql server list --resource-group pawtrack-prod-rg --output table
```

## 12. Paso 5 - Crear el frontend en Azure Static Web Apps

### Recomendacion

Usa Azure Static Web Apps para el frontend. Es la opcion mas simple para una SPA con Vite.

### Opcion A - Portal de Azure

1. Entra a Azure Portal.
2. Busca `Static Web Apps`.
3. Haz clic en `Create`.
4. Llena estos campos:
   - Subscription: tu suscripcion activa
   - Resource Group: `pawtrack-prod-rg`
   - Name: `pawtrack-prod-frontend`
   - Plan: `Standard` recomendado para produccion
   - Region: cercana a tu backend
5. En Deployment details, si solo vas a subir `dist/` manualmente, puedes terminar la creacion y luego desplegar con CLI.

### Opcion B - CLI

Si ya tienes creada la Static Web App, luego deployaras el `dist/` con CLI. Si no la has creado, puedes usar el portal porque es mas amigable para usuarios no tecnicos.

## 13. Paso 6 - Crear y cargar secretos en Key Vault

### 13.1 Obtener el Key Vault real

```powershell
az keyvault list --resource-group pawtrack-prod-rg --output table
```

Asume que el nombre sale como algo parecido a `pawtrack-kv-prod`.

### 13.2 Crear la connection string de SQL

Necesitas construirla con el FQDN real del SQL Server.

Formato sugerido:

```text
Server=tcp:<SQL_SERVER_FQDN>,1433;Initial Catalog=pawtrack;Persist Security Info=False;User ID=pawtrackadmin;Password=<SQL_ADMIN_PASSWORD>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### 13.3 Subir secretos obligatorios

```powershell
az keyvault secret set --vault-name <KEYVAULT_NAME> --name sql-connection-string --value "<SQL_CONNECTION_STRING>"
az keyvault secret set --vault-name <KEYVAULT_NAME> --name jwt-signing-key --value "<JWT_KEY_MUY_LARGA_Y_SEGURA>"
az keyvault secret set --vault-name <KEYVAULT_NAME> --name storage-connection-string --value "<STORAGE_CONNECTION_STRING>"
az keyvault secret set --vault-name <KEYVAULT_NAME> --name appinsights-connection-string --value "<APPINSIGHTS_CONNECTION_STRING>"
```

### 13.4 Como obtener la storage connection string

```powershell
az storage account show-connection-string --name <STORAGE_ACCOUNT_NAME> --resource-group pawtrack-prod-rg --output tsv
```

### 13.5 Secretos opcionales

Solo si la integracion esta lista, sube tambien:

```powershell
az keyvault secret set --vault-name <KEYVAULT_NAME> --name vision-endpoint --value "<VISION_ENDPOINT>"
az keyvault secret set --vault-name <KEYVAULT_NAME> --name vision-key --value "<VISION_KEY>"
az keyvault secret set --vault-name <KEYVAULT_NAME> --name whatsapp-phone-number-id --value "<WHATSAPP_PHONE_NUMBER_ID>"
az keyvault secret set --vault-name <KEYVAULT_NAME> --name whatsapp-access-token --value "<WHATSAPP_ACCESS_TOKEN>"
az keyvault secret set --vault-name <KEYVAULT_NAME> --name telegram-bot-token --value "<TELEGRAM_BOT_TOKEN>"
az keyvault secret set --vault-name <KEYVAULT_NAME> --name facebook-page-access-token --value "<FACEBOOK_PAGE_ACCESS_TOKEN>"
az keyvault secret set --vault-name <KEYVAULT_NAME> --name facebook-page-id --value "<FACEBOOK_PAGE_ID>"
```

## 14. Paso 7 - Configurar App Service del backend

### 14.1 Verificar variables ya inyectadas por Bicep

El Bicep ya intenta configurar en App Service:

- `APPINSIGHTS_CONNECTIONSTRING`
- `ConnectionStrings__DefaultConnection`
- `Azure__Storage__ConnectionString`
- `Jwt__Key`
- `Azure__KeyVaultUri`
- `Cors__AllowedOrigins__0`
- `ASPNETCORE_ENVIRONMENT`

### 14.2 Agregar configuraciones manuales faltantes

Estas configuraciones conviene dejarlas de una vez:

```powershell
az webapp config appsettings set `
  --resource-group pawtrack-prod-rg `
  --name <APP_SERVICE_NAME> `
  --settings `
    ASPNETCORE_ENVIRONMENT=Production `
    App__BaseUrl=https://pawtrack.cr `
    Cors__AllowedOrigins__0=https://pawtrack.cr `
    Azure__KeyVaultUri=https://<KEYVAULT_NAME>.vault.azure.net/
```

### 14.3 Agregar configuraciones opcionales con referencia a Key Vault

Ejemplo para webhook de WhatsApp:

```powershell
az webapp config appsettings set `
  --resource-group pawtrack-prod-rg `
  --name <APP_SERVICE_NAME> `
  --settings `
    WhatsApp__AppSecret="@Microsoft.KeyVault(VaultName=<KEYVAULT_NAME>;SecretName=whatsapp-app-secret)" `
    WhatsApp__VerifyToken="@Microsoft.KeyVault(VaultName=<KEYVAULT_NAME>;SecretName=whatsapp-verify-token)"
```

Haz lo mismo para cualquier variable opcional sensible.

### 14.4 Configuraciones opcionales no sensibles

```powershell
az webapp config appsettings set `
  --resource-group pawtrack-prod-rg `
  --name <APP_SERVICE_NAME> `
  --settings `
    Notifications__Push__Enabled=false
```

Si luego activas el proveedor push, cambia a `true` y agrega `Notifications__Push__ProviderUrl` y `Notifications__Push__ApiKey`.

## 15. Paso 8 - Configurar el frontend para produccion

El frontend necesita variables de build, no solo variables del hosting.

### 15.1 Crear archivo local de build

En `frontend/`, crea temporalmente un archivo `.env.production.local` con este contenido:

```dotenv
VITE_API_URL=https://api.pawtrack.cr
VITE_VAPID_PUBLIC_KEY=<SOLO_SI_PUSH_ESTA_LISTO>
```

Si push no esta listo, puedes dejar solo:

```dotenv
VITE_API_URL=https://api.pawtrack.cr
```

No hagas commit de este archivo.

### 15.2 Build del frontend

```powershell
Set-Location frontend
npm install
npm run build
```

Al final debe existir la carpeta `frontend/dist`.

## 16. Paso 9 - Ejecutar migraciones de base de datos

Haz esto antes del deploy final del backend.

```powershell
Set-Location backend
dotnet ef database update `
  --project src/PawTrack.Infrastructure `
  --startup-project src/PawTrack.API `
  --connection "<SQL_CONNECTION_STRING>"
```

### Resultado esperado

- La base `pawtrack` queda creada y actualizada
- No deben aparecer errores de permisos o columnas faltantes

## 17. Paso 10 - Publicar el backend

Desde `backend/`:

```powershell
dotnet restore
dotnet build
dotnet publish src/PawTrack.API -c Release -o ..\publish\api
```

Comprime la carpeta publicada en un zip. Luego despliega con zip deploy:

```powershell
Compress-Archive -Path ..\publish\api\* -DestinationPath ..\publish\api.zip -Force

az webapp deployment source config-zip `
  --resource-group pawtrack-prod-rg `
  --name <APP_SERVICE_NAME> `
  --src ..\publish\api.zip
```

Despues del deploy, reinicia el App Service:

```powershell
az webapp restart --resource-group pawtrack-prod-rg --name <APP_SERVICE_NAME>
```

## 18. Paso 11 - Publicar el frontend

Desde `frontend/` y con `dist/` ya generado:

```powershell
az staticwebapp deploy `
  --name pawtrack-prod-frontend `
  --resource-group pawtrack-prod-rg `
  --source dist
```

Si tu version de Azure CLI usa otro parametro, consulta la ayuda:

```powershell
az staticwebapp deploy -h
```

## 19. Paso 12 - Configurar dominios personalizados

### 19.1 Recomendacion simple

Usa dos dominios separados:

- `pawtrack.cr` o `www.pawtrack.cr` para frontend
- `api.pawtrack.cr` para backend

### 19.2 Backend App Service

En Azure Portal:

1. Abre el App Service del backend.
2. Ve a `Custom domains`.
3. Agrega `api.pawtrack.cr`.
4. Azure te dira que registro DNS crear.
5. Crea el registro DNS pedido.
6. Cuando valide, agrega HTTPS con certificado administrado por Azure.

### 19.3 Frontend Static Web App

En Azure Portal:

1. Abre la Static Web App.
2. Ve a `Custom domains`.
3. Agrega `pawtrack.cr` o `www.pawtrack.cr`.
4. Azure mostrara el registro DNS necesario.
5. Crea ese registro en tu proveedor DNS.
6. Espera la validacion.
7. Verifica que HTTPS quede activo.

### 19.4 Recomendacion DNS para personas no tecnicas

Si se complica el dominio raiz `pawtrack.cr`, usa este esquema inicial:

- `www.pawtrack.cr` -> frontend
- `api.pawtrack.cr` -> backend
- `pawtrack.cr` -> redireccion a `https://www.pawtrack.cr`

Eso suele ser mas facil que manejar el apex root directamente.

## 20. Paso 13 - Verificacion tecnica minima

Haz estas pruebas en este orden.

### 20.1 Backend responde

```powershell
curl https://api.pawtrack.cr/openapi/v1.json
```

Debes recibir JSON.

### 20.2 Frontend carga

Abre en navegador:

- `https://pawtrack.cr`
- o `https://www.pawtrack.cr`

### 20.3 Login no rompe

Prueba:

- registrar usuario
- iniciar sesion
- cerrar sesion

### 20.4 Fotos suben

Prueba:

- crear mascota
- subir foto
- confirmar que la foto carga desde Blob Storage

### 20.5 Base de datos escribe y lee

Prueba:

- crear mascota
- refrescar pagina
- verificar que sigue existiendo

### 20.6 CORS correcto

Si el frontend no puede llamar a la API, revisa:

- `Cors__AllowedOrigins__0`
- `VITE_API_URL`
- el dominio exacto del frontend

## 21. Bloqueos reales que debes revisar antes del go-live

### 21.1 Health check actual puede generar falso positivo

La infraestructura actual define una prueba de disponibilidad contra `/health`.

En pruebas locales previas, `/health` respondio `401` aunque la API estaba sana. Si en produccion ocurre lo mismo, Azure marcara caidas falsas.

Antes de confiar en la alerta de disponibilidad, valida una de estas dos opciones:

1. Confirmar que `https://api.pawtrack.cr/health` responde `200` sin autenticacion
2. Si no responde `200`, cambiar la prueba a un endpoint realmente publico, por ejemplo un endpoint de health publico o uno equivalente

No ignores este punto. Puede llenar de alertas falsas al equipo.

### 21.2 Frontend no esta automatizado por el Bicep actual

Eso no impide salir a produccion, pero significa que el frontend requiere un paso manual adicional.

### 21.3 Push puede quedar deshabilitado en primera salida

No bloquea el go-live si documentas que el canal push queda para fase 2.

## 22. Checklist de go-live

Marca cada punto solo cuando este hecho de verdad.

- Suscripcion Azure correcta
- Resource Group creado
- Bicep desplegado sin errores
- Key Vault con secretos obligatorios cargados
- App Service con settings obligatorios configurados
- SQL migrado
- Backend publicado
- Frontend publicado
- Dominio frontend funcionando con HTTPS
- Dominio API funcionando con HTTPS
- Login funcionando
- Registro funcionando
- Subida de fotos funcionando
- CORS validado
- Alertas configuradas
- Health check validado

## 23. Checklist de rollback

Si algo sale mal durante el lanzamiento, haz esto:

1. No borres infraestructura.
2. Revierte primero frontend a la ultima build estable.
3. Si el problema es backend, vuelve a desplegar el ultimo zip estable.
4. Si el problema es de configuracion, corrige App Settings y reinicia App Service.
5. Si el problema es de base de datos, no edites tablas manualmente sin respaldo.
6. Si una migracion rompio algo, detente y revisa antes de seguir.

## 24. Prompts listos para usar con IA

Puedes copiar estos prompts en GitHub Copilot, ChatGPT o Claude mientras haces el proceso.

### Prompt 1 - Ayudame a desplegar infraestructura en Azure

```text
Estoy desplegando PawTrack CR en Azure desde un repo con Bicep. Quiero que me guies paso por paso, sin asumir experiencia tecnica. Estoy en Windows y usare Azure CLI. Mi resource group es pawtrack-prod-rg. Quiero que revises cada comando antes de ejecutarlo y me digas que salida debo esperar.
```

### Prompt 2 - Ayudame a cargar secretos en Key Vault

```text
Estoy configurando Key Vault para una app .NET y React llamada PawTrack CR. Necesito que me ayudes a cargar secretos uno por uno en Azure, explicando cuales son obligatorios y cuales opcionales. Quiero usar nombres exactos como sql-connection-string, jwt-signing-key, storage-connection-string y appinsights-connection-string.
```

### Prompt 3 - Ayudame a configurar App Service

```text
Estoy configurando Azure App Service para un backend .NET de PawTrack CR. Quiero una lista exacta de app settings que debo revisar y agregar manualmente. El frontend quedara en https://pawtrack.cr y la API en https://api.pawtrack.cr. Explicame como configurar CORS, App__BaseUrl y referencias a Key Vault.
```

### Prompt 4 - Ayudame a publicar el frontend

```text
Estoy publicando un frontend Vite/React en Azure Static Web Apps. Necesito que me indiques como preparar las variables de build, como crear .env.production.local sin cometerlo al repo, como correr npm run build y como desplegar dist paso por paso.
```

### Prompt 5 - Ayudame con DNS y HTTPS

```text
Estoy conectando pawtrack.cr y api.pawtrack.cr en Azure. Quiero instrucciones simples para configurar DNS, validar dominios personalizados y dejar HTTPS activo tanto en Azure Static Web Apps como en Azure App Service.
```

### Prompt 6 - Validacion final antes de salir

```text
Actua como un release manager tecnico. Tengo PawTrack CR desplegado en Azure. Dame un checklist final de go-live para validar frontend, backend, SQL, Blob Storage, CORS, login, subida de fotos, dominios, HTTPS, monitoreo y rollback. Quiero una respuesta operativa, corta y verificable.
```

## 25. Secuencia rapida si quieres hacerlo en un solo dia

Si necesitas una version ejecutiva, sigue este orden exacto:

1. `az login`
2. Crear `pawtrack-prod-rg`
3. Desplegar `infra/main.bicep`
4. Crear Static Web App manualmente
5. Cargar secretos obligatorios en Key Vault
6. Configurar App Service settings obligatorios
7. Crear `.env.production.local` para frontend
8. Ejecutar migracion SQL
9. Publicar backend
10. Publicar frontend
11. Configurar dominios y HTTPS
12. Validar login, CRUD basico y fotos
13. Verificar alertas y health check

## 26. Definicion de terminado

PawTrack CR puede considerarse realmente live cuando se cumplan estas tres condiciones al mismo tiempo:

1. El frontend abre por dominio publico con HTTPS.
2. La API responde por dominio publico y el frontend puede usarla sin errores de CORS.
3. Un flujo real de negocio funciona de punta a punta: registro o login, creacion de mascota y carga de foto.

Si una de esas tres falla, todavia no estas live.
