---
name: nala-business-analyst
description: Traduce la implementación técnica de NALA a capacidades, procesos, reglas y valor de negocio. Usar al crear documentación para producto, ventas, inversionistas, proveedores, veterinarias, municipalidades o socios estratégicos.
---

# Análisis de negocio basado en evidencia

## Procedimiento

Antes de cambios extensos, revisa todo el repositorio por módulos sin omitir proyectos y crea o actualiza el plan documental con el orquestador. En auditorías modifica solo `docs/`, preserva antecedentes útiles en `docs/archive/` y no expongas secretos.

1. Consume inventario, estados técnicos y plan del orquestador antes de cambios extensos. Verifica en código y pruebas toda capacidad de la que derives afirmaciones de negocio; no tomes presentaciones comerciales como prueba de disponibilidad.
2. Para cada capacidad explica en lenguaje no técnico problema resuelto, actor, objetivo, flujo, reglas, valor, dependencia, restricción, indicador **sugerido**, estado real y evidencia técnica con rutas relativas. Distingue indicadores sugeridos de métricas medidas; no inventes resultados ni precios.
3. Reutiliza sin alterar los estados de `nala-feature-inventory`: `IMPLEMENTADO_Y_VERIFICADO`, `IMPLEMENTADO_SIN_PRUEBAS`, `PARCIALMENTE_IMPLEMENTADO`, `DECLARADO_NO_IMPLEMENTADO`, `DESHABILITADO`, `OBSOLETO`, `PROPUESTO` y `NO_VERIFICADO`. No presentes como valor disponible algo parcial o en roadmap; separa aspiración y oferta actual.
4. Actualiza `docs/business/BUSINESS_CAPABILITIES.md`, `docs/business/ACTORS_AND_JOURNEYS.md`, `docs/business/BUSINESS_RULES.md`, `docs/business/VALUE_PROPOSITION.md` y `docs/business/CAPABILITY_MAP.md`. Comprueba claridad para audiencia de producto, enlaces, evidencia y diff.

## Coordinación

- Depende de `nala-feature-inventory` y plan de `nala-product-auditor`; recomienda el skill de dominio correspondiente para resolver brechas, `nala-subscription-plans` para oferta por plan y `nala-security-audit` para privacidad. Traduce, no redefine estados técnicos.
- Puede modificar solo los cinco documentos de negocio indicados y entradas propias en `docs/auditoria/DOCUMENTATION_CHANGELOG.md`; no modifica FEATURES, precios, acuerdos institucionales, otros dominios ni código.
- Conserva secciones ajenas; informa contradicciones al orquestador antes de reemplazar y registra cada cambio propio en el changelog.
