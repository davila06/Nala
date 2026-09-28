# Recomendación de planes y precios para Costa Rica

**Corte:** 2026-09-28. Todas las cantidades “recomendadas” son hipótesis de precio para validar, no aprobación comercial, fiscal ni legal. Los valores actuales proceden del enum/precio técnico y `PRICING_AND_PLANS.md`, no de compras observadas.

## Mercado y regla de decisión

La recomendación de producto es mantener el núcleo de recuperación gratuito y monetizar colaboración, organización, capacidad y workflow profesional. No hay en el repositorio datos de CAC, churn, ingresos cobrados, uso, costo por usuario o willingness-to-pay local. Por tanto el **mínimo viable económico no puede calcularse** hasta sumar costo real por segmento y margen requerido; no equiparar “precio más bajo que no duela” con precio rentable.

## Precio técnico actual frente a recomendación

| Segmento/tier técnico       | Precio CRC codificado |                                                                               Precio de prueba/recomendado CR |                             Adquisición agresiva (temporal) | Precio mínimo viable                                   | Condición                                                                                        |
| --------------------------- | --------------------: | ------------------------------------------------------------------------------------------------------------: | ----------------------------------------------------------: | ------------------------------------------------------ | ------------------------------------------------------------------------------------------------ |
| Dueño Free                  |                    ₡0 |                                                                                 ₡0 para recuperación esencial |                                                          ₡0 | No aplica                                              | Mantener QR/perfil, reporte perdido/hallazgo, contacto seguro, chat/handover, privacidad básica. |
| UserPlus                    |            ₡2.990/mes |                                                                           Mantener ₡2.990; test ₡2.490/₡3.490 |             ₡1.990/mes por 3 meses, opt-in, no auto-upgrade | No calculable sin costo variable/margen                | Comunicar sólo ventajas que tengan gate y uso comprobados; GPS como hardware/data aparte.        |
| UserFamilia                 |            ₡4.990/mes |                                                                                     Test ₡3.990/₡4.990/₡5.990 |                              ₡2.990/mes por 3 meses, opt-in | No calculable sin costo y churn                        | Mostrar 25 mascotas máximo técnico, hasta 5 miembros totales donde se aplique; no “ilimitado”.   |
| ClinicBasic                 | Sin precio codificado |                                                                                        Free piloto/directorio |               Sin cargo; sponsor si hay coste de onboarding | N/A sin alcance                                        | Medir escaneos, consentimientos, consultas y workflow semanal.                                   |
| ClinicPlus                  |           ₡15.000/mes |                                      Piloto cotizado ₡10.000-₡15.000 por sede/mes sólo después de entrevistas |       1er mes piloto gratis o crédito, no precio permanente | No calculable sin onboarding, soporte y ahorro probado | 500 escaneos/ciclo es dato de migración, confirmar enforcement.                                  |
| ClinicPartner               |           ₡35.000/mes |                                                       Cotizar ₡25.000-₡35.000/sede/mes con alcance contratado |                     No descuento de largo plazo hasta medir | No calculable                                          | Vender certificado/API/grant sólo con homologación, soporte, contrato y límites aprobados.       |
| ShelterBasic                | Sin precio codificado |                                                                                       Gratis/sponsorship base |                                                      Gratis | N/A                                                    | No restringir adopciones por incapacidad de pago de ONG.                                         |
| ShelterPlus                 |            ₡8.000/mes |                                                                                 ₡0-₡8.000, subvención primero |                                   Patrocinado o 50% inicial | No calculable                                          | Acreditar volumen y ahorro; 500 animales y 24 ferias/año son cuotas migradas, no SLA.            |
| StorePlus/Partner           |   ₡12.000/₡25.000 mes |                                             Congelar cobro como marketplace hasta checkout/inventario/soporte |                                    Pilot de catálogo gratis | No calculable                                          | No cobrar take rate ni representar pedido como compra liquidada.                                 |
| MuniBasica/Full/RedRegional | ₡150k/₡300k/₡500k año | Cotización piloto por población, alcance, integraciones y servicio; usar esos valores sólo como ancla de test | Piloto con alcance limitado y precio institucional acordado | No calculable sin procurement, deployment/support      | Tiers y precios anuales modelados, contratación/renovación incompletas.                          |
| Proveedor Verified/Featured | Sin precio codificado |                                                               Sin cargo hasta medir leads/booking atribuibles |  Trial técnico de 30 días no equivale a promoción comercial | N/A                                                    | Separar verificación de ranking; no vender reputación como garantía.                             |

**IVA:** `SubscriptionPricing` incluye helper de IVA 13% y documentación comercial contradice el tratamiento según tipo de comprobante; pedir dictamen fiscal antes de comunicar “IVA incluido/exento”. Los importes arriba son base técnica, no precio final para consumidor.

## Precio psicológico por segmento

| Segmento                  |                                 Punto de prueba |                         Límite de precio a investigar | Qué paga el comprador realmente                                                                        | Métrica de valor                                                                                    |
| ------------------------- | ----------------------------------------------: | ----------------------------------------------------: | ------------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------- |
| Dueño individual          |                    ₡2.990 Plus / ₡4.990 Familia |                      ₡1.990-₡5.990 por mes según tier | Menos fricción de cuidado/organización, más mascotas/personas e historial; no pagar por rescate básico | Conversión por cohortes, activación de beneficio, churn 30/90/180d, costo de soporte y uso marginal |
| Veterinaria independiente |                       ₡10.000-₡15.000 de piloto |                          Entrevistar bandas ₡10k-₡20k | Ahorro de administración, escaneo, comunicación/historial; sólo si encaja con su jornada               | usuarios activos/semana, tareas terminadas, minutos ahorrados, retención, soporte                   |
| Clínica/organización      |                        ₡25.000-₡35.000 cotizado |                 Entrevistar bandas ₡20k-₡50k por sede | workflow, equipo, permisos, registros y APIs contractuales                                             | adopción por veterinario, casos y exportaciones, expansión por sede, renovación                     |
| Refugio/ONG               |                Gratis o sponsor; posible ₡8.000 |                   ₡0-₡8.000 según subsidio y outcomes | Menos administración y mejor publicación/adopción                                                      | animales activos, adopciones cerradas, permanencia, costo de soporte por ONG                        |
| Municipalidad             | Piloto institucional cotizado; no lista pública | ₡150k-₡500k/anual es sólo rango que aparece en código | Padrón, reportes, campañas, auditoría y soporte local                                                  | cantones/distritos, capturas válidas, cierres, tiempo de respuesta, costo de implementación         |
| Dueño con collar          |                  Software sin hardware incluido |     Precio de device/data requiere cotización partner | hardware+conectividad+software                                                                         | costo total 12 meses, retención dispositivo, fallos y devoluciones                                  |

“Psicológico” es una hipótesis de empaquetado/redondeo, no un estudio de precios CR. Redondear a ₡2.990/₡4.990 funciona como punto de test sólo si IVA, precio total y condiciones se comunican transparentemente.

## Valor frente a alternativas

- GPS global cobra hardware y conectividad; NALA no debe comparar suscripción aislada con Tractive/Pawfit sin sumar costo de hardware, cobertura y plan.
- PetHub/QR y plataformas de registro comparan en identidad/recuperación, pero no prueban que su tarifa, recovery claim o cobertura aplique en CR.
- PIMS/telemedicina se cotizan a menudo mediante demo y tienen workflow profesional más amplio; el código NALA no permite justificar competir en precio con ellos aún.
- El mejor diferenciador local potencial es QR + recuperación + historia compartible + partners de Costa Rica, no la amplitud del listado de features.

## Precio y aprobación

1. No publicar monto hasta comprobar flags `PRICING_APPROVED`, aprobación legal/IVA y publicación del catálogo vigente.
2. Informar moneda, período, IVA/comprobante, renovación, cancelación, límites, cambio de plan y qué sucede al expirar.
3. Mantener precio legado para suscriptores hasta consentimiento/aviso contractual de cambio.
4. No cobrar automáticamente por excedente ni upgrade; mostrar consumo/límite antes de bloqueo.
5. Separar hardware, SMS, video y consumo IA como costes/add-ons consentidos cuando existan.

## Plan de experimento 30-60 días

1. Entrevistar 15-20 dueños por segmento y 8-12 clínicas/ONGs, no tratar respuestas como muestra estadística representativa.
2. Test Plus/Familia con exposición aleatoria a los tres precios sugeridos; conservar el precio real aceptado y respetar consumidores actuales.
3. Medir valor realizado, no clic: pago, activación de feature, uso repetido, costos, cancelación y motivo.
4. Para B2B cobrar piloto por alcance, incorporar onboarding/soporte como costo separado y revisar renovación.
5. Publicar sólo si margen unitario, retención y soporte por cuenta pasan umbral aprobado por finanzas/operación.

Enlaces: [tiers fuente](../backend/src/PawTrack.Domain/Subscriptions/SubscriptionTier.cs), [precio base](../backend/src/PawTrack.Domain/Subscriptions/SubscriptionPricing.cs), [plan comercial condicionado](PRICING_AND_PLANS.md), [unit economics](NALA_UNIT_ECONOMICS.md).
