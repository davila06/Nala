# Recomendación de planes y precios para Costa Rica

**Corte de recomendación:** 2026-09-28. **Revalidación de catálogo local:** 2026-10-01. Todas las cantidades recomendadas son hipótesis, no aprobación comercial, fiscal ni legal. `PawTrackDev` muestra UserPlus ₡3.000/mes; la tarifa de producción y pagos observados no se verificaron.

## Mercado y regla de decisión

La recomendación de producto es mantener el núcleo de recuperación gratuito y monetizar colaboración, organización, capacidad y workflow profesional. No hay en el repositorio datos de CAC, churn, ingresos cobrados, uso, costo por usuario o willingness-to-pay local. Por tanto el **mínimo viable económico no puede calcularse** hasta sumar costo real por segmento y margen requerido; no equiparar “precio más bajo que no duela” con precio rentable.

## Precio local observado frente a hipótesis de recomendación

| Segmento/tier técnico       | Precio observado en PawTrackDev |                                                                               Precio de prueba/recomendado CR |                             Adquisición agresiva (temporal) | Precio mínimo viable                                   | Condición                                                                                        |
| --------------------------- | ------------------------------: | ------------------------------------------------------------------------------------------------------------: | ----------------------------------------------------------: | ------------------------------------------------------ | ------------------------------------------------------------------------------------------------ |
| Dueño Free                  |                              ₡0 |                                                                                 ₡0 para recuperación esencial |                                                          ₡0 | No aplica                                              | Mantener QR/perfil, reporte perdido/hallazgo, contacto seguro, chat/handover, privacidad básica. |
| UserPlus                    |       ₡3.000/mes en PawTrackDev |                                                               Test ₡2.490/₡3.000/₡3.490; aprobación pendiente |             ₡1.990/mes por 3 meses, opt-in, no auto-upgrade | No calculable sin costo variable/margen                | Valor solo observado en BD local; no es tarifa aprobada ni evidencia de cobro.                   |
| UserFamilia                 |                      ₡4.990/mes |                                                                                     Test ₡3.990/₡4.990/₡5.990 |                              ₡2.990/mes por 3 meses, opt-in | No calculable sin costo y churn                        | Importe local observado, no aprobado; mostrar 25 mascotas máximo técnico.                        |
| ClinicBasic                 |           Sin precio codificado |                                                                                        Free piloto/directorio |               Sin cargo; sponsor si hay coste de onboarding | N/A sin alcance                                        | Medir escaneos, consentimientos, consultas y workflow semanal.                                   |
| ClinicPlus                  |                     ₡15.000/mes |                                      Piloto cotizado ₡10.000-₡15.000 por sede/mes sólo después de entrevistas |       1er mes piloto gratis o crédito, no precio permanente | No calculable sin onboarding, soporte y ahorro probado | Importe local observado, no aprobado; 500 escaneos/ciclo sujetos a enforcement.                  |
| ClinicPartner               |                     ₡35.000/mes |                                                       Cotizar ₡25.000-₡35.000/sede/mes con alcance contratado |                     No descuento de largo plazo hasta medir | No calculable                                          | Importe local observado, no aprobado; certificados/API requieren soporte y contrato.             |
| ShelterBasic                |           Sin precio codificado |                                                                                       Gratis/sponsorship base |                                                      Gratis | N/A                                                    | No restringir adopciones por incapacidad de pago de ONG.                                         |
| ShelterPlus                 |                      ₡8.000/mes |                                                                                 ₡0-₡8.000, subvención primero |                                   Patrocinado o 50% inicial | No calculable                                          | Importe local observado, no aprobado; cuotas no son SLA.                                         |
| StorePlus/Partner           |             ₡12.000/₡25.000 mes |                                             Congelar cobro como marketplace hasta checkout/inventario/soporte |                                    Pilot de catálogo gratis | No calculable                                          | Importes locales observados, no aprobados; no cobrar take rate ni representar compra liquidada.  |
| MuniBasica/Full/RedRegional |           ₡150k/₡300k/₡500k año | Cotización piloto por población, alcance, integraciones y servicio; usar esos valores sólo como ancla de test | Piloto con alcance limitado y precio institucional acordado | No calculable sin procurement, deployment/support      | Importes locales observados, no aprobados; contratación/renovación incompletas.                  |
| Proveedor Verified/Featured |           Sin precio codificado |                                                               Sin cargo hasta medir leads/booking atribuibles |  Trial técnico de 30 días no equivale a promoción comercial | N/A                                                    | Separar verificación de ranking; no vender reputación como garantía.                             |

**IVA:** `SubscriptionPricing` conserva helper de IVA 13%; los importes runtime vienen de `SubscriptionPlans`. Documentación comercial contradice el tratamiento según tipo de comprobante; pedir dictamen fiscal antes de comunicar “IVA incluido/exento”. El dato local no es precio final aprobado para consumidor.

## Precio psicológico por segmento

| Segmento                  |                                 Punto de prueba |                         Límite de precio a investigar | Qué paga el comprador realmente                                                                        | Métrica de valor                                                                                    |
| ------------------------- | ----------------------------------------------: | ----------------------------------------------------: | ------------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------- |
| Dueño individual          |        ₡3.000 Plus local / ₡4.990 Familia local |                      ₡1.990-₡5.990 por mes según tier | Menos fricción de cuidado/organización, más mascotas/personas e historial; no pagar por rescate básico | Conversión por cohortes, activación de beneficio, churn 30/90/180d, costo de soporte y uso marginal |
| Veterinaria independiente |                       ₡10.000-₡15.000 de piloto |                          Entrevistar bandas ₡10k-₡20k | Ahorro de administración, escaneo, comunicación/historial; sólo si encaja con su jornada               | usuarios activos/semana, tareas terminadas, minutos ahorrados, retención, soporte                   |
| Clínica/organización      |                        ₡25.000-₡35.000 cotizado |                 Entrevistar bandas ₡20k-₡50k por sede | workflow, equipo, permisos, registros y APIs contractuales                                             | adopción por veterinario, casos y exportaciones, expansión por sede, renovación                     |
| Refugio/ONG               |                Gratis o sponsor; posible ₡8.000 |                   ₡0-₡8.000 según subsidio y outcomes | Menos administración y mejor publicación/adopción                                                      | animales activos, adopciones cerradas, permanencia, costo de soporte por ONG                        |
| Municipalidad             | Piloto institucional cotizado; no lista pública | ₡150k-₡500k/anual es sólo rango que aparece en código | Padrón, reportes, campañas, auditoría y soporte local                                                  | cantones/distritos, capturas válidas, cierres, tiempo de respuesta, costo de implementación         |
| Dueño con collar          |                  Software sin hardware incluido |     Precio de device/data requiere cotización partner | hardware+conectividad+software                                                                         | costo total 12 meses, retención dispositivo, fallos y devoluciones                                  |

“Psicológico” es una hipótesis de empaquetado/redondeo, no un estudio de precios CR. Las propuestas ₡2.990/₡4.990 del corte 2026-09-28 son escenarios históricos; los tests nuevos deben guardar el precio/versión expuestos desde el catálogo objetivo, con IVA y condiciones claras.

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
