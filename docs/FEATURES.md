# PawTrack CR — Matriz de features por plan

> Fuente de verdad: enums y pricing del backend en `SubscriptionTier` y `SubscriptionPricing`.
> Revisión: 2026-09-06
> Estado: alineado con la implementación actual del código.

## 1. Nota importante sobre tiers reales

La implementación actual del backend usa estas suscripciones activas:

- B2C: `Free`, `UserPlus`, `UserFamilia`
- Tiendas: `StorePlus`, `StorePartner`
- Refugios/adopciones: `ShelterPlus`
- Clínicas: `ClinicPlus`, `ClinicPartner`
- Municipalidades: `MuniBasica`, `MuniFull`, `MuniRedRegional`

Los valores `ClinicBasic`, `StoreBasic`, `ShelterBasic` existen como estados o marcadores libres de directorio, pero no forman el flujo de pago/activación principal del código actual. La documentación debe tratar esos estados como free/entry points, no como planes de compra activados en producción.

---

## 2. B2C — Dueños de mascotas

| Feature                                      | Free           | UserPlus              | UserFamilia         |
| -------------------------------------------- | -------------- | --------------------- | ------------------- |
| Mascotas registradas                         | 1              | Hasta 3               | Ilimitadas          |
| Historial de escaneos QR                     | 5 últimos      | Ilimitado             | Ilimitado           |
| Búsqueda visual por IA                       | 3/mes          | Ilimitada             | Ilimitada           |
| Alertas por radio                            | 1x (básico)    | 3.33x (aprox. 10km)   | Sin límite efectivo |
| WhatsApp/Telegram/Facebook/Email             | No             | Sí                    | Sí                  |
| Case Room / coordinación                     | No             | Sí                    | Sí                  |
| GPS collar / Tractive / genérico             | No             | Sí                    | Sí                  |
| Bounty / recompensa económica                | No             | Sí                    | Sí                  |
| Miembros de familia                          | 1              | 1                     | Hasta 5             |
| Expediente médico                            | Preview/cuenta | Preview (3 registros) | Completo            |
| Peso por visita / medicación / recordatorios | No             | No                    | Sí                  |
| Exportar PDF médico                          | No             | No                    | Sí                  |

### Gates reales implementados

- `SubscriptionService.GetPetLimitAsync`:
  - `Free` = 1
  - `UserPlus` = 3
  - `UserFamilia` = -1 (ilimitado)
- `SubscriptionService.GetScanHistoryLimitAsync`:
  - `Free` = 5
  - `UserPlus`/`UserFamilia` = ilimitado
- `SubscriptionService.GetMonthlyAiSearchLimitAsync`:
  - `Free` = 3/mes
  - `UserPlus`/`UserFamilia` = sin límite
- `SubscriptionService.GetAlertRadiusMultiplierAsync`:
  - `Free` = 1.0
  - `UserPlus` = 3.33
  - `UserFamilia` = -1.0 (sin tope)

---

## 3. B2B — Tiendas de mascotas

| Feature                         | StorePlus | StorePartner |
| ------------------------------- | --------- | ------------ |
| Directorio y catálogo público   | Sí        | Sí           |
| Pedidos in-app                  | Sí        | Sí           |
| Checkout SINPE Móvil            | Sí        | Sí           |
| Gestión de pedidos              | Sí        | Sí           |
| Badge / destaque en mapa        | Sí        | Sí           |
| Analytics avanzados             | No        | Sí           |
| Multi-sucursal / multi-location | No        | Sí           |
| Posicionamiento prioritario     | No        | Sí           |

### Gating real

- El gate del backend exige `StorePlus` o `StorePartner` para pedidos, analytics y localizaciones avanzadas.
- La activación de plan sincroniza `Store.IsFeatured` y, para `StorePartner`, habilita locales adicionales.

---

## 4. Refugios / adopciones

| Feature                       | ShelterBasic    | ShelterPlus |
| ----------------------------- | --------------- | ----------- |
| Publicar animales en adopción | Hasta 5 activos | Ilimitado   |
| Ferias de adopción            | No              | Sí          |
| Pin destacado en mapa         | No              | Sí          |
| Panel de gestión              | Sí              | Sí          |
| Gestión de solicitudes        | Sí              | Sí          |

### Gating real

- `AdoptionCommands` usa `ShelterBasic` como límite de 5 animales activos y exige `ShelterPlus` para features premium de refugio.
- El dominio usa `ShelterPlus` como tier pagado para adopciones avanzadas.

---

## 5. B2B — Clínicas veterinarias

| Feature                           | ClinicPlus | ClinicPartner |
| --------------------------------- | ---------- | ------------- |
| Destacado en directorio/mapa      | Sí         | Sí            |
| Badge verificado                  | Sí         | Sí            |
| Estadísticas de escaneos          | Sí         | Sí            |
| Visibilidad / métricas            | Sí         | Sí            |
| Certificados PDF verificables     | Sí         | Sí            |
| API keys para integración         | No         | Sí            |
| Widget embebible                  | No         | Sí            |
| Logo en alertas cercanas          | Sí         | Sí            |
| Acceso a endpoints especializados | Sí         | Sí            |

### Gating real

- `TrackClinicView` y `GetClinicScanStats` exigen `ClinicPlus`.
- `ManageApiKey` y `GetNearbyActiveAlerts` exigen `ClinicPartner`.
- `IssueCertificate` / `IssueVaccinePassport` solo permiten `ClinicPartner`.

---

## 6. B2G — Municipalidades

| Feature                              | MuniBasica | MuniFull | MuniRedRegional |
| ------------------------------------ | ---------- | -------- | --------------- |
| Portal básico                        | Sí         | Sí       | Sí              |
| Fotos de animales capturados         | No         | Sí       | Sí              |
| Estadísticas / reportes              | No         | Sí       | Sí              |
| Multi-cantón                         | No         | No       | Sí              |
| Red regional / dashboard consolidado | No         | No       | Sí              |

### Facturación real

- Se facturan anualmente.
- Precios codificados en `SubscriptionPricing.AnnualPriceCrc`:
  - `MuniBasica` = ₡150,000/año
  - `MuniFull` = ₡300,000/año
  - `MuniRedRegional` = ₡500,000/año

---

## 7. Criterio de feature gating actual

El backend es la autoridad final para validar plan activo. En la práctica, la lógica implementada consiste en:

- Validar `IsActive` y `ExpiresAt` en repositorios/subscripciones antes de conceder acceso.
- Usar comparaciones por tier (`>=`, `==`, `is not`) para activar flujos premium.
- Mantener `Free` como acceso básico y no premium.
- Permitir upgrades/downgrades según el tier actual, pero con la lógica comercial final aún pendiente de consolidación en documentación y factura.

---

## 8. Estado de los documentos

La suma de features por plan se debe interpretar como la realidad actual del app, no como objetivos futuros. Los documentos definitivos deben respetar la siguiente fuente de verdad:

- `backend/src/PawTrack.Domain/Subscriptions/SubscriptionTier.cs`
- `backend/src/PawTrack.Domain/Subscriptions/SubscriptionPricing.cs`
- `backend/src/PawTrack.Infrastructure/Subscriptions/SubscriptionService.cs`
- handlers de activación/cancelación y queries de feature gating

Esto elimina contradicciones entre pricing, enums y feature gates del producto.

| Keyword                                                   | Respuesta                                        |
| --------------------------------------------------------- | ------------------------------------------------ |
| "adoptar", "adopcion", "quiero adoptar"                   | Link al directorio + ferias                      |
| "dar en adopcion", "tengo animales", "shelter", "refugio" | Instrucciones para registrarse como Ally Shelter |
