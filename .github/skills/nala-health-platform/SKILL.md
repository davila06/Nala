---
name: nala-health-platform
description: Audita y documenta las capacidades de salud y bienestar de mascotas en NALA. Usar al revisar vacunas, desparasitación, medicamentos, recordatorios, historial clínico, documentos médicos, profesionales veterinarios o asistente de salud.
---

# Salud y bienestar

## Procedimiento

Antes de cambios extensos, revisa todo el repositorio por módulos sin omitir proyectos y crea o actualiza el plan documental con el orquestador. En auditorías modifica solo `docs/`, preserva antecedentes útiles en `docs/archive/` y no expongas secretos.

1. Identifica rutas, handlers, modelos, migraciones, frontend, jobs y pruebas del dominio; acuerda un plan con `nala-product-auditor` antes de documentación extensa. Prioriza comportamiento ejecutable sobre historias o pantallas.
2. Verifica registros de salud, vacunas, desparasitación, alergias, medicamentos, peso, condiciones, recordatorios, archivos, veterinarias, permisos, consentimiento, trazabilidad, privacidad, exportación y límites de cualquier asistente de salud. Distingue captura de datos de recordatorios efectivamente entregados.
3. Para cada flujo registra evidencia relativa, reglas, actores, datos, pruebas y estado `IMPLEMENTADO_Y_VERIFICADO`, `IMPLEMENTADO_SIN_PRUEBAS`, `PARCIALMENTE_IMPLEMENTADO`, `DECLARADO_NO_IMPLEMENTADO`, `DESHABILITADO`, `OBSOLETO`, `PROPUESTO` o `NO_VERIFICADO`. Sin pruebas pertinentes aprobadas no declares verificación.
4. El asistente de salud solo puede describirse como seguimiento, organización o avisos cuando haya evidencia. No lo presentes como sustituto veterinario ni sistema autónomo de diagnóstico o tratamiento; no uses información médica generada por IA como diagnóstico.
5. Actualiza `docs/domains/HEALTH.md`, `docs/domains/HEALTH_RECORDS.md`, `docs/domains/HEALTH_REMINDERS.md` y `docs/domains/HEALTH_SAFETY_BOUNDARIES.md`; comprueba enlaces, límites y diff.

## Coordinación

- Depende de `nala-feature-inventory` y del plan de `nala-product-auditor`; recomienda `nala-security-audit` para consentimiento y privacidad, y `nala-telemedicine-review` solo para consultas remotas efectivas.
- Puede modificar solo los cuatro documentos de salud indicados y entradas propias en `docs/auditoria/DOCUMENTATION_CHANGELOG.md`; no modifica seguridad general, telemedicina, pruebas ni código.
- Conserva secciones ajenas; remite discrepancias al orquestador antes de sustituir y registra cambios propios en el changelog.
