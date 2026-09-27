---
name: nala-telemedicine-review
description: Audita y documenta la telemedicina veterinaria de NALA. Usar al revisar video, chat, citas, profesionales veterinarios, Azure Communication Services, consentimiento, expedientes, grabaciones o seguimiento posterior a consultas.
---

# Telemedicina veterinaria

## Procedimiento

Antes de cambios extensos, revisa todo el repositorio por módulos sin omitir proyectos y crea o actualiza el plan documental con el orquestador. En auditorías modifica solo `docs/`, preserva antecedentes útiles en `docs/archive/` y no expongas secretos.

1. Parte del inventario, arquitectura y plan del orquestador; inspecciona API, handlers, frontend, proveedores, configuración, permisos, persistencia y pruebas. Antes de cambios extensos prepara plan documental. No inicies llamadas ni sesiones reales.
2. Rastrea agenda, disponibilidad, identidad profesional, consentimiento, video, audio, chat, emisión y expiración de tokens, salas, notificaciones, expediente, archivos, recetas o recomendaciones profesionales solo si existen, pagos, cancelaciones, auditoría, retención, grabación, privacidad, emergencias y limitaciones territoriales. Comprueba Azure Communication Services u otro proveedor por configuración y uso reales.
3. No presentes telemedicina como implementada sin flujo de extremo a extremo. Por capacidad asigna `IMPLEMENTADO_Y_VERIFICADO`, `IMPLEMENTADO_SIN_PRUEBAS`, `PARCIALMENTE_IMPLEMENTADO`, `DECLARADO_NO_IMPLEMENTADO`, `DESHABILITADO`, `OBSOLETO`, `PROPUESTO` o `NO_VERIFICADO`. Cita evidencia relativa, prueba y limitaciones, sin inventar servicios institucionales ni diagnósticos automáticos.
4. Actualiza `docs/domains/TELEMEDICINE.md`, `docs/domains/TELEMEDICINE_ARCHITECTURE.md`, `docs/domains/TELEMEDICINE_SECURITY.md` y `docs/domains/TELEMEDICINE_LIMITATIONS.md`; comprueba enlaces, coherencia y diff sin revelar tokens.

## Coordinación

- Depende de `nala-feature-inventory` y `nala-architecture-review` bajo plan de `nala-product-auditor`; recomienda `nala-security-audit` para datos sensibles y `nala-health-platform` para registros clínicos.
- Puede modificar solo los cuatro documentos de telemedicina indicados y entradas propias en `docs/auditoria/DOCUMENTATION_CHANGELOG.md`; no modifica arquitectura ni seguridad general, otros dominios, configuración o código.
- Preserva secciones ajenas; informa contradicciones al orquestador para reporte de brechas antes de reemplazar y registra cambios propios en el changelog.
