# Matriz E2E de pagos

**Estado:** contratos locales implementados; ejecución contra sandbox CyberSource `NO_VERIFICADO`. El proyecto E2E actual no tiene un host de pagos conectado, por lo que esta matriz es un gate de ejecución y no evidencia de proveedor real.

| Escenario                        | Resultado esperado                                             | Estado actual                                        |
| -------------------------------- | -------------------------------------------------------------- | ---------------------------------------------------- |
| Pago aprobado                    | `PaymentIntent=Authorized`; no fulfillment antes de settlement | Cubierto por unit/integration con gateway controlado |
| Pago declinado                   | Intent `Declined`; no ledger Debit ni beneficio                | Cubierto por unit                                    |
| Token inválido                   | Intent `Failed`; no llamada de captura                         | Cubierto por unit de handler/gateway                 |
| Proveedor no configurado         | `PROVIDER_NOT_CONFIGURED`; no éxito simulado                   | Cubierto                                             |
| Timeout/error de red             | Operación `Unknown`; retry con misma clave seguro              | Parcial; sandbox pendiente                           |
| Retry                            | Misma operación y mismo resultado                              | Cubierto por replay ledger                           |
| Doble click                      | Una clave, una operación de proveedor                          | Cubierto por índices/replay; E2E pendiente           |
| Webhook inválido                 | HTTP 401 y ningún cambio                                       | Código implementado; test HTTP pendiente             |
| Webhook duplicado                | 200 idempotente, un `WebhookReceived`                          | Código implementado; E2E pendiente                   |
| Webhook fuera de orden           | No retroceder estado; registrar divergencia                    | Parcial; prueba HTTP pendiente                       |
| Capture repetido                 | No segunda llamada al proveedor                                | Cubierto por operación ledger                        |
| Refund mayor al disponible       | Rechazo sin mutar intent ni ledger                             | Cubierto por dominio/command                         |
| Fulfillment antes de `Settled`   | No activar suscripción/bundle/bounty                           | Cubierto por handler                                 |
| Fulfillment después de `Settled` | Activar una sola vez vía outbox                                | Cubierto por handler de evento                       |

Para aprobar beta faltan contract/E2E tests con merchant sandbox, 3-D Secure real, firma oficial del webhook, timeout real, conciliación y rollback.
