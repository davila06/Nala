# Rollout Sprint 5

## Gates técnicos

- Backend `dotnet build` sin errores.
- Unit tests Regulatory y autorización sin fallos.
- Integration tests de 401/403/BOLA pasando.
- Frontend `npm run build` sin errores.
- Playwright institucional ejecutado contra backend y datos sintéticos.
- Migración `AddRegulatoryExportsAndNala` y `AddRegulatorySubmissionPayloadBlob`
  aplicadas en una base de prueba nueva y una copia de staging.
- Rollback probado en copia de staging, nunca editando migraciones aplicadas.

## Feature flags

- `Features__RegulatoryReportsEnabled=false` deshabilita el portal y endpoints de
  exports en el despliegue inicial.
- `Features__NalaDashboardEnabled=false` deshabilita overview, tendencias y mapa.
- Activar primero para Admin/NALA, después para un cantón piloto.

## Piloto

1. Seleccionar uno o dos cantones y una organización operadora.
2. Cargar datos sintéticos y verificar suppression.
3. Generar PDF/CSV/JSON y comparar hash/filas.
4. Revisar tiempos p50/p95 y errores Blob.
5. Revisar scopes, descargas y auditoría.
6. Recibir aprobación de producto, seguridad y operación.
7. Ampliar gradualmente el scope.

## Azure

La infraestructura Bicep crea contenedores privados para `regulatory-exports`,
`welfare-evidence` y `verification-documents`, además de flags de despliegue.
Las alertas estándar cubren disponibilidad, 5xx, latencia, 401 y 429. Las
alertas de negocio requieren Workbook/KQL en Log Analytics para:

- exports fallidos o atascados;
- hash mismatch;
- errores Blob;
- suppression anómala;
- conflictos críticos de bienestar sin triage;
- cache hit/miss;
- abuso de preview/export.

## Rollback

- Desactivar los feature flags.
- Mantener los endpoints existentes de PawTrack operativos.
- No eliminar tablas Regulatory durante un rollback de aplicación.
- Conservar Blobs y auditoría para investigación.
- Revertir migraciones solo en bases de prueba; en ambientes compartidos usar
  una migración compensatoria aprobada.

## Aprobación final

Sprint 5 no se anuncia como integración SENASA. El go-live requiere evidencia de
los gates, aprobación legal de privacidad/lenguaje, revisión de costos y
aceptación del piloto institucional.
