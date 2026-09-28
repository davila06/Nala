# Estrategia AI-First de NALA

**Corte:** 2026-09-28.  
**Estado actual:** IA visual condicionada; no hay evidencia de copiloto generativo, RAG, agente autónomo ni diagnóstico.

## Realidad actual

| Capacidad | Estado | Evidencia |
| --- | --- | --- |
| Embeddings/matching visual | `IMPLEMENTADO_SIN_PRUEBAS` | [AzureVisionEmbeddingService.cs](../backend/src/PawTrack.Infrastructure/AI/AzureVisionEmbeddingService.cs), módulo VisualMatch y job de refresco |
| Rastreo de uso IA | `IMPLEMENTADO_SIN_PRUEBAS` | `AiSearchUsage` y consumidores del módulo de visual matching |
| Bot conversacional WhatsApp | `PARCIALMENTE_IMPLEMENTADO` | [WhatsAppController.cs](../backend/src/PawTrack.API/Controllers/WhatsAppController.cs), `BotSession` y pasos; entrega externa no verificada |
| RAG | `DECLARADO_NO_IMPLEMENTADO` | No se encontró índice vectorial documental, retrieval ni citas de fuentes |
| Copilot por rol | `DECLARADO_NO_IMPLEMENTADO` | No se encontró agente ejecutable para dueño, clínica, refugio, municipalidad o soporte |
| Diagnóstico/predicción clínica | `DECLARADO_NO_IMPLEMENTADO` | No existe evidencia de modelo validado ni autorización clínica |

## Casos de uso recomendados

### Copilot para dueños

Preparar preguntas para la consulta, explicar documentos con citas, recordar vacunas y orientar al canal correcto. Debe mostrar límites, evitar diagnóstico, proteger datos de salud y escalar a veterinario.

### Copilot para clínicas

Resumir expediente con autorización, preparar agenda, detectar datos faltantes, redactar comunicaciones y generar tareas. La salida debe ser revisable por personal autorizado y auditable.

### Copilot para refugios y municipalidades

Clasificar casos, resumir reportes, detectar duplicados, priorizar campañas y explicar tendencias agregadas por cantón. No debe decidir adopciones, denuncias o acciones coercitivas sin revisión humana.

### Automatización de soporte

Responder sobre estados y procedimientos desde fuentes canónicas, ocultar PII, registrar handoff y nunca inventar estado de pago, salud, contrato o denuncia.

## Arquitectura propuesta

1. Gateway de modelos con autenticación, límites, logging mínimo y controles de costo.
2. Índices separados para documentación pública, clínica autorizada y datos institucionales.
3. Retrieval con citas a repositorio y fecha de evidencia.
4. Policy layer para PII, salud, menores, coordenadas y acciones sensibles.
5. Human-in-the-loop para salud, bienestar, fraude, pagos y decisiones institucionales.
6. Evaluaciones offline y online: groundedness, precisión, rechazo correcto, sesgo, latencia y costo.
7. Observabilidad: prompt/version, modelo, tokens, fallos, feedback y trazabilidad sin guardar secretos.

## Roadmap AI

### 30 días

Definir inventario de fuentes, clasificación de datos, casos de rechazo y dataset anonimizado de evaluación. Mejorar métricas del matching visual sin afirmar precisión.

### 90 días

Piloto de asistente documental para soporte interno con retrieval y citas; dashboard de calidad; revisión de seguridad y costos.

### 12 meses

Copilotos separados por rol para dueño, clínica y municipio, con permisos, auditoría y escalamiento humano. Evaluación continua antes de ampliar.

### No hacer todavía

No habilitar diagnóstico, prescripción autónoma, decisiones de bienestar, scoring de riesgo individual ni agentes que ejecuten pagos o cambios sensibles sin aprobación explícita.
