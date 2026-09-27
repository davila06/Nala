---
name: nala-subscription-plans
description: Audita y documenta planes, suscripciones, límites, entitlements y restricciones comerciales de NALA. Usar al revisar Starter, Plus, Pro, proveedores, veterinarias, municipalidades, pricing, feature gating o diferencias entre planes.
---

# Planes y suscripciones

## Procedimiento

Antes de cambios extensos, revisa todo el repositorio por módulos sin omitir proyectos y crea o actualiza el plan documental con el orquestador. En auditorías modifica solo `docs/`, preserva antecedentes útiles en `docs/archive/` y no expongas secretos.

1. Parte de `nala-feature-inventory` y revisa instrucciones, cambios existentes y el plan del orquestador antes de actualizar documentos extensos. Localiza definiciones de planes en código, configuración, migraciones, API y UI; no des por vigentes nombres de un folleto.
2. Por plan contrasta períodos, pruebas gratuitas, límites cuantitativos, entitlements, restricciones y flags con enforcement en backend y representación en frontend. Traza suscripción, compra, renovación y cancelación solo si hay flujo comprobable. Documenta dependencias entre funcionalidades y niveles.
3. Si clasificas capacidades usa `IMPLEMENTADO_Y_VERIFICADO`, `IMPLEMENTADO_SIN_PRUEBAS`, `PARCIALMENTE_IMPLEMENTADO`, `DECLARADO_NO_IMPLEMENTADO`, `DESHABILITADO`, `OBSOLETO`, `PROPUESTO` o `NO_VERIFICADO`. Cita rutas relativas; separa precio vigente demostrado de precio propuesto o no verificado. No inventes precios ni condiciones comerciales.
4. Actualiza `docs/commercial/PLANS.md`, `docs/commercial/PLAN_FEATURE_MATRIX.md` y `docs/commercial/PLAN_LIMITS.md`; etiqueta explícitamente diferencias entre UI, API y enforcement. Comprueba enlaces, evidencias, conflictos y diff.

## Coordinación

- Depende del inventario de `nala-feature-inventory` y del plan de `nala-product-auditor`; recomienda `nala-api-documenter` para restricciones por ruta y `nala-business-analyst` para explicación comercial. No redefine estados técnicos de features.
- Puede modificar solo los tres documentos comerciales anteriores y entradas propias en `docs/auditoria/DOCUMENTATION_CHANGELOG.md`; no modifica `docs/FEATURES.md`, documentación de otros dominios ni código.
- Conserva secciones ajenas; informa contradicciones al orquestador para el reporte de brechas antes de reemplazar y registra cada cambio en el changelog.
