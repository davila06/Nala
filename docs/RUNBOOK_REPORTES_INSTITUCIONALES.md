# Runbook — Reportes Institucionales y NALA

## Alcance

Los reportes de Sprint 5 son `SENASA-ready`: compatibles, auditables y
privados. No constituyen integración oficial, aprobación ni envío a SENASA.

## Flujo operativo

1. Un usuario institucional solicita un export con periodo, alcance, formato y
   `IdempotencyKey`.
2. El API crea el registro en estado `Requested` y devuelve el `ExportCode`.
3. `RegulatoryExportHostedService` toma un lock distribuido y procesa lotes de
   hasta 10 exports pendientes.
4. El renderer genera JSON, CSV o PDF; el sistema calcula SHA-256 y almacena el
   archivo en el contenedor privado `regulatory-exports`.
5. El export pasa a `Completed` y puede descargarse únicamente por su propietario
   autorizado.
6. El job elimina Blobs vencidos, marca el export como `Expired` y registra la
   acción en auditoría.

## Diagnóstico

- Buscar por `ExportCode` o `ExportId` en `RegulatoryExports`.
- Revisar `Status`, `ErrorCode`, `RequestedAt`, `StartedAt`, `CompletedAt` y
  `ExpiresAt`.
- Consultar `AuditLog` con `EntityType = RegulatoryExport`.
- Comparar `PayloadSha256` con el SHA-256 del archivo descargado.
- Confirmar que el contenedor `regulatory-exports` no tenga acceso anónimo.

## Fallos comunes

### Export atascado en `Requested`

Revisar el hosted service, el lock distribuido y la salud de la base de datos.
No ejecutar generación concurrente manual con la misma exportación.

### Export en `Failed`

Revisar `ErrorCode` y los eventos `RegulatoryExportFailed`. Corregir la causa y
solicitar un nuevo export con una nueva clave de idempotencia.

### Hash no coincide

Suspender la descarga del archivo, conservar metadata y Blob, y escalar como
incidente de integridad. No sobrescribir el Blob original.

### Export expirado

Solicitar una nueva generación. Los exports expirados no deben reactivarse ni
volver a exponerse desde el endpoint de descarga.

### Regeneración segura

No reutilizar un Blob ni una clave de idempotencia cuando cambien filtros,
fórmula o versión de schema. Solicitar un nuevo export, conservar el hash del
archivo anterior y registrar el motivo de regeneración.

### Cancelación de job

Cancelar mediante el endpoint autorizado de submission o el mecanismo operativo
aprobado. Después de cancelar, revisar el estado del export y su auditoría; no
ejecutar una segunda generación concurrente con el mismo registro.

### Blob faltante

Registrar `ExportCode`, revisar el Blob privado y el evento de finalización. No
sobrescribir el archivo ni alterar el hash; generar un nuevo export si procede.

### Escalamiento

- Alta: acceso indebido, PII expuesta, hash mismatch o Blob público.
- Media: cola atascada, errores repetidos de Blob o p95 fuera del SLA.
- Baja: export individual fallido con retry disponible.

Registrar solo IDs operativos y métricas. Nunca incluir payloads, tokens, SAS o PII.

## Privacidad

- No colocar PII, tokens, SAS, URLs privadas ni payloads en logs.
- Los datos públicos deben permanecer agregados y sujetos a suppression.
- Las coordenadas exactas, reportantes, evidencia y datos clínicos no pertenecen
  en exports públicos.

## Integración externa

Sprint 5 no envía información oficialmente a SENASA. Cualquier gateway externo
debe permanecer deshabilitado hasta contar con convenio, canal aprobado,
requisitos técnicos y revisión legal.
