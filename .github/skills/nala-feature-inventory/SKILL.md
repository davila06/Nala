---
name: nala-feature-inventory
description: Descubre, clasifica y documenta las características reales de NALA. Usar al crear o actualizar FEATURES.md, inventariar módulos, validar funcionalidades, establecer dependencias o determinar qué está implementado, parcial, propuesto u obsoleto.
---

# Inventario de características

## Procedimiento

Antes de cambios extensos, revisa todo el repositorio por módulos sin omitir proyectos y crea o actualiza el plan documental con el orquestador. En auditorías modifica solo `docs/`, preserva antecedentes útiles en `docs/archive/` y no expongas secretos.

1. Delimita módulos y revisa instrucciones y cambios preexistentes; antes de una actualización extensa inventaría los proyectos afectados y acuerda el plan documental con `nala-product-auditor`. Recorre controllers y endpoints, commands, queries, handlers, entidades, reglas de dominio, frontend y rutas, migraciones, jobs, integraciones, flags y pruebas.
2. Para cada capacidad sigue entrada, ejecución, persistencia e interfaz; diferencia endpoint o pantalla aislados de un flujo completo. Cruza permisos, plan requerido cuando exista enforcement comprobable, límites e integración real; ni menús ni mocks demuestran disponibilidad.
3. Registra identificador estable, nombre, dominio, objetivo, valor, actores, flujo, reglas, validaciones, seguridad, backend, frontend, persistencia, integraciones, plan, limitaciones, pruebas y brechas. Usa rutas relativas a código y pruebas para cada afirmación; anota "no verificado" si falta evidencia.
4. Asigna uno de `IMPLEMENTADO_Y_VERIFICADO`, `IMPLEMENTADO_SIN_PRUEBAS`, `PARCIALMENTE_IMPLEMENTADO`, `DECLARADO_NO_IMPLEMENTADO`, `DESHABILITADO`, `OBSOLETO`, `PROPUESTO` o `NO_VERIFICADO`. Reserva el primero a pruebas pertinentes ejecutadas y aprobadas; `PROPUESTO` describe planes explícitos, no producto actual.
5. Actualiza `docs/FEATURES.md` y las filas propias de `docs/auditoria/FEATURE_TRACEABILITY_MATRIX.md`. Valida correspondencia feature-evidencia-estado, enlaces y diff. No inventes características a partir del contexto funcional de NALA.

## Coordinación

- Depende del inventario de proyectos y plan de `nala-product-auditor`; alimenta `nala-subscription-plans`, `nala-business-analyst` y auditoría integral. Recomienda `nala-api-documenter` para contratos y `nala-architecture-review` para dependencias transversales.
- Puede modificar solo `docs/FEATURES.md`, filas propias de `docs/auditoria/FEATURE_TRACEABILITY_MATRIX.md` y entradas propias en `docs/auditoria/DOCUMENTATION_CHANGELOG.md`; no modifica planes comerciales, documentación de dominio, otros documentos ni código.
- Mantén filas ajenas y secciones vigentes. Antes de reemplazar contradicciones, comunícalas al orquestador para `docs/auditoria/DOCUMENTATION_GAP_REPORT.md` y registra cada cambio propio en el changelog.
