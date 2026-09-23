# PawTrack: observabilidad y SLO operativos

## Alcance

Este runbook cubre los indicadores tecnicos que ya se emiten desde el backend:

- `pawtrack.webhook.queued`
- `pawtrack.webhook.delivered`
- `pawtrack.webhook.failed`
- `pawtrack.webhook.delivery.duration_ms`
- `pawtrack.medical.exported`
- `pawtrack.api.requests`
- `pawtrack.api.errors`
- `pawtrack.api.request.duration_ms`

El health check `external-provider-configuration` valida la presencia de
configuración no secreta necesaria para Azure Storage, Application Insights,
correo y canales de broadcast. No imprime valores y no forma parte del probe
`/health/ready` hasta que el entorno tenga evidencia contractual aprobada.

La instrumentacion se publica en el `Meter` `PawTrack.Enterprise`. El colector
de produccion debe exportar ese Meter a Azure Monitor/OpenTelemetry; el codigo
no asume que Application Insights capture automaticamente todos los `Meter`.
Las métricas HTTP usan solo método, primer segmento de ruta y clase de estado
para evitar cardinalidad peligrosa; no incluyen usuario, email, token ni PII.

## Objetivos iniciales

| Servicio              |                 SLO inicial | Ventana  | Alerta recomendada          |
| --------------------- | --------------------------: | -------- | --------------------------- |
| API publica y Partner |        99.9% disponibilidad | 30 dias  | burn rate 14x/1h o 6x/6h    |
| Webhooks salientes    |       99% entregas exitosas | 24 horas | fallos >= 5% durante 15 min |
| Webhooks salientes    |                   p95 < 2 s | 24 horas | p95 >= 2 s durante 15 min   |
| Exports clinicos      | 99% solicitudes completadas | 30 dias  | fallos >= 1% durante 30 min |
| Primera respuesta     |     75% en menos de 6 horas | 30 dias  | SLO < 70% por canton        |
| Reunificacion         |              mediana < 72 h | 90 dias  | degradacion > 20% mensual   |

## Analitica de producto

`GET /api/v1/product-events/performance` requiere rol `Admin` y acepta `from`,
`to` y `canton`. La consulta agrega en SQL y devuelve cohortes mensuales con:

- mascotas registradas y activadas;
- reportes de perdida y reunificaciones;
- conversion de activacion y recuperacion;
- minutos medianos y p90 hasta primera respuesta y reunificacion, calculados por incidente de pérdida;
- porcentaje de primeras respuestas dentro del SLO de seis horas.

El rango máximo es 366 días. `Sin especificar` se mantiene como grupo separado
para que la ausencia de cantón sea visible y no infle silenciosamente otro
territorio.

Definiciones:

$$
Activacion = \frac{mascotas\ con\ perfil\ completo\ o\ QR\ activo}{mascotas\ registradas}
$$

$$
Recuperacion = \frac{casos\ con\ handover\ o\ reunificacion}{reportes\ de\ perdida}
$$

## Consultas KQL de referencia

```kusto
customMetrics
| where name in (
    "pawtrack.webhook.queued",
    "pawtrack.webhook.delivered",
    "pawtrack.webhook.failed")
| summarize total=sum(value) by name, bin(timestamp, 5m)
| order by timestamp asc
```

```kusto
customMetrics
| where name == "pawtrack.webhook.delivery.duration_ms"
| summarize p50=percentile(value, 50), p95=percentile(value, 95),
            p99=percentile(value, 99) by bin(timestamp, 5m)
| order by timestamp asc
```

## Respuesta operativa

1. Confirmar si el incidente es de API, cola, destino webhook o credenciales.
2. Revisar `WebhookQueued`, `WebhookFailed` y duracion p95 en la ultima hora.
3. Consultar el estado de las entregas y el ultimo error persistido antes de
   reintentar manualmente.
4. Deshabilitar temporalmente solo la suscripcion afectada si el destino esta
   devolviendo errores permanentes; no borrar entregas ni firmas.
5. Reprocesar unicamente entregas idempotentes y documentar el `IdempotencyKey`.
6. Registrar causa raiz, duracion, impacto y accion correctiva en el incidente.

## Limites de evidencia

La existencia de este documento no prueba disponibilidad 99.9%. La validacion
del SLO requiere un workspace de Azure Monitor configurado, exportacion del
Meter, alertas activas y una primera ventana de observacion con datos reales.

## IaC Azure

`infra/main.bicep` provisiona el workspace de Log Analytics, Application
Insights, Action Group, alertas HTTP/latencia/disponibilidad, diagnósticos del
Container App y una clave RSA de Key Vault para firma detached. La identidad
administrada del Container App recibe `Key Vault Secrets User` y `Key Vault
Crypto User`.

La validación local del contrato B2B se ejecutó con frontend preview, backend
local, SQL Server LocalDB y Azurite: aislamiento Store/Clinic 1/1. La suite de
concurrencia WebAuthn en SQL Server también pasó 1/1.

El `what-if` Azure no se ejecutó: la sesión CLI activa pertenece a otra
suscripción/tenant y el resource group histórico `PawnTrackBeta` no existe en
ese contexto. Antes de continuar se debe seleccionar explícitamente la
suscripción correcta y confirmar el resource group destino.

Validacion local ejecutada:

```powershell
az bicep build --file infra/main.bicep --stdout
```

El despliegue requiere elegir explícitamente el resource group y ejecutar un
`what-if` con parámetros seguros antes de `az deployment group create`. No se
deben reutilizar recursos de otra aplicación ni compartir la clave de firma.
