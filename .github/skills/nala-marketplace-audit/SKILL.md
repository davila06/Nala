---
name: nala-marketplace-audit
description: Audita y documenta el marketplace y el ecosistema de proveedores de NALA. Usar al revisar veterinarias, paseadores, groomers, hoteles, pet sitters, adiestradores, reservas, comisiones, reputación o publicación de servicios.
---

# Marketplace y proveedores

## Procedimiento

Antes de cambios extensos, revisa todo el repositorio por módulos sin omitir proyectos y crea o actualiza el plan documental con el orquestador. En auditorías modifica solo `docs/`, preserva antecedentes útiles en `docs/archive/` y no expongas secretos.

1. Delimita el dominio desde inventario y plan del orquestador; recorre rutas, handlers, modelos, migraciones, servicios, frontend e integraciones pertinentes. Para una actualización extensa, acuerda primero el plan documental.
2. Rastrea onboarding, tipos y verificación de proveedores, perfiles, catálogo, disponibilidad, reservas, cobertura, tarifas, comisiones, pagos, cancelaciones, reseñas, reputación, moderación, disputas, notificaciones, suscripciones y acceso a datos de mascotas. Documenta solo los pasos con evidencia relativa en código/configuración/pruebas.
3. Diferencia estrictamente directorio de proveedores, marketplace transaccional y visión futura. Clasifica por capacidad `IMPLEMENTADO_Y_VERIFICADO`, `IMPLEMENTADO_SIN_PRUEBAS`, `PARCIALMENTE_IMPLEMENTADO`, `DECLARADO_NO_IMPLEMENTADO`, `DESHABILITADO`, `OBSOLETO`, `PROPUESTO` o `NO_VERIFICADO`; mocks y contratos no prueban operaciones reales.
4. Actualiza `docs/domains/MARKETPLACE.md`, `docs/domains/PROVIDER_ONBOARDING.md`, `docs/domains/BOOKING_FLOW.md` y `docs/domains/MARKETPLACE_LIMITATIONS.md`. Indica actores, flujo, reglas, seguridad, persistencia, integraciones, pruebas y límites. Valida enlaces y diff.

## Coordinación

- Depende del inventario de `nala-feature-inventory` y plan de `nala-product-auditor`; recomienda `nala-subscription-plans` para gating, `nala-security-audit` para acceso a datos y `nala-api-documenter` para rutas públicas.
- Puede modificar solo los cuatro documentos de marketplace indicados y entradas propias en `docs/auditoria/DOCUMENTATION_CHANGELOG.md`; no modifica planes, fichas generales ni otros dominios o código.
- Conserva contenido ajeno; comunica contradicciones al orquestador antes de reemplazar y registra los cambios propios en el changelog.
