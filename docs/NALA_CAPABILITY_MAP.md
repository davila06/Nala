# Mapa de capacidades de NALA

**Corte:** 2026-09-28. Clasificación basada en código y pruebas del repositorio. `IMPLEMENTADO_Y_VERIFICADO` significa que existe flujo pertinente y pruebas ejecutadas en el corte; no significa producción.

## Identificación y recuperación

| Capacidad                        | Estado                                                                            | Evidencia y límite                                                                                                                                                                                          |
| -------------------------------- | --------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| QR y perfil público              | `IMPLEMENTADO_Y_VERIFICADO`                                                       | [PetsController.cs](../backend/src/PawTrack.API/Controllers/PetsController.cs), [QrCodeService.cs](../backend/src/PawTrack.Infrastructure/Pets/QrCodeService.cs); no acredita impresión física ni Blob real |
| NFC                              | `PROPUESTO`                                                                       | Documentación de dominio sin lector, API, vinculación o UI NFC                                                                                                                                              |
| Collar/GPS                       | `IMPLEMENTADO_Y_VERIFICADO` para API y lógica; proveedor/hardware `NO_VERIFICADO` | [CollarsController.cs](../backend/src/PawTrack.API/Controllers/CollarsController.cs), [TrackSolidService.cs](../backend/src/PawTrack.Infrastructure/Collars/TrackSolidService.cs)                           |
| Reporte de pérdida               | `IMPLEMENTADO_Y_VERIFICADO`                                                       | [LostPetsController.cs](../backend/src/PawTrack.API/Controllers/LostPetsController.cs), handlers y pruebas                                                                                                  |
| Reporte de hallazgo/avistamiento | `IMPLEMENTADO_Y_VERIFICADO`                                                       | [SightingsController.cs](../backend/src/PawTrack.API/Controllers/SightingsController.cs), PII scrubber y pruebas                                                                                            |
| Chat de recuperación y handover  | `IMPLEMENTADO_Y_VERIFICADO`                                                       | [ChatController.cs](../backend/src/PawTrack.API/Controllers/ChatController.cs), [HandoverController.cs](../backend/src/PawTrack.API/Controllers/HandoverController.cs)                                      |

## Salud

| Capacidad                                     | Estado                                                            | Evidencia y límite                                                                                                                               |
| --------------------------------------------- | ----------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| Expediente/timeline                           | `IMPLEMENTADO_Y_VERIFICADO`                                       | [MedicalController.cs](../backend/src/PawTrack.API/Controllers/MedicalController.cs); no es EHR externo                                          |
| Vacunas, peso, medicamentos y desparasitación | `IMPLEMENTADO_Y_VERIFICADO` en registros/modelos                  | Módulo Medical y migraciones; resultados clínicos no son inferibles                                                                              |
| Recordatorios                                 | `IMPLEMENTADO_Y_VERIFICADO`                                       | [VetReminder.cs](../backend/src/PawTrack.Domain/Medical/VetReminder.cs), job de alertas y pruebas                                                |
| Documentos y exportación                      | `IMPLEMENTADO_Y_VERIFICADO`                                       | Descarga protegida, PDF/export; integridad clínica y firma externa no verificadas                                                                |
| Certificados/pasaporte                        | `IMPLEMENTADO_Y_VERIFICADO` para emisión/verificación del sistema | [CertificatesController.cs](../backend/src/PawTrack.API/Controllers/CertificatesController.cs); no equivale a aval estatal                       |
| Castración/campañas                           | `IMPLEMENTADO_SIN_PRUEBAS`                                        | [CastrationCampaignsController.cs](../backend/src/PawTrack.API/Controllers/CastrationCampaignsController.cs); no se acredita operación municipal |

## Telemedicina

| Capacidad                        | Estado                      | Evidencia y límite                                                     |
| -------------------------------- | --------------------------- | ---------------------------------------------------------------------- |
| Chat enmascarado de recuperación | `IMPLEMENTADO_Y_VERIFICADO` | Chat vinculado a casos perdidos; no es telemedicina                    |
| Consulta clínica administrativa  | `PARCIALMENTE_IMPLEMENTADO` | `ClinicalConsultation` y endpoints; no hay sesión audiovisual          |
| Video/audio, tokens, grabación   | `DECLARADO_NO_IMPLEMENTADO` | No hay SDK/cliente/configuración ACS                                   |
| Agenda y seguimiento veterinario | `PARCIALMENTE_IMPLEMENTADO` | Citas, schedules y registros; no se demuestra atención remota completa |

## Comunidad y ecosistema

| Capacidad                                     | Estado                      | Evidencia y límite                                                                                                                                                                                                            |
| --------------------------------------------- | --------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Refugios, aliados y adopciones                | `IMPLEMENTADO_Y_VERIFICADO` | [AlliesController.cs](../backend/src/PawTrack.API/Controllers/AlliesController.cs), [AdoptionsController.cs](../backend/src/PawTrack.API/Controllers/AdoptionsController.cs); no acredita ONG real                            |
| Veterinarias y organizaciones multi-sede      | `IMPLEMENTADO_Y_VERIFICADO` | [ClinicsController.cs](../backend/src/PawTrack.API/Controllers/ClinicsController.cs), módulos Clinic; aprobaciones externas pendientes                                                                                        |
| Proveedores: catálogo/disponibilidad/reservas | `IMPLEMENTADO_Y_VERIFICADO` | [ServiceProvidersController.cs](../backend/src/PawTrack.API/Controllers/ServiceProvidersController.cs); pagos y payout parciales                                                                                              |
| Tiendas, productos y pedidos                  | `PARCIALMENTE_IMPLEMENTADO` | [StoresController.cs](../backend/src/PawTrack.API/Controllers/StoresController.cs), [StoreOrdersController.cs](../backend/src/PawTrack.API/Controllers/StoreOrdersController.cs); no inventario/checkout operativo demostrado |
| Municipalidades y reportes                    | `IMPLEMENTADO_SIN_PRUEBAS`  | [MunicipalController.cs](../backend/src/PawTrack.API/Controllers/MunicipalController.cs); no integración oficial acreditada                                                                                                   |
| Bienestar animal                              | `IMPLEMENTADO_Y_VERIFICADO` | Controllers de welfare, evidencia y administración; operación institucional no verificada                                                                                                                                     |

## Monetización

| Capacidad                         | Estado                      | Evidencia y límite                                                                                                                                            |
| --------------------------------- | --------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Planes y suscripciones            | `PARCIALMENTE_IMPLEMENTADO` | Catálogo, activación, expiración y gates; faltan CRUD completo de definiciones y reconciliación de cuotas                                                     |
| Entitlements/consumo              | `PARCIALMENTE_IMPLEMENTADO` | [EntitlementService.cs](../backend/src/PawTrack.Infrastructure/Subscriptions/EntitlementService.cs); quedan fallbacks legacy y medidor frontend sin call site |
| Tarjeta/SINPE/facturación         | `IMPLEMENTADO_SIN_PRUEBAS`  | Controllers y servicios; transacciones/proveedor real `NO_VERIFICADO`                                                                                         |
| Promociones/cupones               | `IMPLEMENTADO_SIN_PRUEBAS`  | [PromotionsController.cs](../backend/src/PawTrack.API/Controllers/PromotionsController.cs) y persistencia                                                     |
| Comisiones/payouts de marketplace | `DECLARADO_NO_IMPLEMENTADO` | Gateway de reservas manual; no hay liquidación operativa universal                                                                                            |

## IA y automatización

| Capacidad                                       | Estado                      | Evidencia y límite                                                                                                                                                                |
| ----------------------------------------------- | --------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Matching visual/embeddings                      | `IMPLEMENTADO_SIN_PRUEBAS`  | [AzureVisionEmbeddingService.cs](../backend/src/PawTrack.Infrastructure/AI/AzureVisionEmbeddingService.cs); devuelve ausencia cuando no hay configuración y no acredita precisión |
| Bot WhatsApp                                    | `PARCIALMENTE_IMPLEMENTADO` | [WhatsAppController.cs](../backend/src/PawTrack.API/Controllers/WhatsAppController.cs), sesiones y pasos; proveedor/entrega real no verificados                                   |
| Copilot dueño/clínica/refugio/municipio         | `DECLARADO_NO_IMPLEMENTADO` | No se encontró agente o chat generativo ejecutable                                                                                                                                |
| RAG, predicción clínica, resúmenes inteligentes | `DECLARADO_NO_IMPLEMENTADO` | No se encontró pipeline, índice, modelo evaluado ni guardrails específicos                                                                                                        |
| Automatización de jobs y notificaciones         | `IMPLEMENTADO_Y_VERIFICADO` | Hosted services, jobs, outbox/notificaciones y pruebas; operación externa pendiente                                                                                               |

## Lectura del mapa

El núcleo de identificación, recuperación y salud es el activo diferenciador actual. Marketplace, pagos externos, IA y telemedicina deben venderse como superficies parciales o roadmap hasta completar la evidencia operativa. Para el detalle por feature y contradicciones, consultar [FEATURE_TRACEABILITY_MATRIX.md](auditoria/FEATURE_TRACEABILITY_MATRIX.md) y [PRODUCT_SCOPE.md](PRODUCT_SCOPE.md).
