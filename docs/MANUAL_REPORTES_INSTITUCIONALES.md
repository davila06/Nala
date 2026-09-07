# Manual de Reportes Institucionales

## Alcance

Los reportes de PawTrack CR/NALA son `SENASA-ready`: agregados, auditables y
compatibles con procesos institucionales. No son certificados oficiales ni
representan un envío a SENASA.

## Solicitar un reporte

1. Ingresa a `/reportes-institucionales` con un rol institucional autorizado.
2. Selecciona el tipo de reporte y formato PDF, CSV o JSON.
3. Define un periodo de hasta 366 días y el cantón autorizado cuando aplique.
4. Solicita el export. La solicitud es idempotente mediante una clave interna.
5. Usa **Generar** para iniciar el procesamiento o espera al job distribuido.
6. Descarga únicamente cuando el estado sea `Completed`.

Los previews, cuando se habiliten para el flujo, están limitados a periodos
cortos y nunca incluyen PII, evidencia ni coordenadas exactas.

## Estados

- `Requested`: solicitud registrada.
- `Running`: generación en proceso.
- `Completed`: archivo privado listo para descarga.
- `Failed`: generación fallida; revisar el código seguro y solicitar nuevamente.
- `Expired`: archivo purgado; debe generarse un nuevo export.

## Privacidad y suppression

Los usuarios municipales solo pueden consultar cantones autorizados. Las
métricas públicas y los grupos pequeños se muestran como `Suppressed` para
reducir riesgo de reidentificación. No se deben copiar datos agregados para
inferir casos individuales.

## Integración externa

La preparación de submissions registra el paquete, hash y Blob privado, pero el
gateway actual es `NoOp`: no existe envío externo automático. No usar lenguaje
como “enviado a SENASA” sin convenio y canal aprobado.

## Incidentes

Para un hash que no coincide, Blob faltante, export atascado o acceso indebido,
conservar `ExportCode`, `ExportId`, hora y usuario, y seguir el
[runbook operativo](RUNBOOK_REPORTES_INSTITUCIONALES.md). No incluir tokens, SAS,
PII ni payloads completos en tickets o logs.
