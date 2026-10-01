# Resumen ejecutivo de NALA

**Corte general:** 2026-09-28. **Revalidación focal NFC/contacto:** 2026-10-01.
**Conclusión:** NALA tiene una base técnica amplia y un núcleo defendible en identidad, recuperación y salud, pero todavía debe cerrar evidencia operativa, monetización completa, telemedicina e IA antes de presentarse como ecosistema global.

## Estado real

Implementado en código: QR/perfiles, pérdida, avistamientos, chat enmascarado, salud, clínicas, adopciones, proveedores, tiendas, collares/API GPS, seguridad, notificaciones, reportes, webhooks, importaciones y varias superficies institucionales. Parcial: NFC mediante guía de configuración manual/app externa, subscriptions/entitlements, marketplace/pagos de reservas, tiendas/inventario, integraciones externas y matching visual condicionado. NFC nativa, telemedicina video/audio ACS, Dynamics 365, Power Platform, RAG, copilotos, agentes y diagnóstico IA no están implementados. El endpoint de contacto de reportes requiere autenticación y rate limit, pero no está restringido al propietario; ver [auditoría del landing](auditoria/LANDING_CONTENT_AUDIT.md).

Ver [NALA_ACTUAL_PRODUCT_STATE.md](NALA_ACTUAL_PRODUCT_STATE.md) y [NALA_CAPABILITY_MAP.md](NALA_CAPABILITY_MAP.md).

## Top 25 funcionalidades recomendadas

1. QR universal con onboarding y medición de scans.
2. Flujo de pérdida/hallazgo de un toque.
3. Matching visual evaluado con revisión humana.
4. Red de alertas por cantón.
5. Expediente compartible con consentimiento.
6. Recordatorios de salud configurables.
7. Certificados verificables con límites legales claros.
8. Portal clínico de uso diario.
9. Interoperabilidad de expediente.
10. Agenda clínica y seguimiento.
11. Telemedicina regulada por país.
12. Marketplace con reputación y disputas.
13. Payout y conciliación regional.
14. Catálogo de servicios con disponibilidad real.
15. Programa de refugios/foster.
16. Campañas municipales de bienestar/castración.
17. WhatsApp opt-in y plantillas verificadas.
18. Wearables partner con SLA.
19. Entitlements administrables y medidor visible.
20. Copilot documental con citas.
21. Copilot clínico revisable.
22. Moderación y antifraude asistidos.
23. Analítica agregada para municipios.
24. Membership B2C basada en utilidad recurrente.
25. Plataforma multi-país de consentimiento y pagos.

## Quick wins: 30 días

- Reconciliar catálogo de entitlements y eliminar fallbacks donde sea seguro.
- Montar `EntitlementMeter` en las superficies reales y probar cuotas.
- Validar enlaces, estados y tablas de documentos canónicos.
- Crear dashboard mínimo de activación QR, pérdida y avistamientos.
- Definir piloto CR con una clínica, refugio y municipio, sin prometer operación antes del convenio.
- Crear checklist de evidencia para pagos, mensajería, Vision y TrackSolid.

## Roadmap 90 días

- Piloto Costa Rica con métricas de recuperación y salud.
- Ledger de pagos, conciliación y estados de payout.
- Reputación, cancelaciones y disputas de marketplace.
- Asistente interno RAG con fuentes y citas.
- Matriz de autorización y pruebas autenticadas own/foreign para rutas críticas.
- Runbooks de soporte, moderación, incidentes y proveedores.

## Roadmap 12 meses

- Clínicas con uso diario y expediente interoperable.
- Partner GPS con soporte y SLA.
- Municipal edition validada en al menos un piloto.
- Localización de Panamá/México/Colombia basada en métricas.
- Copilotos por rol con evaluación, permisos y revisión humana.
- Observabilidad de negocio, costos y calidad de modelos.

## Roadmap 24 meses

- Plataforma multi-país con impuestos, moneda, consentimiento y pagos regionales.
- Telemedicina donde exista base legal y red profesional.
- Marketplace con liquidez y protección.
- Pet Identity Passport portable y red de recuperación regional.
- Data platform agregada para bienestar, con governance y privacidad.

## Top riesgos

1. Confundir código/IaC con operación productiva.
2. Vender claims regulatorios, veterinarios o de recuperación sin evidencia.
3. Marketplace sin liquidez, payout o resolución de disputas.
4. Datos de salud/ubicación tratados sin consentimiento y minimización.
5. IA generativa o clínica sin evaluación y escalamiento humano.
6. Duplicación documental que reintroduzca estados obsoletos.
7. Expansión regional antes de validar economía unitaria y soporte.
8. Dependencia de proveedores sin contratos, credenciales y SLA.

## Deuda técnica y documental

- Entitlements incompletos y medidor no integrado.
- Dos carpetas históricas de migraciones.
- IaC no declara frontend Static Web Apps.
- Módulos Bicep reutilizables aún vacíos.
- Warnings Markdown y tablas inconsistentes.
- Documentos de deployment/pricing/TODO históricos que requieren subordinación visible.
- Cobertura incompleta de BOLA autenticado y de operación externa.

## Archivos creados

- `NALA_ACTUAL_PRODUCT_STATE.md`
- `NALA_CAPABILITY_MAP.md`
- `NALA_COMPETITIVE_GAP_ANALYSIS.md`
- `NALA_CR_MARKET_STRATEGY.md`
- `NALA_LATAM_EXPANSION.md`
- `NALA_GLOBAL_VISION.md`
- `NALA_AI_STRATEGY.md`
- `NALA_PRICING_STRATEGY.md`
- `DOCUMENTATION_AUDIT.md`
- `NALA_EXECUTIVE_SUMMARY.md`

## Archivos actualizados

En este corte se crearon informes nuevos sin modificar código. La navegación canónica existente permanece en [README.md](README.md); el plan y los documentos de alcance previos conservan su historial y límites.

## Archivos eliminados

Ninguno. Los documentos históricos se conservan para trazabilidad.
