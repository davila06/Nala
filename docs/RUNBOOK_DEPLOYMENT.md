# Runbook Canonico de Deployment

**Estado:** activo  
**Audiencia:** DevOps y mantenedores  
**Corte:** 2026-09-09

## Arquitectura vigente

- Frontend: Azure Static Web Apps.
- Backend: Azure Container Apps Linux, definido en `infra/main.bicep`.
- Datos: Azure SQL.
- Archivos: Azure Blob Storage.
- Secretos: Azure Key Vault.
- Telemetria: Application Insights y Log Analytics.

La API mantiene una replica en produccion para QR y procesos programados. El
maximo es de tres replicas para el volumen MVP; no ampliarlo sin configurar
Redis y Azure SignalR, que preservan estado distribuido y tiempo real durante
el scale-out. Confirmar los nombres reales con Azure y `infra/` antes de ejecutar comandos.
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
5. Validar expand-contract y ejecutar el Container Apps Job privado de migraciones.
6. Construir y publicar backend con tag inmutable.
7. Construir y publicar frontend con la URL API correcta por ambiente.
8. Configurar dominio, DNS, HTTPS y CORS.
9. Ejecutar health, smoke, autenticacion y un flujo de negocio.
10. Habilitar alertas y registrar version, migraciones y rollback.

## Rollback

- Backend: el workflow conserva la revision anterior y revierte el trafico
  automaticamente si `/health/ready` no queda verde.
- Frontend: redeploy del artifact anterior.
- Base de datos: cada release solo puede expandir el esquema. `DropColumn`,
  `DropTable`, `RenameColumn`, `RenameTable` y `AlterColumn` quedan bloqueados
  en `Up()` por CI. El cleanup ocurre en una release posterior, cuando ninguna
  revision antigua dependa del contrato anterior. Ante un defecto, desplegar
  una migracion compensatoria; restaurar backup solo bajo incidente declarado.
- Secretos: rotar y reiniciar solo el componente afectado.

## Verificacion final

`/health`, `/health/ready`, login, registrar mascota, QR publico, upload de
foto, reporte de perdida, una lectura de base de datos y una alerta de
Application Insights deben funcionar. Usar [GUIA_QA_E2E.md](GUIA_QA_E2E.md).

Los procedimientos históricos están en
[GUIA_DEPLOY_PASO_A_PASO.md](GUIA_DEPLOY_PASO_A_PASO.md) y
[DEPLOY_INFO.md](DEPLOY_INFO.md); no ejecutar sus valores sin validarlos.
