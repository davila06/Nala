# Infraestructura PawTrack

`main.bicep` es la unica fuente de infraestructura desplegable. Los templates
ARM JSON son artefactos generados y no deben versionarse ni desplegarse.

## Validacion

```powershell
az bicep build --file infra/main.bicep --stdout | Out-Null
az deployment group what-if `
  --resource-group <resource-group> `
  --template-file infra/main.bicep `
  --parameters infra/parameters.prod.bicepparam `
  --parameters sqlAdminPassword=$env:SQL_ADMIN_PASSWORD
```

Produccion usa red privada para SQL, Blob Storage y Key Vault, una identidad
administrada compartida para API y migraciones, y Azure Front Door Premium con
WAF como entrada publica. Las migraciones se ejecutan mediante el Container Apps
Job antes de promover una revision nueva.
