# Visión global de NALA

**Corte:** 2026-09-28.  
**Regla:** la visión describe capacidades futuras; el estado actual está en [NALA_ACTUAL_PRODUCT_STATE.md](NALA_ACTUAL_PRODUCT_STATE.md) y [NALA_CAPABILITY_MAP.md](NALA_CAPABILITY_MAP.md).

## Qué tendría que cambiar para competir globalmente

NALA debe pasar de un monolito amplio preparado para crecimiento a una plataforma operable multi-país, con contratos de datos estables, aislamiento por tenant, pagos y compliance regionales, observabilidad de negocio y una red de confianza. Extraer servicios no es el primer objetivo: primero deben estabilizarse límites de módulos, ownership de datos, contratos API, jobs idempotentes y métricas.

## Pilares

- **Identidad universal:** QR/NFC/wearables, perfil portable, consentimiento y verificación.
- **Recuperación:** matching multimodal, alertas geográficas, coordinación y handover seguro.
- **Salud:** expediente longitudinal, interoperabilidad, documentos verificables y acceso clínico.
- **Ecosistema:** clínicas, refugios, servicios, tiendas, municipios, insurers y partners.
- **Inteligencia:** copilotos con fuentes, automatización supervisada y analítica agregada.
- **Confianza:** antifraude, reputación, moderación, privacidad y trazabilidad.
- **Economía:** suscripciones, marketplace, pagos, payout, seguros y revenue sharing.

## Top 10 funcionalidades mundiales

1. **Pet Identity Passport:** identidad QR/NFC/wearable con historial y consentimiento portable.
2. **Global Lost-Pet Network:** matching visual/geográfico multi-país con privacidad y escalamiento humano.
3. **Health Record Interoperability:** export/import estándar, acceso por grants y auditoría clínica.
4. **Veterinary Copilot con evidencia:** resumen, preparación de consulta y seguimiento; nunca diagnóstico autónomo.
5. **Telemedicina regulada por país:** video, agenda, receta, consentimiento, pago y handoff presencial.
6. **Trusted Pet Services Marketplace:** verificación, disponibilidad, reputación, payout, disputas y seguros.
7. **Municipal/NGO Animal Welfare Cloud:** campañas, refugios, bienestar, adopciones y métricas agregadas.
8. **Wearable-neutral telemetry platform:** partners de hardware, eventos normalizados, batería y alertas.
9. **Pet commerce membership:** ofertas personalizadas, retail partners, delivery e inventario con consentimiento.
10. **Pet data trust and insights:** analítica agregada para prevención y políticas públicas, con governance fuerte.

## Arquitectura objetivo por etapas

### 0-12 meses

Estabilizar modular monolith, contratos API, catálogo de entitlements, ledger de pagos, observabilidad, seguridad y evidencia de operaciones. No fragmentar servicios solo por escala hipotética.

### 12-24 meses

Extraer integraciones y jobs de alto riesgo cuando existan métricas de carga; introducir event contracts, outbox/inbox, tenant/pais, feature flags y data platform con retención diferenciada.

### 24+ meses

Separar bounded contexts de identidad/recuperación, salud, marketplace y analytics solo con señales de volumen, autonomía de equipos y necesidad de aislamiento. Mantener un sistema de identidad/consentimiento común.

## Moats potenciales

- Datos longitudinales de identidad, recuperación y salud con consentimiento.
- Red local de clínicas, refugios y autoridades, difícil de copiar sin confianza.
- Interoperabilidad y evidencia verificable, no solo una app de consumo.
- Modelos de matching evaluados con datos regionales y revisión humana.
- Workflows de cumplimiento y privacidad incorporados desde el diseño.

## Condiciones de éxito

No se debe perseguir escala global antes de demostrar en Costa Rica: tiempo de recuperación, retención, uso semanal de clínicas, economía de marketplace, tasa de incidentes y operación de soporte. La expansión debe ser una repetición de un playbook medido, no una copia de documentos o infraestructura.
