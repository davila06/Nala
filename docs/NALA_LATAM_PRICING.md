# Marco de precio y localización LATAM

**Corte:** 2026-09-28. Países objetivo: México, Colombia, Chile, Perú, Panamá, Argentina, República Dominicana y Guatemala. Es una hipótesis de investigación, no tarifa publicada ni análisis de elasticidad.

## Indicador de capacidad comparativa

Banco Mundial, indicador `NY.GDP.PCAP.PP.CD`, PIB per cápita PPA en dólares internacionales corrientes, observación 2024 (última serie solicitada; API declara `lastupdated=2026-07-13`). La PPA ayuda a comparar capacidad agregada relativa, pero **no** representa ingreso disponible de dueños, mediana, distribución, gasto veterinario, willingness-to-pay o mercado pet.

| País                 | Código | PIB per cápita PPA 2024 (int'l $) | Índice vs Costa Rica |     Banda inicial a probar vs precio CR |
| -------------------- | ------ | --------------------------------: | -------------------: | --------------------------------------: |
| Costa Rica           | CRI    |                            31.552 |                 1,00 |                                   ancla |
| México               | MEX    |                            25.820 |                 0,82 |                              0,75-0,95x |
| Colombia             | COL    |                            22.439 |                 0,71 |                              0,65-0,85x |
| Chile                | CHL    |                            36.071 |                 1,14 |                              0,95-1,15x |
| Perú                 | PER    |                            17.882 |                 0,57 |                              0,55-0,75x |
| Panamá               | PAN    |                            41.430 |                 1,31 |                              1,00-1,25x |
| Argentina            | ARG    |                            30.475 |                 0,97 | 0,65-1,00x, con actualización frecuente |
| República Dominicana | DOM    |                            27.582 |                 0,87 |                              0,70-0,90x |
| Guatemala            | GTM    |                            14.393 |                 0,46 |                              0,45-0,65x |

Fuente: [World Bank API, NY.GDP.PCAP.PP.CD, date=2024](https://api.worldbank.org/v2/country/CRI;MEX;COL;CHL;PER;PAN;ARG;DOM;GTM/indicator/NY.GDP.PCAP.PP.CD?format=json&date=2024&per_page=100), respuesta consultada 2026-09-28. Índices calculados como PIB país / Costa Rica y redondeados. La banda de test es juicio de pricing, no sale matemáticamente del indicador y requiere entrevistas/experimentos locales.

## Arquitectura de precio

- Mantener estructura de producto: Free recovery, Plus individual, Familia, Clinic packages, Shelter/subsidy, Municipal contracts. Localizar moneda y precio desde configuración/catálogo, no ampliar `SubscriptionPricing` hardcoded en CRC.
- Testear equivalencia de valor, no conversión FX uno-a-uno. Ajustar por pagos, impuestos, hardware/data, costo de soporte, capacidad y partner.
- Antes de facturar: país legal de venta, moneda, factura/IVA/retenciones, contrato consumidor, renovación/cancelación, residencia/retención de datos y contacto local.
- No permitir falsear `RequiresInvoice` como regla fiscal. Es necesaria revisión de asesor tributario en cada país.
- Redondear a precios locales psicológicos sólo luego de entrevistas; no publicar estimados USD convertidos como precio final.

## Segmentos y estrategia de entrada

| País                 | Banda inicial orientativa | Adquisición y canal a validar                                          | Features locales / riesgos obligatorios antes de vender                                                                 |
| -------------------- | ------------------------- | ---------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| México               | 0,75-0,95x CR             | Clínicas/ONGs en ciudad piloto; cobro local                            | MXN, impuestos/CFDI, privacidad federal/estatal, SPEI/tarjeta, cobertura/profesionales; validar país y estado por legal |
| Colombia             | 0,65-0,85x CR             | Clínicas/refugios y partnerships urbanos                               | COP, facturación, habeas data, pagos locales, recibos/reversos, consentimiento de salud                                 |
| Chile                | 0,95-1,15x CR             | Clínicas, QR y expediente                                              | CLP, factura y privacidad vigente, pagos locales, respaldo de datos y professional rules                                |
| Perú                 | 0,55-0,75x CR             | Alianzas clinic/shelter, bajo consumo de datos                         | PEN, pagos locales (incl. wallets/transferencias a validar), datos personales, acceso offline/low bandwidth             |
| Panamá               | 1,00-1,25x CR             | Clínicas privadas, operadores regionales                               | PAB/USD, medios de pago, privacidad, fiscalidad y contratación institucional                                            |
| Argentina            | 0,65-1,00x CR con ajuste  | Precio corto plazo y paquetes prepago; evitar lista anual nominal fija | ARS, inflación/FX, impuestos y facturación, métodos de pago, revisión de precio/contrato frecuente                      |
| República Dominicana | 0,70-0,90x CR             | Red de veterinarias/municipios y WhatsApp opt-in                       | DOP, fiscalidad, privacidad, pagos y licencias profesionales                                                            |
| Guatemala            | 0,45-0,65x CR             | QR, refugios y adopción; modelo sponsor/ONG                            | GTQ, métodos de pago, datos personales, cobertura, soporte local y baja conectividad                                    |

Los métodos (SPEI, PSE, Webpay, Yape/Plin, etc.) son candidatos a validar con proveedores, no integraciones actuales de NALA.

## Funcionalidad que debe viajar por país

1. QR/perfil público, pérdida/hallazgo, contactos mínimos con protección de PII.
2. Correos/push y WhatsApp con consentimiento, plantillas y reglas locales; no asumir SMS gratuito.
3. Formatos localizados de teléfono, dirección, fecha, zona horaria, cantón/estado/municipio, idioma y moneda.
4. Exportación/borrado/consentimiento y soporte humano con workflows auditables.
5. Cobros con referencia idempotente, conciliación, refund/disputa, invoice/tax IDs y recuperación de fallos.
6. Clínicas/profesionales validados bajo ley local; no exportar certificados ni telemedicina de CR como habilitación profesional extranjera.
7. Datos multi-país explícitos en tenant, permisos, analytics, retención y contrato; la infraestructura declarada hoy no demuestra residencia regional.

## Política de prueba

Lanzar país sólo con piloto que demuestre: adquisición por canal, activación QR, casos/reunificaciones con denominador y periodo, retención 90 días, conversión pagada, margen después de pagos/soporte/proveedores y una ruta de queja/reembolso. Testear los precios de cada país por bandas relativas anteriores; segmentar por ciudad y canal. Para CAC/LTV y go/no-go usar [unit economics](NALA_UNIT_ECONOMICS.md); para secuencia consultar [estrategia LATAM previa](NALA_LATAM_EXPANSION.md).
