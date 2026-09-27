---
name: nala-ai-readiness
description: Evalúa la preparación técnica, documental, de datos, seguridad y gobernanza de NALA para incorporar inteligencia artificial. Usar al revisar asistentes, RAG, embeddings, búsqueda vectorial, MCP, prompts, automatización, moderación o casos de uso de IA.
---

# Preparación para IA

## Procedimiento

Antes de cambios extensos, revisa todo el repositorio por módulos sin omitir proyectos y crea o actualiza el plan documental con el orquestador. En auditorías modifica solo `docs/`, preserva antecedentes útiles en `docs/archive/` y no expongas secretos.

1. Parte de arquitectura, inventario de funciones y plan del orquestador; antes de cambios extensos evalúa qué proyectos, datos y controles existen realmente. No despliegues servicios ni implementes modelos en una auditoría documental.
2. Examina casos de uso, calidad y clasificación de datos, consentimiento, documentos fuente, RAG, embeddings, búsqueda vectorial, prompts, evaluaciones, guardrails, trazabilidad, costos estimables, latencia, moderación, revisión humana, monitoreo, riesgos, fallos, datos veterinarios, ubicación, contenido comunitario y MCP/herramientas. Marca lo no verificable cuando falten telemetría o proveedores.
3. Separa IA asistiva de decisiones profesionales; no recomiendes diagnósticos veterinarios autónomos ni uses información médica generada por IA como diagnóstico. No declares RAG o IA existentes solo por dependencias o roadmap. Cita evidencia relativa y clasifica `IMPLEMENTADO_Y_VERIFICADO`, `IMPLEMENTADO_SIN_PRUEBAS`, `PARCIALMENTE_IMPLEMENTADO`, `DECLARADO_NO_IMPLEMENTADO`, `DESHABILITADO`, `OBSOLETO`, `PROPUESTO` o `NO_VERIFICADO`.
4. Actualiza `docs/ai/AI_READINESS.md`, `docs/ai/AI_USE_CASES.md`, `docs/ai/AI_GOVERNANCE.md`, `docs/ai/AI_SAFETY.md` y `docs/ai/AI_IMPLEMENTATION_ROADMAP.md`. En roadmap separa requisitos, decisiones propuestas y riesgos del producto actual. Revisa enlaces y diff.

## Coordinación

- Depende de `nala-architecture-review`, `nala-feature-inventory` y plan de `nala-product-auditor`; recomienda `nala-security-audit` para datos/amenazas, y `nala-health-platform` para límites veterinarios.
- Puede modificar solo los cinco documentos IA anteriores y entradas propias en `docs/auditoria/DOCUMENTATION_CHANGELOG.md`; no modifica controles de seguridad generales, arquitectura, prompts productivos ni código.
- Preserva secciones ajenas; eleva contradicciones al orquestador antes de sustituir y registra cambios propios en el changelog.
