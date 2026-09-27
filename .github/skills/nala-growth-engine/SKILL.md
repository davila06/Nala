---
name: nala-growth-engine
description: Audita y documenta las capacidades de crecimiento, marketing y comunidad de NALA. Usar al revisar referidos, influencers, afiliados, cupones, UGC, campañas, tracking, atribución, alianzas, engagement o automatización de marketing.
---

# Crecimiento y comunidad

## Procedimiento

Antes de cambios extensos, revisa todo el repositorio por módulos sin omitir proyectos y crea o actualiza el plan documental con el orquestador. En auditorías modifica solo `docs/`, preserva antecedentes útiles en `docs/archive/` y no expongas secretos.

1. Usa inventario y plan del orquestador para identificar componentes activos de crecimiento; contrasta backend, frontend, configuración, eventos y pruebas antes de cambios extensos.
2. Revisa referidos, códigos, cupones, afiliados, influenciadores, contenido generado por usuarios, campañas, UTM, atribución, eventos, funnels, consentimiento, preferencias, email, push, WhatsApp solo si existe, métricas, automatizaciones, fraude y experimentos. Distingue captura de evento de atribución o automatización operativa.
3. No inventes alianzas, métricas ni resultados. Cita rutas relativas para cada flujo y clasifica `IMPLEMENTADO_Y_VERIFICADO`, `IMPLEMENTADO_SIN_PRUEBAS`, `PARCIALMENTE_IMPLEMENTADO`, `DECLARADO_NO_IMPLEMENTADO`, `DESHABILITADO`, `OBSOLETO`, `PROPUESTO` o `NO_VERIFICADO`; las propuestas no cuentan como alcance actual.
4. Actualiza `docs/growth/GROWTH_ENGINE.md`, `docs/growth/REFERRALS.md`, `docs/growth/CAMPAIGNS.md`, `docs/growth/ATTRIBUTION.md` y `docs/growth/GROWTH_EVENTS.md`; especifica privacidad, abuso y límites. Valida enlaces, afirmaciones y diff.

## Coordinación

- Depende de `nala-feature-inventory` y plan de `nala-product-auditor`; recomienda `nala-security-audit` para consentimiento, `nala-subscription-plans` para descuentos y `nala-business-analyst` para valor comercial.
- Puede modificar solo los cinco documentos de crecimiento y entradas propias en `docs/auditoria/DOCUMENTATION_CHANGELOG.md`; no modifica precios, seguridad general, métricas de otros dominios ni código.
- Preserva contenido vigente de otros propietarios; eleva contradicciones al orquestador antes de reemplazar y registra cambios propios en el changelog.
