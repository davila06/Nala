# Hoja de ruta propuesta de IA

**No es alcance actual.** Partiendo de [Vision](../../backend/src/PawTrack.Infrastructure/AI/AzureVisionEmbeddingService.cs) y [rutas de matching](../../backend/src/PawTrack.API/Controllers/SightingsController.cs): 1) verificar contrato/cuenta y calidad del matching con fotos autorizadas; 2) definir métricas reproducibles, costos y borrado; 3) evaluar RAG solo con fuentes aprobadas, identidad de acceso y trazas; 4) añadir herramientas/agentes únicamente tras pruebas de privacidad, seguridad, revisión humana y límites clínicos. El [documento estratégico](../PRODUCT_STRATEGY_TOP1.md) no constituye evidencia de implementación.

La ubicación de [collares](../../backend/src/PawTrack.API/Controllers/CollarsController.cs) y datos de [salud](../../backend/src/PawTrack.API/Controllers/MedicalController.cs) requieren controles adicionales. Ningún paso de esta hoja de ruta se marca como disponible por incluirlo aquí.
