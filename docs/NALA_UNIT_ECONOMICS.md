# Unit economics: modelo inicial y vacíos de evidencia

**Corte:** 2026-09-28. No existe en el repositorio una factura Azure, telemetría de costo/usuario, CAC, ingresos cobrados, churn o GMV. Lo siguiente es un modelo determinista con volúmenes asumidos y precios técnicos codificados; no describe la economía actual de NALA.

## Fórmulas necesarias

- `MRR = sum(cuentas activas pagadas * importe neto recurrente)`; anual se reconoce `precio anual / 12`, pero caja y revenue recognition deben separarse.
- `CAC = gasto atribuible de adquisición / nuevos clientes pagados incrementales` por canal y cohorte.
- `LTV gross profit = ARPU mensual * margen bruto / churn mensual` sólo en modelo perpetuo geométrico estable; no incluir marketplace GMV como revenue.
- `Margen bruto = (ingreso neto - cloud variable - proveedor/pagos - soporte directo/fulfillment) / ingreso neto`.
- `Break-even clientes = costos fijos mensuales / contribución mensual por cliente`; sumar onboarding/costos one-off por separado.

## Ingresos brutos en escenarios ilustrativos

Todos los conteos son inputs hipotéticos, no forecast ni ventas observadas. Precio usa `SubscriptionPricing` actual antes de IVA y aprobación. Excluye add-ons, churn, descuentos reales, refunds, impuestos, comisiones de pago, mora, pilotos gratis y costos.

| Escenario de modelado | Dueños activos | Conversión asumida |    Mezcla Plus/Familia | Ingreso dueños/mes | B2B asumido                                                                           | Ingreso total bruto equivalente/mes |
| --------------------- | -------------: | -----------------: | ---------------------: | -----------------: | ------------------------------------------------------------------------------------- | ----------------------------------: |
| Conservador           |          1.000 |      1% = 10 pagos |     8 Plus + 2 Familia |            ₡33.820 | 2 ClinicPlus + 1 ClinicPartner + 2 ShelterPlus                                        |                            ₡114.820 |
| Esperado              |          5.000 |     3% = 150 pagos |  120 Plus + 30 Familia |           ₡508.500 | 8 ClinicPlus + 3 ClinicPartner + 10 ShelterPlus + 1 MuniBasica anual prorrateada      |                            ₡826.000 |
| Agresivo              |         20.000 |   5% = 1.000 pagos | 800 Plus + 200 Familia |         ₡3.390.000 | 30 ClinicPlus + 10 ClinicPartner + 50 ShelterPlus + 5 MuniBasica anuales prorrateadas |                          ₡4.652.500 |

Cálculo reproduce los importes base: Plus ₡2.990, Familia ₡4.990, ClinicPlus ₡15.000, ClinicPartner ₡35.000, ShelterPlus ₡8.000/mes y MuniBasica ₡150.000/año. Estos inputs no reflejan TAM, conversiones, capacidad de soporte, población de Costa Rica, descuentos o aprobación. El escenario agresivo probablemente requiere distribución, personal y operaciones que no se han costeado.

## CAC y LTV: techo de prueba, no medición

No hay CAC/LTV real: no se encontró atribución de canal ni churn por cohorte. Esta sensibilidad usa margen bruto hipotético de 40%/70%/85% y churn mensual hipotético de 10%/5%/2%; `CAC payback ceiling` es máximo aritmético para recuperar CAC en seis meses, **no** presupuesto recomendado. Familia/Plus usan precio base anterior a IVA.

| Plan           | Escenario margen/churn | LTV bruto teórico | CAC máximo a 6 meses de payback |
| -------------- | ---------------------- | ----------------: | ------------------------------: |
| Plus ₡2.990    | 40% / 10%              |           ₡11.960 |                          ₡7.176 |
| Plus ₡2.990    | 70% / 5%               |           ₡41.860 |                         ₡12.558 |
| Plus ₡2.990    | 85% / 2%               |          ₡127.075 |                         ₡15.249 |
| Familia ₡4.990 | 40% / 10%              |           ₡19.960 |                         ₡11.976 |
| Familia ₡4.990 | 70% / 5%               |           ₡69.860 |                         ₡20.958 |
| Familia ₡4.990 | 85% / 2%               |          ₡212.075 |                         ₡25.449 |

LTV perpetuo puede sobreestimar al ignorar retención finita, soporte, descuentos, costos de adquisición de hardware, expansión, morosidad y comisión de pago. Reemplazar supuestos por cohortes mensuales; reportar LTV a 12/24 meses, margen y payback por canal/plan.

## Costos operativos: fórmula y disponibilidad

| Centro de costo                    | Evidencia presente                                                                                            | Unidad que hay que medir                                      | Cálculo / estado                                                                                                                   |
| ---------------------------------- | ------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| Azure SQL                          | [main.bicep](../infra/main.bicep) declara SQL Database GP serverless Gen5 1 vCore, min 0.5, auto-pause 60 min | vCore-seg/hora, almacenamiento, backup, HA/egress             | Retail Pricing consultado para `GP_S_Gen5_1`, East US: respuesta sin filas; región real no definida. Precio mensual NO_VERIFICADO. |
| Container Apps                     | Bicep/release config declara servicio; replicas/cpu/mem deben leerse del deployment vigente                   | vCPU-sec, GiB-sec, requests, minimum replicas                 | No hay métricas/consumo ni precio regional confirmado.                                                                             |
| Storage                            | Bicep declara StorageV2 Standard LRS y containers                                                             | GB-month por tier, operaciones, egress, lifecycle             | Necesita fotos/docs por usuario, retención y tamaño. No se obtuvo consumo real.                                                    |
| Front Door/network                 | Bicep declara Front Door y red                                                                                | requests, egress, rules, private endpoints                    | Dependiente de región/tráfico, no estimable con repositorio.                                                                       |
| Azure Monitor/App Insights         | Bicep Log Analytics 30 días + App Insights                                                                    | GB ingeridos, retención y query                               | Medir volumen; eliminar PII/secrets del logging. Precio no cotizado por volumen.                                                   |
| Key Vault                          | Infra y configuración                                                                                         | operaciones/secret/version, networking                        | Calls/tenant y SKU no documentados con volumen.                                                                                    |
| Azure Vision/matching              | `AzureVisionEmbeddingService`                                                                                 | imágenes aceptadas, embedding/query, almacenamiento vectorial | No hay benchmark costo/latencia ni uso; encerrar por caso y medir. `OpenAI` no tiene pipeline implementado.                        |
| Azure Communication Services/video | Sin SDK/integración encontrada                                                                                | minutos de video, participantes, recording/egress             | Producto no implementado; costo N/A hasta elegir proveedor/país.                                                                   |
| Email                              | `EmailSender`                                                                                                 | mensajes entregados, bounce, proveedor                        | Proveedor/plan y volumen efectivos no verificados.                                                                                 |
| Push                               | NotificationDispatcher/servicio push                                                                          | dispositivos, fan-out, entregas                               | SDK/configuración real y pricing no verificados.                                                                                   |
| SMS                                | No se verificó sender SMS en flujo core                                                                       | mensajes/país/carrier                                         | No incluir revenue/feature SMS; costo/partner N/A.                                                                                 |
| WhatsApp/broadcast                 | canales y controllers en código                                                                               | conversaciones, plantillas, mensajes, retries                 | Cobro Meta/provider, plantillas y throughput dependen de cuenta operativa no verificada.                                           |
| Pago tarjeta/SINPE/factura         | adapters CyberSource, webhook y referencia SINPE                                                              | pagos, % fee, refund, chargeback, factura                     | Credenciales/proveedor activo no verificados; no presumir fee ni settlement.                                                       |
| Soporte y operación humana         | No hay payroll/tickets/horas en repo                                                                          | minutos/ticket, implementación, tiempo de turno               | Mayor riesgo oculto B2B/B2G; registrar costo de onboarding/mes.                                                                    |
| Hardware/data GPS                  | Hardware y proveedor de producción no verificados                                                             | coste landed, conectividad, reemplazos/devoluciones           | No asignar margen de tracker hasta quote de proveedor.                                                                             |

El Bicep declara recursos, no despliegue. Pricing retail no produjo cotización utilizable; no rellenar tarifas manuales. Adquirir export/cost management de Azure por recurso/mes y tomar las facturas proveedoras antes de fijar mínimos.

## Break-even

No calculable sin costos fijos y variable marginal. Para modelo posterior:

`Clientes necesarios = (Azure base + personal/soporte + marketing fijo + overhead) / (ARPU neto * margen bruto)`.

Para productos locales, separar B2C, por-sede clínica, shelter subsidiado, contrato municipal, hardware, servicios profesionales y adquisición; mezcla total puede ocultar productos con margen negativo. Para clínicas/municipal incluir implementation fee o no ofertar gratis indefinidamente.

## Datos requeridos para reemplazar hipótesis

1. Revenue ledger: pago confirmado, refund, chargeback, IVA/fees, upgrades/downgrades, meses activos y consentimiento de precio.
2. Funnel de cohortes y atribución/costo por canal para CAC incremental.
3. Retención 30/90/180/365d por tier y segmento, motivos de cancelación y reactivación.
4. Entitlement consumption y costos por operación (Vision, Blob, mensajes, SQL, exports, video futuro).
5. Tickets/hora/onboarding/ventas y staffing real.
6. Margen y costo por proveedor/hardware, devolución, partner share.
7. Costos fijos mensuales/annualized, gasto total Azure de billing y actual resource SKU/region.
8. Test de willingness-to-pay separado de renovación y de precios actuales.

No usar el modelo ilustrativo en forecast de board/inversionistas sin reemplazar inputs y aprobarlo con finanzas. Ver [estrategia CR](NALA_CR_PRICING.md), [LATAM](NALA_LATAM_PRICING.md) y [resumen ejecutivo](NALA_PRICING_EXECUTIVE_SUMMARY.md).
