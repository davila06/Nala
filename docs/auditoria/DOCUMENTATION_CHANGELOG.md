# Changelog documental de auditoría

## 2026-09-27

- Orquestador: creado [plan](DOCUMENTATION_UPDATE_PLAN.md), [alcance](../PRODUCT_SCOPE.md), [matriz](FEATURE_TRACEABILITY_MATRIX.md) y [reporte de brechas](DOCUMENTATION_GAP_REPORT.md); actualizado [índice](../README.md) sin borrar históricos.
- Inventario: agregada nota de corte en [FEATURES](../FEATURES.md), conservando la matriz objetivo y sus decisiones de producto.
- Arquitectura y seguridad: añadidos [resumen](../ARCHITECTURE.md) y [controles](../SECURITY.md) con subdocumentos en `docs/architecture/` y `docs/security/`; no se modificó infraestructura ni software.
- API y planes: creados [API](../API.md), [comercial](../commercial/PLANS.md), catálogos auxiliares y [CONFIGURATION](../CONFIGURATION.md), [DATA_MODEL](../DATA_MODEL.md), [INTEGRATIONS](../INTEGRATIONS.md), [TESTING](../TESTING.md), [KNOWN_LIMITATIONS](../KNOWN_LIMITATIONS.md).
- Especialistas: nuevos documentos bajo `docs/domains/`, `docs/editions/`, `docs/growth/`, `docs/ai/` y `docs/business/`, con diferencias entre capacidades y propuestas.
- Archivados: ninguno; [README](../README.md) preserva la clasificación histórica de documentos previos. Fuentes anteriores no se reemplazaron de manera silenciosa.

Cada afirmación nueva usa evidencia de código enlazada en su documento. Pruebas de integración, despliegue y proveedores siguen pendientes ([TESTING](../TESTING.md)); este changelog no los acredita.

## 2026-09-27 — AI Knowledge Hub

- Creado `/ai/` como hub Evidence First con contexto, inventario técnico, producto/historia, arquitectura, dominios, integraciones, seguridad, releases, siete prompts reutilizables, scorecard y gobernanza de agentes.
- Creados `docs/adrs/` con siete ADR en estado `DRAFT`; son transcripciones rastreables de la sección ADR del manual técnico, no decisiones formalmente aprobadas.
- Creados `docs/business/ONTOLOGY.md`, `docs/business/CAPABILITIES.md`, `docs/strategy/` y `docs/compliance/` como síntesis/propuestas con evidencia y límites explícitos.
- Actualizado el índice [docs/README.md](../README.md) para descubrir el hub. No se modificó código productivo ni se ejecutaron pruebas de producto, migraciones o despliegues.
- Validación documental final: 67 archivos revisados, 0 enlaces locales rotos, 0 problemas de formato básico y 30 capacidades asignadas una vez cada una. Diagnóstico del editor sin errores en los documentos comprobados; `git diff --check` limpio. Scorecard cualitativo 63/100; no certifica readiness de producción.
