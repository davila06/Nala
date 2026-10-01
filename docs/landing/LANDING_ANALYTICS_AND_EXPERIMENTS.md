# Analytics y experimentación

Eventos permitidos sin PII: `landing_viewed`, `hero_primary_cta_clicked`, `audience_selected`, `report_lost_pet_clicked`, `report_found_pet_clicked`, `pricing_viewed`, `faq_opened`.

No enviar nombres, correos, teléfonos, IDs de mascotas, GPS, salud, tokens ni texto de formularios.

Funnel: vista → intención → CTA → login/app. KPI: CTR por intención, salida al login, uso de guías y error de catálogo. El landing ya tiene un bridge local (`AnalyticsBridge`) que emite únicamente eventos DOM anónimos con allowlist; no envía datos a terceros. Requiere decisión de consentimiento, proveedor, retención y DPA antes de conectar transporte real.

A/B futuro: headline identidad vs recuperación. Detener si aumenta confusión, rebote o claims no verificables.
