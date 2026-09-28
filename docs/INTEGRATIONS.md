# Integraciones: código frente a operación (corte 2026-09-28)

La presencia de un SDK, adaptador, registro DI o recurso Bicep demuestra una
superficie de integración en el código, no una conexión operativa, credencial
válida, despliegue ni SLA. ACS, Dynamics 365 y Power Platform no están
integrado en este repositorio.

| Integración | Evidencia del adaptador | Estado operativo |
| -------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------- |
| Azure Blob | [BlobStorageService](../backend/src/PawTrack.Infrastructure/Storage/BlobStorageService.cs), [DI](../backend/src/PawTrack.Infrastructure/InfrastructureServiceCollectionExtensions.cs) | `NO_VERIFICADO` en Azure real. |
| Azure Vision | [AzureVisionEmbeddingService](../backend/src/PawTrack.Infrastructure/AI/AzureVisionEmbeddingService.cs) devuelve null sin configuración | `NO_VERIFICADO` en proveedor. |
| TrackSolid Pro | [TrackSolidService](../backend/src/PawTrack.Infrastructure/Collars/TrackSolidService.cs) omite polling sin credenciales | `NO_VERIFICADO` en dispositivos. |
| Pagos CyberSource/SINPE | [PaymentsController](../backend/src/PawTrack.API/Controllers/PaymentsController.cs), [WebhooksController](../backend/src/PawTrack.API/Controllers/WebhooksController.cs), [DI](../backend/src/PawTrack.Infrastructure/InfrastructureServiceCollectionExtensions.cs) | `NO_VERIFICADO` para transacciones y firma con proveedor real. |
| Reservas de proveedor | [ManualProviderPaymentGateway](../backend/src/PawTrack.Application/ServiceProviders/Payments/ManualProviderPaymentGateway.cs) | `PARCIALMENTE_IMPLEMENTADO`: intención pendiente, sin pago externo. |
| Envío regulatorio | [NoOpRegulatorySubmissionGateway](../backend/src/PawTrack.Application/Regulatory/Gateways/NoOpRegulatorySubmissionGateway.cs) | `DECLARADO_NO_IMPLEMENTADO` para envío a institución. |
| WhatsApp/Telegram/Facebook/email | [registro de canales](../backend/src/PawTrack.Infrastructure/InfrastructureServiceCollectionExtensions.cs) | `NO_VERIFICADO` para contratos, credenciales y entregas reales. |
| Azure Communication Services | No hay paquetes `Azure.Communication.*`, cliente, configuración ni endpoint | `DECLARADO_NO_IMPLEMENTADO`; las consultas clínicas no son videollamadas. |
| Dynamics 365 / Dataverse | No hay SDK, connector, endpoint, webhook ni configuración | `DECLARADO_NO_IMPLEMENTADO`; el CRM de NALA es interno. |
| Power Platform | No hay Power Automate, Power Apps, Dataverse ni connector | `DECLARADO_NO_IMPLEMENTADO`; los jobs .NET son internos. |

La [validación de proveedores](EXTERNAL_PROVIDER_VALIDATION.md) sigue siendo un gate operativo separado; DI e infraestructura declarada no constituyen aprobación comercial ni disponibilidad.
