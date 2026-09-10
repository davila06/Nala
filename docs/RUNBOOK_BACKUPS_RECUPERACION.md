# Runbook de Backups y Recuperacion

**Estado:** activo para definir operacion  
**Audiencia:** DevOps y Admin de plataforma  
**Corte:** 2026-09-09

## Objetivo

Recuperar el servicio sin improvisar despues de perdida de datos, fallo de
migracion, corrupcion, caida regional o eliminacion accidental.

## Inventario de recuperacion

| Componente   | Fuente de restauracion                          | Verificacion                         |
| ------------ | ----------------------------------------------- | ------------------------------------ |
| Azure SQL    | backups automaticos y point-in-time restore     | consulta de migraciones y health     |
| Blob Storage | redundancia y versioning/politicas configuradas | descarga de objeto de prueba         |
| Key Vault    | backup/restore de secretos y rotacion           | lectura desde identidad administrada |
| Backend      | imagen inmutable en ACR y commit                | `/health` y `/health/ready`          |
| Frontend     | build reproducible y deployment artifact        | smoke de rutas publicas              |

## Antes de restaurar

1. Abrir incidente y congelar despliegues.
2. Identificar hora buena conocida y alcance del daño.
3. Confirmar que existe una copia util y que no contiene secretos expuestos.
4. Avisar a los responsables del tenant si hay riesgo de perdida de datos.
5. No ejecutar migraciones destructivas sobre produccion sin respaldo y plan de rollback.

## Recuperacion SQL

- Preferir point-in-time restore a una base temporal.
- Validar `__EFMigrationsHistory` y conteos de tablas criticas.
- Ejecutar smoke de login, mascotas, perdida, clinica y reportes.
- Cambiar la cadena de conexion solo mediante Key Vault y despliegue controlado.
- No copiar datos productivos a entornos de desarrollo sin anonimizar.

## Recuperacion de blobs

- Verificar contenedor, permisos y content type.
- Restaurar primero objetos privados de evidencia y documentos.
- Confirmar que los contenedores publicos solo sean los permitidos por
  `BlobStorageService`.

## Criterios de cierre

El incidente se cierra cuando health checks, migraciones, autenticacion, carga
de fotos y una transaccion critica por modulo pasan. Documentar RTO real,
perdida de datos, causa raiz y acciones preventivas.

Complementa [OBSERVABILITY_SLO_RUNBOOK.md](OBSERVABILITY_SLO_RUNBOOK.md) y
[GUIA_DEPLOY_PASO_A_PASO.md](GUIA_DEPLOY_PASO_A_PASO.md).
