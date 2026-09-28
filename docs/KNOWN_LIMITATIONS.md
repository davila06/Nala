# Limitaciones verificables y pendientes (corte 2026-09-28)

## Alcance que no debe anunciarse como disponible

- NFC sigue siendo `PROPUESTO`: no hay lector, vinculación, API, persistencia
  ni UI ejecutable.
- Telemedicina audiovisual, Azure Communication Services, Dynamics 365 y
  Power Platform son `DECLARADO_NO_IMPLEMENTADO`.
- IA solo cubre validación/matching visual condicionado; no hay RAG,
  copiloto, agente autónomo, diagnóstico ni tratamiento.
- Las integraciones Azure tienen adaptadores y configuración, pero su operación
  en Azure, credenciales, despliegue y SLA son `NO_VERIFICADO`.
- Suscripciones y marketplace tienen flujo parcial: no equivalen a aprobación
  comercial, pagos liquidados, inventario transaccional ni todas las cuotas.

1. Integraciones externas no comprobadas: [Vision](../backend/src/PawTrack.Infrastructure/AI/AzureVisionEmbeddingService.cs), [TrackSolid](../backend/src/PawTrack.Infrastructure/Collars/TrackSolidService.cs), [pagos](../backend/src/PawTrack.Infrastructure/InfrastructureServiceCollectionExtensions.cs). Sin prueba con proveedor no se puede afirmar disponibilidad o SLA.
2. [Proveedor de pagos manual](../backend/src/PawTrack.Application/ServiceProviders/Payments/ManualProviderPaymentGateway.cs) y [gateway regulatorio NoOp](../backend/src/PawTrack.Application/Regulatory/Gateways/NoOpRegulatorySubmissionGateway.cs) no son procesamiento/entrega externos.
3. Telemedicina con audio/video no está respaldada por flujo en [rutas clínicas](../backend/src/PawTrack.API/Controllers/ClinicsController.cs) y [router UI](../frontend/src/app/routes.tsx); ver [telemedicina](domains/TELEMEDICINE.md).
4. El [funnel de producto](../backend/src/PawTrack.API/Controllers/ProductAnalyticsController.cs) no acredita tracción, atribución causal ni cifras de resultados.
5. Los adapters y jobs externos no sustituyen pruebas de proveedor; el estado
   actualizado de las suites se encuentra en [TESTING](TESTING.md).
6. [FEATURES](FEATURES.md) y [PRICING_AND_PLANS](PRICING_AND_PLANS.md) contienen contratos y cifras sujetos a validación; la aplicación de **cada** gate y aprobación comercial es `NO_VERIFICADO` salvo evidencia en [matriz](auditoria/FEATURE_TRACEABILITY_MATRIX.md).
7. La referencia de API previa describe accesos diferentes al [handler real de contacto](../backend/src/PawTrack.Application/LostPets/Queries/GetLostPetContact/GetLostPetContactQuery.cs); ver [brechas](auditoria/DOCUMENTATION_GAP_REPORT.md).
