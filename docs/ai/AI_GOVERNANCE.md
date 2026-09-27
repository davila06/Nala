# Gobernanza de datos e IA

Antes de IA sobre fotos, historial médico o GPS, inventariar origen, consentimiento, retención, acceso y trazas sin datos personales. [PawTrackDbContext](../../backend/src/PawTrack.Infrastructure/Persistence/PawTrackDbContext.cs) incluye embeddings, historiales y localizaciones; su sola presencia no acredita consentimiento para entrenamiento ni uso por terceros. El [servicio Azure Vision](../../backend/src/PawTrack.Infrastructure/AI/AzureVisionEmbeddingService.cs) envía imágenes al proveedor solo cuando está configurado; ese flujo exige validación contractual externa. Las [rutas médicas](../../backend/src/PawTrack.API/Controllers/MedicalController.cs) requieren ownership y step-up en mutaciones; evaluar acceso de herramientas de IA por separado.

Responsables, aprobaciones de modelos, inventario de datasets y revisiones humanas son `NO_VERIFICADO`. No documentar certificaciones o decisiones clínicas automáticas.
