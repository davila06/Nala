# Prompt reutilizable: revisión GPS/IoT

## Entrada

- Collar/proveedor/flujo: `<indicar>`
- Ambiente/dispositivo y periodo observado: `<indicar>`
- Prueba/evidencia operativa disponible: `<indicar>`

## Instrucciones

Traza alta y ownership de collar, ingest/polling/webhook, validación de coordenadas, persistencia, consulta de historial, alertas, geofence, retención, rate limits y UI. Verifica idempotencia, replay, autenticidad de webhook/device key y fallos desconectados. Distingue código/DI de contrato, hardware, conectividad y datos vivos.

Usa [dominio GPS](../03_domains/gps.md), [provider matrix](../../docs/domains/GPS_PROVIDER_MATRIX.md), [retención](../../docs/MATRIZ_RETENCION_DATOS.md) y [guardrails](../06_security/SECURITY_BASELINE.md). No inventes cobertura, autonomía, precisión o batería.

## Salida

Flujo probado, nivel de evidencia, datos GPS expuestos/retención, findings priorizados, pruebas mínimas, proveedor/firmware no verificado y decisión/owner pendiente.
