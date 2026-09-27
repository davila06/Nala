# Seguridad para IA asistiva

La [vectorización](../../backend/src/PawTrack.Infrastructure/AI/AzureVisionEmbeddingService.cs) devuelve `null` ante fallo/configuración ausente; la interfaz debe tratarlo como indisponibilidad, no como ausencia de mascota coincidente. [Rutas de matching](../../backend/src/PawTrack.API/Controllers/SightingsController.cs) exponen datos sensibles de fotos/ubicación: comprobar consentimiento, uso abusivo, límite de consultas y borrado antes de ampliar IA.

No entregar diagnóstico ni tratamiento veterinario autónomo. La [API médica](../../backend/src/PawTrack.API/Controllers/MedicalController.cs) ofrece organización y alertas, no prueba autorización profesional de un agente. Evaluaciones de alucinación, moderación y revisión humana quedan `NO_VERIFICADO`; ver [roadmap](AI_IMPLEMENTATION_ROADMAP.md).
