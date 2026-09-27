---
name: nala-api-documenter
description: Genera y mantiene documentación fiel de las APIs implementadas en NALA. Usar al revisar endpoints, controllers, minimal APIs, contratos, autorización, requests, responses, errores, versionado u OpenAPI.
---

# Documentación de API

## Procedimiento

Antes de cambios extensos, revisa todo el repositorio por módulos sin omitir proyectos y crea o actualiza el plan documental con el orquestador. En auditorías modifica solo `docs/`, preserva antecedentes útiles en `docs/archive/` y no expongas secretos.

1. Delimita proyectos API y catálogo de rutas; revisa cambios existentes y, para una actualización extensa, coordina plan con `nala-product-auditor`. Inspecciona registro de endpoints, controllers o minimal APIs, middleware, handlers, validadores, permisos, DTOs y pruebas.
2. Por endpoint documenta método, ruta y versionado, propósito, autenticación, autorización efectiva, request, response, códigos de estado, errores, validaciones, paginación, rate limiting, efectos secundarios, idempotencia y evidencias relativas. Si un aspecto no puede comprobarse, indica `NO_VERIFICADO` en lugar de inferirlo de OpenAPI o contratos.
3. Confirma el flujo hasta la lógica y datos; separa rutas activas de declaradas o deshabilitadas. Para capacidades usa `IMPLEMENTADO_Y_VERIFICADO`, `IMPLEMENTADO_SIN_PRUEBAS`, `PARCIALMENTE_IMPLEMENTADO`, `DECLARADO_NO_IMPLEMENTADO`, `DESHABILITADO`, `OBSOLETO`, `PROPUESTO` o `NO_VERIFICADO`; no confundas una ruta aislada con un proceso completo.
4. Actualiza `docs/API.md`, `docs/api/ENDPOINT_CATALOG.md`, `docs/api/AUTHORIZATION_MATRIX.md` y `docs/api/ERROR_CATALOG.md`. Mantén enlaces relativos, ejemplos sin credenciales y documentación de rutas respaldada por código. Verifica conflictos, enlaces y diff.

## Coordinación

- Depende del inventario de `nala-product-auditor` y de las características de `nala-feature-inventory`; recomienda `nala-security-audit` ante posibles BOLA/IDOR y `nala-architecture-review` para flujos entre módulos.
- Puede modificar solo los cuatro documentos API indicados y entradas propias en `docs/auditoria/DOCUMENTATION_CHANGELOG.md`; no modifica matriz de features, documentos de seguridad, código ni contratos.
- Conserva contenido vigente ajeno; remite discrepancias al orquestador para el reporte de brechas antes de corregir contenido compartido y anota las correcciones propias en el changelog.
