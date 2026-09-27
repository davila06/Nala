---
name: nala-product-auditor
description: Audita integralmente el repositorio de NALA y sincroniza la documentación con el alcance real del producto. Usar al solicitar auditorías completas, revisión del alcance, actualización de /docs, detección de contradicciones o comparación entre código y documentación.
---

# Auditoría integral de NALA

## Procedimiento

1. Antes de cambiar documentos, revisa instrucciones del repositorio, estado del árbol de trabajo, soluciones y todos los proyectos, módulos y dependencias. Divide repositorios grandes por módulo sin omitir proyectos. Inspecciona backend, frontend, persistencia, configuración, migraciones, pruebas, infraestructura, CI/CD y `docs/`.
2. Rastrea las capacidades desde la entrada hasta la lógica, almacenamiento, UI e integración. Prioriza código ejecutable, configuración y migraciones, pruebas e infraestructura sobre documentación. No tomes mocks, interfaces, contratos, TODOs, menús o prototipos como prueba de una función terminada. Cita rutas relativas por afirmación.
3. Ejecuta `dotnet restore`, `dotnet build` y `dotnet test` cuando no impliquen efectos externos, secretos o cambios versionados fuera de `docs/`; registra comandos, resultados y omisiones. No despliegues ni ejecutes migraciones.
4. Compara implementación y `docs/`; asigna a cada función un estado: `IMPLEMENTADO_Y_VERIFICADO` (flujo y prueba pertinente aprobada), `IMPLEMENTADO_SIN_PRUEBAS` (flujo sin prueba aprobada), `PARCIALMENTE_IMPLEMENTADO`, `DECLARADO_NO_IMPLEMENTADO`, `DESHABILITADO`, `OBSOLETO`, `PROPUESTO` (decisión futura explícita) o `NO_VERIFICADO` (evidencia insuficiente). Distingue propuesto de declarado como disponible sin implementación. Explica incertidumbres.
5. Antes de actualizar otros documentos, crea `docs/auditoria/DOCUMENTATION_UPDATE_PLAN.md` con inventario, cobertura por proyecto, discrepancias, responsables documentales, orden, riesgos y validación. Consolida resultados comprobados de los skills especializados; no atribuyas hallazgos a skills que no se ejecutaron.
6. Crea o actualiza `docs/README.md`, `docs/PRODUCT_SCOPE.md`, `docs/auditoria/FEATURE_TRACEABILITY_MATRIX.md`, `docs/auditoria/DOCUMENTATION_GAP_REPORT.md` y `docs/auditoria/DOCUMENTATION_CHANGELOG.md`. Separa alcance vigente, parciales, exclusiones, propuestas, roadmap y deuda. Conserva antecedentes útiles en `docs/archive/`; usa Markdown, enlaces relativos y Mermaid cuando aclare flujos reales.
7. Valida enlaces, estados, evidencias, contradicciones, exposición de secretos y diff completo. Informa archivos creados, actualizados, archivados y no modificados, conteos por estado, pruebas, límites y riesgos prioritarios. No anuncies cobertura total con proyectos pendientes.

## Coordinación

- Orquesta conceptualmente los otros 14 skills; usa `nala-feature-inventory` para características, `nala-architecture-review` para arquitectura, `nala-security-audit` para controles, `nala-business-analyst` para lenguaje de negocio y los skills de dominio cuando haya evidencia pertinente. Ejecutar uno no implica haber ejecutado los demás.
- Puede modificar solo los cinco documentos indicados y el plan previo, dentro de `docs/`; deriva documentos de API, seguridad, arquitectura, negocio y dominios a sus propietarios. No modifica código productivo, pruebas, infraestructura ni `CHANGELOG.md` raíz.
- Si comparte matriz o changelog con otro skill, conserva las entradas ajenas y actualiza solo la sección o filas que consolida. Registra conflictos con evidencia en el reporte de brechas antes de sustituir contenido, y cada cambio en `docs/auditoria/DOCUMENTATION_CHANGELOG.md`.
