# Runbook de Jobs en Background

**Estado:** activo  
**Audiencia:** DevOps y desarrolladores backend  
**Corte:** 2026-09-09

## Regla comun

Los jobs deben usar `IDistributedJobLock` al operar en varias instancias,
respetar cancelacion, registrar duracion/errores y ser idempotentes. La logica
se prueba en servicios inyectables; el wrapper `BackgroundService` no es la
unidad de negocio.

## Jobs principales

| Job/servicio                     | Funcion                                                 | Operacion                         |
| -------------------------------- | ------------------------------------------------------- | --------------------------------- |
| Outbox processor                 | publica eventos pendientes                              | revisar retries y poison messages |
| Personal data retention          | purga sightings, chats cerrados y notificaciones leidas | diario 03:00 CR                   |
| QR scan retention                | limita historial QR                                     | ventana configurada               |
| Collar connectivity              | alertas offline/bateria                                 | cada 15 min + cooldown            |
| Tractive polling                 | ingesta de GPS                                          | 30 s lost mode / 5 min normal     |
| Collar location purge            | borra ubicaciones antiguas                              | retencion de 30 dias              |
| Provider trial expiration        | baja trial vencido                                      | diario                            |
| Provider verification expiration | marca verificaciones vencidas                           | programado                        |
| Booking expiration/reminders     | expira y recuerda reservas                              | programado                        |
| Subscription expiration/renewal  | actualiza vigencias y avisos                            | programado                        |
| Embedding refresh                | regenera embeddings pendientes                          | programado                        |
| Regulatory export                | genera exports solicitados                              | lock distribuido                  |
| Webhook delivery                 | entrega y reintenta webhooks                            | outbox/fanout                     |

## Diagnostico

1. Revisar logs por nombre del job y correlation ID.
2. Confirmar que el lock no quede retenido.
3. Verificar excepciones y conteos de filas afectadas.
4. Reprocesar solo si la operacion es idempotente.
5. Comparar estado antes/despues y documentar el incidente.

No ejecutar el mismo job manualmente en dos instancias ni borrar tablas de
outbox, auditoria o retencion para "destrabar" un ciclo.
