# Roadmap de monetización y upselling

**Corte:** 2026-09-28. Es una propuesta, no roadmap aprobado ni funcionalidad existente. Cada línea conserva el principio: seguridad y recuperación esenciales gratis; cobro transparente y consentimiento expreso.

## Qué vender y cómo empaquetar

| Segmento/oportunidad      | Modelo                                    | Trigger observable                                                           | Oferta destino                                             | Dependencias antes de cobrar                                                            | KPI                                                     |
| ------------------------- | ----------------------------------------- | ---------------------------------------------------------------------------- | ---------------------------------------------------------- | --------------------------------------------------------------------------------------- | ------------------------------------------------------- |
| Hogar: más de una mascota | Upgrade de plan                           | Usuario intenta registrar mascota fuera de MaxPets; evento backend denegado  | UserPlus / UserFamilia                                     | Confirmar límite y UX antes del intento; conservar acceso a emergencia                  | intentos que convierten, adopción 30d, cancelación      |
| Familia/cuidadores        | Upgrade de plan                           | Cuenta invita segundo adulto/cuidador o pide acceso compartido               | UserFamilia                                                | Permisos granulares, consentimientos, auditoría, cinco miembros totales explicados      | miembros activos, retención hogar, soporte/permisos     |
| Expediente/documentos     | Upgrade o aumento de capacidad            | Consulta repetida de historial, export, intento de documento o grant clínico | Familia o plan clínico                                     | Protección de salud, cuotas y exportaciones, flujo de consentimiento                    | archivos/exports por activo, grant aprobado, costo Blob |
| GPS/collar                | Add-on hardware y conectividad + software | Usuario pide registrar primer/otro collar o busca historial/zonas            | Add-on de dispositivo/data, independiente del plan base    | OEM/proveedor, cobertura CR, costo landed, SIM, batería, devolución, garantía           | activación, costo por dispositivo, falla, renovación    |
| Matching visual           | Add-on basado en consumo                  | Solicitud llega a cuota / demanda visual del caso                            | Cuota incluida acotada o pack consumible                   | Benchmark precision/recall, costo API, idempotencia, error sin cobro, seguridad         | costo por match aceptado, precision@k, falso positivo   |
| Clínica independiente     | Plan por sede                             | Escaneos/profesionales/exports frecuentes, solicitud de grant o report       | ClinicPlus -> ClinicPartner                                | Workflow diario, catálogo vigente, onboarding, soporte, control de certificados         | semanal activos/vet, renovación, minutos/valor          |
| Clínica multi-sede        | Contrato Enterprise (roadmap)             | Se intenta registrar otra sede o pide SSO/roles/API                          | Contrato por sede + implementación                         | Tenant isolation, SSO, API/versiones, audit, SLA, soporte y contrato                    | sede activa, expansion revenue, gross retention         |
| Refugio/ONG               | Patrocinio, subvención o licencia social  | Animal listing/volumen supera acceso básico, solicitud de campaña            | ShelterPlus patrocinado/contrato                           | Verificar resultados de adopción, fondos del sponsor, moderación                        | adopciones completas, costo soporte, continuidad ONG    |
| Municipalidad             | Servicio profesional + contrato anual     | Aumento de capturas/distritos/usuarios o report requerido                    | Piloto -> MuniFull/RedRegional                             | Procurement, datos, tenant, soporte, auditoría, residencia, SLA y revisión legal        | capturas resueltas, costo de implementación, renovación |
| Marketplace               | Cross-sell                                | Reserva/pedido aceptado por ambas partes                                     | Primero fee SaaS/listing; take-rate sólo luego             | Checkout, ledger, payout, refunds, dispute, fraude, fiscales y liquidez                 | conversión, GMV, tasa de cancelación, margen neto       |
| Telemedicina              | Add-on por sesión/paquete vía partner     | Consulta fuera de horario o solicitud de video                               | Fee por cita/sesión con consentimiento                     | Licencias locales, profesionales, proveedor de video, límites, documentación y soporte  | consulta completada, costo/min, recurrencia             |
| IA generativa             | Add-on de consumo o premium B2B futuro    | Flujo/rol solicita resumen; no trigger en código actual                      | Add-on token/caso sólo tras evaluación                     | No está implementada; RAG fuente citada, evals, safeguards, human approval, presupuesto | costo/operación, grounding, escalamiento y error        |
| Seguro                    | Cross-sell por tercero regulado           | Owner solicita cotización o partner autoriza offer                           | Comisión/lead afiliado consentido, no aseguramiento NALA   | insurer licenciado local, disclosures, claims/refunds, privacidad                       | leads válidos, activación, quejas y comisión neta       |
| API/Integraciones         | Add-on de integración/uso                 | Clínica/municipio necesita conexión y volumen mensurable                     | Paquete API / Enterprise                                   | API estable, keys, scopes, rate limits, versionado, support                             | uso APIs, error rate, costo por tenant                  |
| Soporte/SLA               | Add-on contractual                        | Cuenta solicita soporte con tiempos/turnos garantizados                      | Support tier / contrato                                    | staffing, runbooks, disponibilidad medida, incident response                            | costo por account, SLA attainment y CSAT                |
| Hardware QR/NFC           | Add-on físico                             | Usuario realiza pedido                                                       | Venta directa/pedido de partner, costo y entrega separados | supplier, fulfillment, NFC/QR testing, devoluciones y customer support                  | conversión QR, costo unitario, entrega/devolución       |

## Roadmap propuesto

### Próximos 30 días: sin paywalls nuevos

1. Crear catálogo interno único de features: estado, endpoint, entitlement, owner, variable cost, plan, approval y medición.
2. Conciliar `SubscriptionPricing`, `TIER_PRICE` frontend y catálogo API; bloquear displays con discrepancias.
3. Hacer tests de contrato de límites clave: MaxPets, MaxFamilyMembers, MaxActiveLostCases, AiMatchesPerCycle, MaxGpsCollars y cuotas por sujeto/contexto.
4. Montar consumo de entitlement/avisos 70%, 85%, 100% con consentimiento, sin upgrade automático; hoy el medidor no tiene call-site confirmado.
5. Instrumentar funnels y eventos de costo con IDs agregados, sin PII/GPS exacto; crear dashboard de cohortes.
6. Entrevistas de disposición a pagar con dueños, clínicas, ONG y municipio; precio del código es baseline, no resultado.

### 31-90 días: cerrar unit economics y piloto B2B

1. Ejecutar experimento B2C de precios con asignación estable, cohorts, IVA revisado y guardia de no degradar caso perdido.
2. Correr un piloto de Clínica con onboarding/scope acotado, uso semanal, grants, soporte y medición de tareas.
3. Crear oferta patrocinada para shelters, con outcome de adopción definido y no paywall de participación.
4. Definir cálculo por recurso (Blob, Vision, SQL, outbound email, push, SMS si se añade) y cap de gasto por caso.
5. Validar cobro/conciliación/refunds y notificación recurrente en sandbox contratado antes de vender suscripción automática.
6. Municipal: discovery con una institución, no replicar a otra sin procurement, impacto y costo de soporte.

### 3-12 meses: expandir sólo tras evidencia

- B2C: familia/guardian features, household billing y regionalized prices.
- B2B: clínica por sede/usuarios, auditoría, API, integrations y soporte comercial.
- Shelter: sponsorship/fundraising integrations sólo si está aprobado y medible.
- Marketplace: iniciar por reserva con política de cancelación y pagos externos bajo partner; habilitar payout/take-rate tras controles legales/financieros.
- GPS: add-on de partner con inventario/cobertura y costo total de propiedad calculado.
- Telemedicina: partner/API con credenciales profesionales y reglas por país, no build video first.
- IA: matching medido y resúmenes administrativos después de evaluación, privacidad, coste y aprobación humana. Nunca diagnóstico autónomo.
- Seguro: afiliación con compañía habilitada por país, no producto asegurador NALA.
- Enterprise: SSO, multi-tenant robusto, SLA, API, residencia, logs/exports y procurement antes de un tier explícito.

## Reglas de upsell ético

- Nunca bloquear perfil/QR, crear/consultar caso perdido, reportar sighting o acceder a información urgente por paywall.
- Mostrar costo, límite, fecha de reset, datos que se conservarán y opción no pagada antes del límite.
- No subir plan/cobrar excedente de forma automática; consentimiento verificable con precio, moneda y fecha.
- Para cuota no medida usar `REQUIERE VALIDACIÓN CON DATOS DE USO`, no inventar límite.
- Si servicio falla antes de procesarse, no consumir cuota; los reintentos deben ser idempotentes.
- Segmentación por conducta necesita consentimiento, privacy minimization y no explotar urgencia por mascota perdida.

Ver [matriz de features/upsell](NALA_FEATURE_PLAN_UPSELL_MATRIX.md), [CR pricing](NALA_CR_PRICING.md) y [economía unitaria](NALA_UNIT_ECONOMICS.md).
