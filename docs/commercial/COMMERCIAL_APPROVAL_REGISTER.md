# Registro canónico de aprobación comercial

**Estado:** `APROBADO_EN_PAWTRACKDEV`  
**Fecha de declaración:** 2026-10-01  
**Responsable de decisión:** Denis Avila Umaña, fundador y propietario  
**Actor Admin que persistió la aprobación:** `admin@pawtrack.cr` (`AA000001-0000-0000-0000-000000000001`)  
**Referencia:** `ACTA-COM-2026-10-01`  
**Fuente de catálogo:** `SubscriptionPlans` en `PawTrackDev`  
**Alcance:** publicación documental del catálogo técnico local, sujeta al gate de aprobación persistido por plan.

## Resultado de verificación

La aprobación se ejecutó el 2026-10-01 mediante `PUT /api/admin/subscription-plans/{id}/commercial-approval`, usando la versión vigente de cada plan y el actor Admin indicado. La consulta posterior confirmó 10 planes activos con `CommercialApprovalReference = ACTA-COM-2026-10-01`, fecha de aprobación y actor persistidos. El catálogo público devuelve los 10 planes.

Este documento registra la evidencia local de aprobación ejecutada por Admin. No acredita por sí solo despliegue en producción, aprobación fiscal/legal externa ni operación comercial fuera de `PawTrackDev`.

## Catálogo observado

| Tier              | Nombre           | Precio mensual CRC | Precio anual CRC | Activo | Publicable ahora            |
| ----------------- | ---------------- | -----------------: | ---------------: | -----: | --------------------------- |
| `ClinicPartner`   | Clínica Partner  |            ₡35.000 |                — |     Sí | Sí, catálogo local aprobado |
| `ClinicPlus`      | Clínica Plus     |            ₡15.000 |                — |     Sí | Sí, catálogo local aprobado |
| `MuniBasica`      | Municipal Básica |                  — |         ₡150.000 |     Sí | Sí, catálogo local aprobado |
| `MuniFull`        | Municipal Full   |                  — |         ₡300.000 |     Sí | Sí, catálogo local aprobado |
| `MuniRedRegional` | Red Regional     |                  — |         ₡500.000 |     Sí | Sí, catálogo local aprobado |
| `ShelterPlus`     | Refugio Plus     |             ₡8.000 |                — |     Sí | Sí, catálogo local aprobado |
| `StorePartner`    | Tienda Partner   |            ₡25.000 |                — |     Sí | Sí, catálogo local aprobado |
| `StorePlus`       | Tienda Plus      |            ₡12.000 |                — |     Sí | Sí, catálogo local aprobado |
| `UserFamilia`     | Familia          |             ₡4.990 |                — |     Sí | Sí, catálogo local aprobado |
| `UserPlus`        | Plus             |             ₡3.000 |                — |     Sí | Sí, catálogo local aprobado |

Los importes son los valores observados en la base local y no incluyen una conclusión fiscal sobre IVA ni acreditan precio de producción.

## Verificación posterior

El catálogo público fue verificado con 10 filas. Compras, promociones, downgrades y activaciones quedan sujetos a sus pruebas específicas; esta aprobación no modifica precios ni términos activos.
