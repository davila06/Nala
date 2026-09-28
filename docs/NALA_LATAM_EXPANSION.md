# Expansión de NALA en Latinoamérica

**Corte estratégico:** 2026-09-28.  
**Estado:** propuesta basada en el producto real y en riesgos de expansión; no es aprobación legal ni plan de despliegue.

## Requisitos comunes antes de salir de Costa Rica

1. Separar configuración por país: moneda, impuestos, idioma, zona horaria, formatos de dirección, teléfono y documentos.
2. Crear un ledger de pagos y conciliación independiente del método local; soportar refunds, chargebacks, idempotencia y payout.
3. Definir residencia, retención, exportación y borrado de datos por jurisdicción.
4. Localizar consentimiento, términos, privacidad, comunicaciones comerciales y datos de salud.
5. Operar soporte, moderación y escalamiento humano en español regional.
6. Medir por país activación, recuperación, retención, uso clínico, conversión y margen.
7. No replicar municipalidad, SENASA, certificados o claims regulatorios sin partner y revisión local.

## Matriz de entrada

| País | Prioridad inicial | Obligatorio para validar | Localización/regulación a investigar | Integraciones/pagos a evaluar |
| --- | --- | --- | --- | --- |
| México | Alta después de CR | QR, pérdida, clínicas, adopción, pagos y soporte | Privacidad federal/estatal, CFDI, datos de salud y reglas de profesionales | SPEI, tarjetas, wallets, facturación CFDI, partners veterinarios |
| Colombia | Alta | Recuperación, clínicas y marketplace de servicios | Habeas data, facturación electrónica, regulación veterinaria y pagos | PSE, tarjetas, wallets y aliados locales |
| Chile | Media-alta | Salud documentada, QR, clínicas y pagos | Protección de datos, documentos tributarios y ejercicio profesional | Webpay, transferencias y facturación local |
| Perú | Media | QR, adopción, clínicas y WhatsApp | Datos personales, comprobantes y reglas municipales | Yape/Plin, tarjetas y agregador local |
| Panamá | Media | QR, servicios y clínicas privadas | Protección de datos, facturación y alianzas institucionales | Transferencias, tarjetas y wallets |
| Guatemala | Media | Recuperación, refugios y red clínica | Datos personales, facturación y métodos de pago locales | Transferencias, tarjetas y cash partners |
| República Dominicana | Media | Recuperación, clínicas y servicios | Datos personales, comprobantes y operación de profesionales | Transferencias, tarjetas y wallets |
| Argentina | Selectiva | Salud, adopción, pagos y control de inflación | Protección de datos, facturación, moneda e impuestos cambiantes | Transferencias, tarjetas, Mercado Pago y precios dinámicos |

Estas filas son hipótesis de entrada. La obligatoriedad legal debe ser validada por asesor local antes de comercializar.

## Capacidades obligatorias de plataforma

### Producto

- Identidad QR y perfil público con URLs regionales.
- Pérdida, avistamiento, contacto seguro, chat y moderación.
- Expediente, documentos, consentimiento, certificados y acceso por clínica.
- Directorio y reservas con reputación, cancelación y disputas.
- Notificaciones por WhatsApp/email/push con opt-in por país.

### Plataforma

- Tenant/pais como dimensión explícita en datos, configuración, métricas y permisos.
- Catálogo de planes y precios localizado, no hardcodeado a CRC/IVA 13%.
- Payment abstraction con ledger, conciliación y webhooks por proveedor.
- Storage, observabilidad, backup y recuperación probados por región.
- Idempotencia y auditoría para integraciones y procesos regulatorios.

## Orden recomendado

1. Costa Rica: probar recuperación + salud + clínica + refugio.
2. Panamá: mercado puente de menor complejidad operativa relativa y partners regionales.
3. Colombia/México: invertir en localización fiscal, pagos y red clínica.
4. Chile/Perú: ampliar salud y marketplace con partner local.
5. Guatemala/República Dominicana/Argentina: entrar con un caso de uso estrecho y economía unitaria validada.

## Riesgos

- Una misma arquitectura técnica no resuelve regulación ni distribución.
- Los datos de ubicación, salud y menores pueden tener requisitos adicionales.
- Las monedas/inflación pueden destruir márgenes si los precios no se versionan.
- Marketplace sin liquidez local genera una experiencia peor que un directorio.
- Telemedicina y seguros requieren análisis profesional específico por país.
