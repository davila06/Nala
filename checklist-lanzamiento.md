# Checklist de lanzamiento — PawTrack CR en Azure

Marca cada paso solo cuando este verificado. Si algo falla, para y corrige antes de seguir.

---

## Fase 1 — Infraestructura

- [ ] `az login` exitoso con la suscripcion correcta
- [ ] Resource group `pawtrack-prod-rg` creado
- [ ] Bicep desplegado sin errores
  ```powershell
  $env:SQL_ADMIN_PASSWORD = "PASSWORD_FUERTE"
  az deployment group create `
    --resource-group pawtrack-prod-rg `
    --template-file infra/main.bicep `
    --parameters infra/parameters.prod.bicepparam `
    --parameters alertEmailAddress="ops@pawtrack.cr"
  ```
- [ ] Outputs guardados (App Service URL, Key Vault URI, Storage, SQL FQDN, SWA URL/Name)
  ```powershell
  az deployment group show `
    --name main --resource-group pawtrack-prod-rg `
    --query properties.outputs -o table
  ```

---

## Fase 2 — Secretos en Key Vault

- [ ] `sql-connection-string` cargado
- [ ] `jwt-signing-key` cargado (minimo 32 caracteres aleatorios)
- [ ] `storage-connection-string` cargado
- [ ] `appinsights-connection-string` cargado

  ```powershell
  # Obtener nombre del Key Vault
  az keyvault list --resource-group pawtrack-prod-rg --query "[0].name" -o tsv

  # Obtener storage connection string
  az storage account show-connection-string `
    --name <STORAGE_ACCOUNT_NAME> --resource-group pawtrack-prod-rg -o tsv
  ```

---

## Fase 3 — Configuracion del backend (App Service)

- [ ] `ASPNETCORE_ENVIRONMENT` = `Production`
- [ ] `App__BaseUrl` = URL publica del frontend (ej. `https://pawtrack.cr`)
- [ ] `Cors__AllowedOrigins__0` = URL publica del frontend (ej. `https://pawtrack.cr`)
- [ ] `Azure__KeyVaultUri` = URI del Key Vault

  ```powershell
  az webapp config appsettings set `
    --resource-group pawtrack-prod-rg --name <APP_SERVICE_NAME> `
    --settings `
      ASPNETCORE_ENVIRONMENT=Production `
      App__BaseUrl=https://pawtrack.cr `
      Cors__AllowedOrigins__0=https://pawtrack.cr `
      Azure__KeyVaultUri=https://<KEYVAULT_NAME>.vault.azure.net/
  ```

---

## Fase 4 — Base de datos

- [ ] Migracion SQL ejecutada sin errores

  ```powershell
  cd backend
  dotnet ef database update `
    --project src/PawTrack.Infrastructure `
    --startup-project src/PawTrack.API `
    --connection "<SQL_CONNECTION_STRING>"
  ```

---

## Fase 5 — Build y deploy backend

- [ ] Build exitoso
- [ ] Zip deploy completado
- [ ] App Service reiniciado

  ```powershell
  cd backend
  dotnet publish src/PawTrack.API -c Release -o ..\publish\api
  Compress-Archive -Path ..\publish\api\* -DestinationPath ..\publish\api.zip -Force
  az webapp deployment source config-zip `
    --resource-group pawtrack-prod-rg --name <APP_SERVICE_NAME> --src ..\publish\api.zip
  az webapp restart --resource-group pawtrack-prod-rg --name <APP_SERVICE_NAME>
  ```

---

## Fase 6 — Build y deploy frontend

- [ ] `.env.production.local` creado en `frontend/` (NO hacer commit)
  ```dotenv
  VITE_API_URL=https://api.pawtrack.cr
  ```
- [ ] Build exitoso (`npm run build` genera `dist/`)
- [ ] Deploy a Static Web App completado

  ```powershell
  cd frontend
  npm install
  npm run build

  # Obtener deployment token
  az staticwebapp secrets list --name <SWA_NAME> --resource-group pawtrack-prod-rg --query "properties.apiKey" -o tsv

  # Desplegar con SWA CLI
  npx @azure/static-web-apps-cli deploy ./dist `
    --deployment-token <TOKEN>
  ```

---

## Fase 7 — Dominios y HTTPS

- [ ] Dominio `api.pawtrack.cr` configurado en App Service con certificado Azure (Managed Certificate)
- [ ] Dominio `pawtrack.cr` o `www.pawtrack.cr` configurado en Static Web App
- [ ] HTTPS activo en ambos dominios sin advertencias
- [ ] `App__BaseUrl`, `VITE_API_URL` y `Cors__AllowedOrigins__0` apuntan a los dominios reales
  > Despues de asignar dominios reales, vuelve a correr el Bicep con `frontendUrl` actualizado en `parameters.prod.bicepparam`

---

## Fase 8 — Verificacion funcional

- [ ] `https://api.pawtrack.cr/openapi/v1.json` responde 200
- [ ] Frontend carga en `https://pawtrack.cr`
- [ ] Registro de usuario funciona
- [ ] Login funciona
- [ ] Creacion de mascota funciona
- [ ] Subida de foto funciona (foto carga desde URL de Blob Storage)
- [ ] Sin errores CORS en consola del navegador

---

## Fase 9 — Monitoreo

- [ ] Application Insights recibe telemetria (verificar en Azure Portal → App Insights → Live Metrics)
- [ ] `/health` responde 200 publica — si da 401, cambiar la prueba de disponibilidad de Bicep a un endpoint realmente publico (ej. `/openapi/v1.json`)
- [ ] Alertas de Monitor configuradas (Bicep ya las crea; verificar que el email ops recibe prueba)

---

## Definicion de terminado

PawTrack CR esta **live** cuando se cumplen las tres condiciones simultaneamente:

1. Frontend abre por dominio publico con HTTPS
2. API responde por dominio publico sin errores CORS
3. Flujo completo funciona: login → crear mascota → subir foto

---

*Guia detallada: [pasos para ir live.md](pasos%20para%20ir%20live.md)*
