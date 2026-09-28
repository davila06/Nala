# Mapeo real de planes y features

**Corte:** 2026-09-28. **Autoridad:** enums/código, migración de entitlements, fallbacks y rutas; no implica venta aprobada ni estado de una base desplegada.

## Nombres detectados en código

`SubscriptionTier` define `Free`, `UserPlus`, `UserFamilia`, `ClinicBasic`, `ClinicPlus`, `ClinicPartner`, `StoreBasic`, `StorePlus`, `StorePartner`, `ShelterBasic`, `ShelterPlus`, `MuniBasica`, `MuniFull` y `MuniRedRegional` ([enum](../backend/src/PawTrack.Domain/Subscriptions/SubscriptionTier.cs)). Las membresías de proveedores son otra dimensión: `Free`, `Verified`, `Featured` ([ProviderMembershipTier](../backend/src/PawTrack.Domain/ServiceProviders/ProviderMembershipTier.cs)); no tienen precio en `SubscriptionPricing`. No existe `Enterprise`, ni tier llamado `Premium`, `Family`, `Veterinary`, `Essential` o `Professional` como valor canónico. UI traduce UserPlus a “Plus” y UserFamilia a “Familia”.

[Precios CRC codificados](../backend/src/PawTrack.Domain/Subscriptions/SubscriptionPricing.cs) son importes técnicos base, sin afirmar cobro efectivo ni precio aprobado. Anual: 20% de descuento para B2C; tiers municipales son anuales. La guía [PRICING_AND_PLANS.md](PRICING_AND_PLANS.md) documenta la política de aprobación; [Test-GoLiveGovernance.ps1](../scripts/Test-GoLiveGovernance.ps1) se ejecuta en el workflow de despliegue productivo. No se encontró enforcement de esas flags en endpoints de compra, catálogo o UI, y no se consultó el estado real de variables productivas.

## Plan actual: precio y límites de entitlement observados

La migración [AddEntitlementCatalogAndConsumption](../backend/src/PawTrack.Infrastructure/Persistence/Migrations/20260921203255_AddEntitlementCatalogAndConsumption.cs) inserta definiciones sólo si existen filas `SubscriptionPlans` para esos tiers. Este repositorio no demuestra cuáles filas hay en la base activa. `EntitlementService` usa catálogo persistido para tiers con plan cargado; si no hay definición intenta fallback legacy, y sin fallback deniega. Por eso “configurado en migración” y “activo en runtime” son estados distintos.

| Tier técnico                     | Precio de referencia codificado | Superficie              | Entitlements/límites con evidencia                                                                                                                                                                                                  | Venta                                             |
| -------------------------------- | ------------------------------- | ----------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------- |
| Free                             | ₡0                              | Dueño / acceso base     | `MaxPets=1`; FreeEntitlements() fija MaxClinicScansPerCycle=25/ciclo; 1 promoción/ciclo; vista previa médica 3; límites gratis adicionales a validar por consumidor                                                                 | Base gratuita técnica                             |
| UserPlus                         | ₡2.990/mes                      | Dueño                   | 3 mascotas; 3 casos perdidos simultáneos; matching 10/ciclo, 15 candidatos; difusión 5/caso/día; 1 collar; preview médico 3; retención scan 365 días                                                                                | Precio requiere aprobación                        |
| UserFamilia                      | ₡4.990/mes                      | Hogar                   | 25 mascotas (no ilimitadas); hasta 5 miembros totales; 50 recordatorios; 10 casos; matching 30/ciclo y 35 candidatos; 5 collares; difusión 10/caso/día; retención scan 3.650 días                                                   | Precio requiere aprobación                        |
| ClinicBasic                      | No codificado                   | Directorio/escaneo base | Sin precio/lista de cuotas en el mapa revisado                                                                                                                                                                                      | Base técnica, no contrato                         |
| ClinicPlus                       | ₡15.000/mes                     | Clínica                 | 500 escaneos/ciclo; certificados 0/ciclo en migración                                                                                                                                                                               | Requiere contrato/precio/SLA aprobado             |
| ClinicPartner                    | ₡35.000/mes                     | Clínica                 | 5.000 escaneos; 10 API keys; 500 certificados; 250 pasaportes; 20 exportaciones médicas/ciclo; 25 veterinarios autorizados                                                                                                          | Requiere contrato/precio/SLA aprobado             |
| StoreBasic                       | No codificado                   | Tienda                  | Free fallback: 10 productos, 1 sede, 0 pedidos habilitados/ciclo; confirmar gate consumidor                                                                                                                                         | Directorio/base técnica                           |
| StorePlus                        | ₡12.000/mes                     | Tienda                  | 100 productos; 250 pedidos/ciclo en migración y fallback                                                                                                                                                                            | No vender como checkout/liquidación PawTrack      |
| StorePartner                     | ₡25.000/mes                     | Tienda                  | 1.000 productos; 2.500 pedidos/ciclo; 5 sedes; 20 exportaciones/ciclo; import 1.000                                                                                                                                                 | Multi-sede/payment ops no verificadas como oferta |
| ShelterBasic                     | No codificado                   | Refugio/aliado          | Free fallback: hasta 5 animales adoptables; revisar sujeto/contexto que consulta el entitlement                                                                                                                                     | Acceso base técnico                               |
| ShelterPlus                      | ₡8.000/mes                      | Refugio                 | 500 animales; 24 ferias/año en migración; fallback no cubre todas las cuotas                                                                                                                                                        | Requiere aprobación/contrato                      |
| MuniBasica                       | ₡150.000/año                    | Municipalidad           | 500 capturas/año                                                                                                                                                                                                                    | Tier modelado, compra/renovación incompleta       |
| MuniFull                         | ₡300.000/año                    | Municipalidad           | 5.000 capturas/año; lote 500                                                                                                                                                                                                        | Tier modelado, compra/renovación incompleta       |
| MuniRedRegional                  | ₡500.000/año                    | Red regional            | 30.000 capturas/año; lote 2.000; dashboard regional, transferencias inter-cantón, API booleanas                                                                                                                                     | Tier modelado, compra/renovación incompleta       |
| Proveedor Free/Verified/Featured | No codificado                   | Profesional/servicios   | `HasCatalogAccess` se basa en tier distinto a suscripción; trial único Verified de 30 días en primera aprobación; no se reinicia al reactivar [ServiceProvider](../backend/src/PawTrack.Domain/ServiceProviders/ServiceProvider.cs) | No pricing aprobado                               |

> **Precisión:** `FreeEntitlements()` fija algunas claves globalmente; `GetLegacyEntitlement()` sólo contiene un subconjunto para tiers pagados. La tabla no afirma que toda clave se aplique en cada ruta. `StoreBasic`/`ShelterBasic` no deben interpretarse como subscripciones mensuales.

### Correcciones P0 implementadas en código

- Free tiene `MaxFamilyMembers=1` (sólo titular); UserFamilia configura 5 integrantes totales.
- Cada invitación pendiente reserva una plaza; el máximo de invitaciones pendientes es 3. API y UI muestran conteo pendiente/cupo y el servidor serializa invitaciones y aceptaciones con locks distribuidos por cuenta y usuario.
- Escanear QR/RFID identifica y registra contacto, pero no concede lectura del historial ni escritura clínica. Lectura requiere grant activo `read`; escritura requiere grant activo `write`; ambos son aprobados por el dueño y revocables.
- La compra nueva usa el precio del plan activo del catálogo. La renovación usa `Subscription.AmountCrc` aceptado para ese término; un importe promocional cero no inicia cobro automático sin una nueva aceptación.
- El frontend ya no presenta tarifas locales como fallback, ni afirma IA/movimiento/hardware ilimitados o un bundle GPS no verificado.

## Diferencias entre familia estratégica y tiers actuales

| Rótulo solicitado | Correspondencia real posible                                                      | Estado de correspondencia                                         |
| ----------------- | --------------------------------------------------------------------------------- | ----------------------------------------------------------------- |
| Free              | `Free`; bases `ClinicBasic`, `StoreBasic`, `ShelterBasic` son productos distintos | Existe, varios sujetos; no un producto único                      |
| Basic             | `ClinicBasic`, `StoreBasic`, `ShelterBasic`                                       | Tiers base; no comparten features ni enforcement                  |
| Premium           | `UserPlus`, `ClinicPlus`, `StorePlus` sólo como lectura comercial                 | No existe enum Premium; no cruzar segmentos                       |
| Family            | `UserFamilia`                                                                     | Nombre de UI “Familia”; valor técnico `UserFamilia`               |
| Veterinary        | `ClinicBasic/Plus/Partner`                                                        | Segmento, no tier                                                 |
| Shelter           | `ShelterBasic/Plus`                                                               | Familia institucional modelada                                    |
| Municipality      | `MuniBasica/Full/RedRegional`                                                     | Familia institucional modelada con compra anual incompleta        |
| Professional      | Proveedor `Free/Verified/Featured`                                                | Membresía de publicación, no SubscriptionTier                     |
| Enterprise        | Ninguno                                                                           | Propuesta futura; no existe tier, SSO/white-label/SLA contractual |
| Add-on            | `SubscriptionAddon`, APIs/admin                                                   | Entidad/flujo técnico; no equivale a add-on comercial publicado   |

## Qué significa actualmente “feature de plan”

- Los gates frontend de [PlanGate](../frontend/src/features/pets/components/PlanGate.tsx) usan etiquetas Plus/Familia y no sustituyen autorización de backend.
- Rutas/hhandlers consultan entitlements de forma no uniforme. Ejemplos: crear mascota (`MaxPets`), registrar collar (`MaxGpsCollars`), visual matching (`AiMatchesPerCycle`), pérdida (`MaxActiveLostCases`).
- Una definición en la migración no prueba que el `SubscriptionPlan` correspondiente exista en base o que el handler compruebe esa clave.
- El `EntitlementMeter` no se encontró montado en una ruta de producto; no se acredita advertencia de consumo al 70/85/100%.
- B2C permite plazos técnicos 1, 3, 6 o 12 meses; renovación y captura de tarjeta no significan cobro recurrente universal.

## Recomendación de taxonomía sin falsear lo actual

Conservar los tiers existentes en el catálogo técnico. Para estrategia, agrupar por **persona** (hogar, clínica, tienda, refugio, municipalidad, proveedor) y **paquete** (base, pago, regional) sin renombrar enums. No introducir un plan Enterprise hasta que haya aislamiento/tenant, SSO, API comercial versionada, soporte, SLA, seguridad, procurement y billing contractual. No usar el mismo “Premium” para hogar y clínica.

Ver [FEATURES](FEATURES.md), [límites](commercial/PLAN_LIMITS.md), [matriz actual de plan](commercial/PLAN_FEATURE_MATRIX.md), [valoración](NALA_PLAN_VALUE_ANALYSIS.md) y [matriz maestra](NALA_FEATURE_PLAN_UPSELL_MATRIX.md).
