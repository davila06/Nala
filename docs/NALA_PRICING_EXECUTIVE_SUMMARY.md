# Resumen ejecutivo: qué vende NALA y a qué precio debería probar

**Corte de repositorio y mercado:** 2026-09-28. **Estado:** análisis de producto y propuesta; no aprobación comercial, fiscal, legal ni forecast financiero.

## Estado real de los planes

El código modela tiers `Free`, `UserPlus`, `UserFamilia`, ClinicBasic/Plus/Partner, StoreBasic/Plus/Partner, ShelterBasic/Plus y MuniBasica/Full/RedRegional. `Professional/Verified/Featured` es membresía separada de proveedores. **No existe tier Enterprise**, ni un único catálogo `Essential/Premium/Veterinary`. Precios codificados: dueños ₡2.990/₡4.990 al mes; Clinic ₡15.000/₡35.000; Store ₡12.000/₡25.000; Shelter ₡8.000; Municipios ₡150k/₡300k/₡500k anual. Son valores técnicos sujetos a approvals y no prueban que el cliente los haya pagado.

Hay catálogo, activación, cancelación, upgrade/downgrade, add-ons y servicio de entitlements, pero catálogo persistido, gates y fallback no son uniformes. “Familia ilimitada” es incorrecto; `MaxPets=25`. El billing recurrente carece de renovación automática universal; sin tarjeta guardada, renovación manual. Véase [plan mapping](NALA_PLAN_MAPPING.md).

## Qué puede vender realmente hoy

- **Base gratuita:** identidad/QR digital, parte del flujo de pérdida/hallazgo, relato de avistamientos, relay/chat/handover, datos básicos de salud. Confirma limitación por endpoint antes de publicación; no cobrar por emergencia.
- **Suscripción B2C técnica:** más mascotas/miembros/historial, funciones de GPS/matching en ciertas rutas; conversión, costo y disponibilidad del dispositivo externos no verificados.
- **Clínica:** portal y módulos de escaneo/registro/grants/certificados; precio y contrato sujetos a aprobación, integración diaria/operación externa no verificada.
- **Refugio/municipio:** perfiles, adopción/reportes/capturas; operar/contratar/renovar institucionalmente aún requiere evidencia y soporte.
- **No vender como activo:** video/telemedicina, OpenAI/RAG/agentes, seguro, hardware propio, marketplace con payout/escrow/comisión/refund, SLA/24x7 o integración oficial SENASA.

## Recomendación de precios para pruebas, no publicación

| Plan                   | Público objetivo             | Features permitidas/recomendadas                                                               |                                              Precio CR | Precio LATAM                                         |                              Precio global |
| ---------------------- | ---------------------------- | ---------------------------------------------------------------------------------------------- | -----------------------------------------------------: | ---------------------------------------------------- | -----------------------------------------: |
| Free                   | Dueño y finder               | QR/perfil, caso perdido/hallazgo, contacto seguro, sighting, privacidad y exportación esencial |                                                     ₡0 | Gratis en todos los países                           |                                     Gratis |
| Plus                   | Dueño individual             | Extras de comodidad/capacidad; no paywall de seguridad                                         |           Test ₡2.490 / ₡2.990 / ₡3.490; código ₡2.990 | `P_CR × índice local`, local currency; test por país |                     US$4,99 como hipótesis |
| Familia                | Hogar/cuidadores             | 25 mascotas máximo, hasta 5 miembros, compartir, recordatorios/historial según gate            |           Test ₡3.990 / ₡4.990 / ₡5.990; código ₡4.990 | `P_CR × índice local`; validar ciudad/FX             |                     US$7,99 como hipótesis |
| Clínica Basic          | Independiente                | Directorio/escaneo base                                                                        |                                              ₡0 piloto | Free piloto local                                    |                                        N/A |
| ClinicPlus             | Clínica/sede                 | Escaneo/visibilidad/funciones comprobadas                                                      |                        Cotizar test ₡10k-₡15k/sede/mes | Precio local y tributación; revalidar margen         | Cotización SaaS local, no tarifa universal |
| ClinicPartner          | Clínica integrada            | API/certificados sólo tras autorización y soporte                                              |                           ₡25k-₡35k/sede/mes en piloto | Contrato local + onboarding                          |                      Cotización enterprise |
| ShelterBasic / Plus    | Refugio/ONG                  | Publicación/adopción; protección de misión vía subsidio                                        |                            Gratis/sponsor; evaluar ₡8k | Free/sponsor y partner local                         |                          Subsidio/contrato |
| Municipality tiers     | Gobierno local/regional      | Capturas/reportes por alcance y contrato                                                       | Piloto a cotizar; código da ₡150k-₡500k/año como ancla | Cotización país/municipio/procurement                |                  Contrato + implementación |
| Enterprise (propuesto) | Redes clínicas/instituciones | SSO/API/tenant/SLA sólo cuando exista                                                          |                                            No publicar | No publicar                                          |      Quote only después de build/seguridad |

Banda LATAM orientativa: México 0,75-0,95; Colombia 0,65-0,85; Chile 0,95-1,15; Perú 0,55-0,75; Panamá 1,00-1,25; Argentina 0,65-1,00 con repricing; Dominicana 0,70-0,90; Guatemala 0,45-0,65 multiplicado por precio CR, no importes convertidos listos para vender. Las bandas son hipótesis sobre PPA y entrevistas pendientes. Global en USD también requiere tax/payment localization.

## Precios: razón, comparación, riesgo e ingreso potencial

- Mantener puntos CRC de Plus/Familia como baseline evita cambiar la única referencia codificada; variar en tests para medir conversión/retención.
- La escalera Familia agrega miembros y más capacidad por sólo ₡2.000 extra sobre Plus, una diferencia potencialmente fácil de entender; validar costo por uso y cannibalization.
- B2B exige implementation/support. No deducir que precio anual municipal cubre servicio ni licitación.
- Competidores de GPS empaquetan hardware+conectividad; telemed/PIMS compiten con software/operación más profunda. NALA no tiene dato comparable de precio local.
- Riesgo principal: cobro por producto parcial o incumplir SLA/IVA; el upside depende de conversión y densidad, no del número de tiers.
- Potencial ingreso ilustrativo: ₡114.820 / ₡826.000 / ₡4.652.500 bruto mensual equivalente bajo conteos supuestos; no forecast ni net revenue. Ver [unit economics](NALA_UNIT_ECONOMICS.md).

## Competencia y posicionamiento

Diferenciador potencial: QR + flujo de recuperación + expediente compartido + red de clínica/refugio/municipio, localizado en Costa Rica. Competidores tienen hardware/GPS, PIMS y telemedicina, marketplace con pagos/protección o seguro con underwriting. Evidencia oficial pública consultada en 2026-09-28 incluye Tractive, Pawfit, PetHub, Vetster, PetDesk, Digitail, Wag, Rover, Pumpkin y Figo. Fi/Whistle redirigieron; Airvet respondió 429; Dutch/ Banfield redirigieron, PetPage no se extrajo, Chewy aplicó challenge. No adjudicarles capacidades/precios no capturados.

## Escenarios de ingresos

| Escenario hipotético |                Dueños/mes | MRR bruto equivalente | Qué debe ser cierto                                                                                |
| -------------------- | ------------------------: | --------------------: | -------------------------------------------------------------------------------------------------- |
| Conservador          |  1.000 MAU, 1% conversión |        ₡114.820 total | 10 pagos hogar + 3 cuentas B2B; sin costo descontado                                               |
| Esperado             |  5.000 MAU, 3% conversión |        ₡826.000 total | 150 suscriptores hogar + 22 cuentas clinic/shelter/municipio; precio y venta aprobados             |
| Agresivo             | 20.000 MAU, 5% conversión |      ₡4.652.500 total | 1.000 suscriptores hogar + 95 cuentas B2B; no forecast: distribución/soporte/capacidad por validar |

CAC, LTV, margen realizado y break-even neto: `NO_VERIFICADO`. El análisis de [unit economics](NALA_UNIT_ECONOMICS.md) presenta sensibilidad de LTV y un techo matemático de CAC para payback a seis meses bajo supuestos, no mediciones. El precio mínimo viable no es calculable sin Azure invoices, costos SMS/email/pagos, soporte, adquisición y churn.

## Features premium faltantes y orden estratégico

No subir al plan pagado todavía: video, IA generativa, seguro, payout, garantía GPS, SLA/24x7, multi-tenant enterprise. Primero faltan gates completos, medición/costos, integración/partner, soporte humano, consentimiento, safeguards y contrato. [Roadmap](NALA_ROADMAP_MONETIZATION.md).

## Top 25 mejoras prioritarias

1. Fuente única catálogo/precio/moneda/periodicidad/IVA/approval.
2. Validar flags de aprobación antes de mostrar/activar pago.
3. Conciliar `SubscriptionPricing`, UI, catálogo público y marketing.
4. Corregir copy de Familia “ilimitado” a 25 máximo o cambiar código.
5. Completar pruebas por entitlement/tier/contexto.
6. Consultar catálogo activo de `SubscriptionPlans`/`PlanEntitlements` por ambiente.
7. Terminar admin CRUD de definiciones y versionado efectivo.
8. Instrumentar consumo, costos, idempotencia y eventos de denegación.
9. Montar medidor de consumo/avisos 70/85/100 con consentimiento.
10. Mantener rutas de recuperación esenciales gratis/ininterrumpidas.
11. Separar rol/tenant y subject id en B2B billing.
12. Definir downgrade/read-only/limpieza sin borrar historial necesario.
13. Validar provider credentials y webhook signing/reconciliation end-to-end.
14. Documentar retries, refunds, dispute, failed renewals y support.
15. Medir CAC y cohortes de retención por canal/tier.
16. Medir costo de Blob/SQL/Vision/email/push por cohortes.
17. Desarrollar piloto clinic con workflow semanal antes de vender Partner.
18. Determinar Clinic API/Certificate contract + roles + key rotation.
19. Crear sponsorship/refuge model and outcome reporting.
20. No habilitar marketplace commission sin ledger/payout/dispute.
21. Validar GPS hardware/coverage/support per Costa Rica and quote.
22. Localizar currency/tax/payment providers by country.
23. Introducir country config/tenant, retention and legal review for LATAM.
24. Definir partner/deployment architecture before Enterprise/SLA.
25. Mantener source/evidence audit trail para todos claims comerciales.

## Quick wins 30 días

- Remover “ilimitado” del copy si sigue código en 25.
- Bloquear claims de GPS/AI/telemed/insurance/marketplace not proven.
- Ejecutar la primera entrevista/price-test planificado y construir event metrics.
- Revisar el estado de approvals y price displays en staging sin cambiar prices production.
- Hacer un presupuesto por unidad de Vision, Blob, email/push y soporte; no usar facturas no confirmadas.

## Roadmap

- **90 días:** completar entitlement enforcement, cerrar pagos/IVA/reconciliation, piloto clinic/shelter con scorecard, B2C experiments por cohorte, baseline CAC/retention, country readiness checklist para PAN o el siguiente mercado seleccionado.
- **12 meses:** seleccionar expansión por evidencia (no secuencia fija), provider marketplace controlled, local currency/taxes/recurring billing, clinic integrations, GPS add-on via vetted partner, possibly video/insurance via licensed local partner, Enterprise only after security/tenant/SLA operations.

## Riesgos

Aprobación comercial/fiscal no verificada; compra de terceros/producción no probada; costos unitarios, demanda y churn ausentes; competencia comparada por páginas de marketing; controles de salud/GPS y datos personales; excesivo scope frente a capacidad del equipo. Decisión inmediata recomendada: mantener Free de recuperación y testear sólo B2C ₡2.990/₡4.990 con aprobación, while B2B piloted/quoted and no false recurring/enterprise claims.
