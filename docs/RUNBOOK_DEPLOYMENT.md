# Runbook Canonico de Deployment

**Estado:** activo  
**Audiencia:** DevOps y mantenedores  
**Corte:** 2026-09-09

## Arquitectura vigente

- Frontend: Azure Static Web Apps.
- Backend: Azure App Service Linux o el recurso equivalente definido por la
  infraestructura desplegada; no asumir Container Apps por documentos antiguos.
- Datos: Azure SQL.
- Archivos: Azure Blob Storage.
- Secretos: Azure Key Vault.
- Telemetria: Application Insights y Log Analytics.

Confirmar los nombres reales con Azure y `infra/` antes de ejecutar comandos.
Nunca copiar contrasenas, connection strings o tokens a este documento.

## Preflight

```powershell
dotnet restore
dotnet build PawTrack.sln --no-restore
npm install --legacy-peer-deps # desde frontend si el lockfile lo requiere
npm run typecheck              # desde frontend
npm test                       # desde frontend
npm run build                  # desde frontend
```

Validar `git diff --check`, migraciones pendientes, secretos en Key Vault,
CORS, `App__BaseUrl`, `VITE_API_URL`, health checks y workflow CI.

## Secuencia

1. Validar infraestructura Bicep y parámetros por ambiente.
2. Aplicar infraestructura con identidad federada, no credenciales permanentes.
3. Confirmar identidad administrada y acceso minimo a Key Vault.
4. Configurar secretos y variables sin imprimir valores.
5. Aplicar migraciones EF Core con ventana controlada.
6. Construir y publicar backend con tag inmutable.
7. Construir y publicar frontend con la URL API correcta por ambiente.
8. Configurar dominio, DNS, HTTPS y CORS.
9. Ejecutar health, smoke, autenticacion y un flujo de negocio.
10. Habilitar alertas y registrar version, migraciones y rollback.

## Rollback

- Backend: volver a la imagen anterior despues de verificar compatibilidad de
  esquema.
- Frontend: redeploy del artifact anterior.
- Base de datos: no revertir migraciones destructivamente; restaurar backup o
  aplicar migracion compensatoria.
- Secretos: rotar y reiniciar solo el componente afectado.

## Verificacion final

`/health`, `/health/ready`, login, registrar mascota, QR publico, upload de
foto, reporte de perdida, una lectura de base de datos y una alerta de
Application Insights deben funcionar. Usar [GUIA_QA_E2E.md](GUIA_QA_E2E.md).

Los procedimientos históricos están en
[GUIA_DEPLOY_PASO_A_PASO.md](GUIA_DEPLOY_PASO_A_PASO.md) y
[DEPLOY_INFO.md](DEPLOY_INFO.md); no ejecutar sus valores sin validarlos.
