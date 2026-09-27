# Integraciones y dependencias externas

Para estado por adaptador y gate operativo, consultar [INTEGRATIONS.md](../../docs/INTEGRATIONS.md) y [validación externa](../../docs/EXTERNAL_PROVIDER_VALIDATION.md).

| Área                                      | Evidencia versionada                                       | Qué no acredita                                                                                            |
| ----------------------------------------- | ---------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| Azure SQL / EF Core                       | DI y `PawTrackDbContext`                                   | Conectividad de cada entorno, backups restaurables o región activa.                                        |
| Azure Blob                                | Adaptador y registro DI                                    | Permisos, contenedores, retención ni disponibilidad real en Azure.                                         |
| Azure Vision                              | `AzureVisionEmbeddingService` y handlers/rutas de matching | Credenciales, precisión, costo, latencia, consentimiento del corpus o SLA. Estado externo `NO_VERIFICADO`. |
| GPS / TrackSolid Pro / Jimi / OEM         | servicios, polling/webhooks documentados                   | Contrato, credenciales, dispositivos, cobertura o recepción real. Estado externo `NO_VERIFICADO`.          |
| Pagos / SINPE / CyberSource               | controllers, gateways y documentación de pagos             | Liquidación, checkout universal, autorización comercial o transacción exitosa.                             |
| WhatsApp, Telegram, Facebook, email, push | adaptadores y workflows de mensajes                        | Credenciales, entregabilidad, plantillas aprobadas o SLA.                                                  |
| Envío regulatorio                         | `NoOpRegulatorySubmissionGateway`                          | Presentación real ante SENASA u otra institución; actualmente no implementada.                             |

No existe prueba aquí de una integración de LLM/RAG/agentes en ejecución. La evidencia de matching por embeddings de imágenes no es un agente conversacional.
