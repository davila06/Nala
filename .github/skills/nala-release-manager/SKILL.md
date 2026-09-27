---
name: nala-release-manager
description: Prepara releases verificables de NALA a partir del repositorio. Usar al generar changelogs, release notes, inventarios de cambios, evaluación de impacto, checklist de publicación o comparación entre versiones.
---

# Preparación de releases

## Procedimiento

Antes de cambios extensos, revisa todo el repositorio por módulos sin omitir proyectos y crea un plan documental; en auditorías modifica solo `docs/` y preserva antecedentes útiles en `docs/archive/`. La única excepción fuera de `docs/` es `CHANGELOG.md` raíz ante una solicitud explícita de preparar release. No expongas secretos.

1. Acota la versión por tags, commits, ramas, pull requests disponibles o diff; registra el rango exacto. Antes de cambios extensos crea un plan de documentos y revisa el árbol de trabajo. Si no existe rango verificable, pide rango o presenta solo cambios observables sin asignarlos a una release.
2. Rastrea archivos, migraciones, configuración, APIs, breaking changes, features, correcciones, seguridad, documentación, despliegue, rollback y pruebas. Cita rutas relativas y referencias verificables al historial; no atribuyas roadmap a versiones ni anuncies capacidades solo por contratos o mocks.
3. Cuando corresponda reutiliza `IMPLEMENTADO_Y_VERIFICADO`, `IMPLEMENTADO_SIN_PRUEBAS`, `PARCIALMENTE_IMPLEMENTADO`, `DECLARADO_NO_IMPLEMENTADO`, `DESHABILITADO`, `OBSOLETO`, `PROPUESTO` o `NO_VERIFICADO`; especifica lo no probado y no publiques secretos. Valida build/pruebas cuando sea seguro, registrando fallos y bloqueos. No despliegues sin solicitud explícita.
4. En una solicitud explícita de **preparar release**, crea o actualiza `CHANGELOG.md` raíz y `docs/releases/RELEASE_NOTES.md`, `docs/releases/RELEASE_CHECKLIST.md`, `docs/releases/BREAKING_CHANGES.md`, `docs/releases/ROLLBACK_PLAN.md`. En una auditoría documental solo modifica `docs/`: no toques `CHANGELOG.md` raíz. Valida referencias, contenido y diff.

## Coordinación

- Depende de cambios verificables, y consulta `nala-feature-inventory`, `nala-api-documenter`, `nala-security-audit` y `nala-product-auditor` según impacto. Consume hechos confirmados, no roadmap.
- Puede modificar los cuatro documentos de releases y entradas propias en `docs/auditoria/DOCUMENTATION_CHANGELOG.md`; solo en una solicitud explícita de release puede modificar `CHANGELOG.md` raíz. No modifica código, pruebas, infraestructura, documentos de dominio ni ejecuta despliegues.
- Conserva historial y secciones ajenas; comunica contradicciones al orquestador antes de sustituir y registra cambios documentales propios en el changelog de auditoría.
