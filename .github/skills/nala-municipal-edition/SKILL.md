---
name: nala-municipal-edition
description: Audita y documenta la edición municipal e institucional de NALA. Usar al revisar mascotas perdidas, animales comunitarios, campañas, castración, denuncias, refugios, convenios, dashboards municipales, pilotos o integraciones institucionales.
---

# Edición municipal

## Procedimiento

Antes de cambios extensos, revisa todo el repositorio por módulos sin omitir proyectos y crea o actualiza el plan documental con el orquestador. En auditorías modifica solo `docs/`, preserva antecedentes útiles en `docs/archive/` y no expongas secretos.

1. Delimita proyectos, rutas, roles y datos institucionales desde el inventario del orquestador; antes de cambios extensos acuerda el plan documental. Comprueba configuración y uso en código en lugar de inferir convenios a partir de propuestas.
2. Examina registro territorial, animales comunitarios, mascotas perdidas, campañas, castración, vacunación, refugios, adopciones, denuncias, casos, mapas, reportes, métricas, roles, acceso institucional, segregación de datos, exportaciones, interoperabilidad, pilotos, soporte, capacitación y carga administrativa.
3. Separa funciones técnicas, piloto acordado y convenio formal: no atribuyas integración ni aprobación a SENASA, municipalidades u otras instituciones sin evidencia formal comprobable. No expongas datos personales o ubicación sensible. Por capacidad usa `IMPLEMENTADO_Y_VERIFICADO`, `IMPLEMENTADO_SIN_PRUEBAS`, `PARCIALMENTE_IMPLEMENTADO`, `DECLARADO_NO_IMPLEMENTADO`, `DESHABILITADO`, `OBSOLETO`, `PROPUESTO` o `NO_VERIFICADO`, con rutas relativas y pruebas pertinentes.
4. Actualiza `docs/editions/MUNICIPAL.md`, `docs/editions/MUNICIPAL_CAPABILITIES.md`, `docs/editions/MUNICIPAL_PILOT.md` y `docs/editions/MUNICIPAL_DATA_GOVERNANCE.md`; separa alcance, propuesta, piloto no verificado y limitaciones. Revisa enlaces y diff.

## Coordinación

- Depende de `nala-feature-inventory` y plan de `nala-product-auditor`; recomienda `nala-security-audit` para segregación y `nala-business-analyst` para actores; consulta `nala-api-documenter` para interoperabilidad.
- Puede modificar solo los cuatro documentos municipales anteriores y entradas propias en `docs/auditoria/DOCUMENTATION_CHANGELOG.md`; no modifica convenios externos, seguridad general, otros dominios ni código.
- Conserva secciones ajenas; eleva contradicciones al orquestador antes de corregir afirmaciones compartidas y registra cambios propios en el changelog.
