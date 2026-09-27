# Fronteras de seguridad clínica

Las métricas y alertas del [MedicalController](../../backend/src/PawTrack.API/Controllers/MedicalController.cs) sirven para organización y seguimiento; no son diagnóstico ni tratamiento automático. Los permisos clínicos dependen de [rutas y grants](../../backend/src/PawTrack.API/Controllers/ClinicsController.cs) y [entidades](../../backend/src/PawTrack.Infrastructure/Persistence/PawTrackDbContext.cs). La [política MFA](../../backend/src/PawTrack.API/Program.cs) protege mutaciones seleccionadas; verifica otras rutas individualmente.

No divulgar archivos o datos médicos en ejemplos; no usar respuestas de IA como diagnóstico. [Azure Vision](../../backend/src/PawTrack.Infrastructure/AI/AzureVisionEmbeddingService.cs) vectoriza imágenes de matching y no constituye asistente clínico. Escalar emergencias y decisiones médicas a profesional competente, sin afirmar que esta política técnica equivale a habilitación sanitaria.
