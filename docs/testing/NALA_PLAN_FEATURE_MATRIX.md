# NALA Plan and Feature Matrix

**Status:** implementation exists, coverage is partial.

## Encoded tiers

The enum in [`SubscriptionTier.cs`](../../backend/src/PawTrack.Domain/Subscriptions/SubscriptionTier.cs) defines:

- Owners: `Free`, `UserPlus`, `UserFamilia`.
- Clinics: `ClinicBasic`, `ClinicPlus`, `ClinicPartner`.
- Stores: `StoreBasic`, `StorePlus`, `StorePartner`.
- Shelters: `ShelterBasic`, `ShelterPlus`.
- Municipalities: `MuniBasica`, `MuniFull`, `MuniRedRegional`.

## Enforcement surface

Plan and entitlement behavior is implemented through subscription repositories, plan catalog entities and `IEntitlementService`/`EntitlementService`. The existence of a tier does not prove every feature gate is wired. Each feature requires a test for active, insufficient and expired subscription states.

| Capability family               | Code evidence                                            | Current status                                    |
| ------------------------------- | -------------------------------------------------------- | ------------------------------------------------- |
| Catalog and plan administration | subscription controllers, plan entities and repositories | `IMPLEMENTADO_SIN_PRUEBAS` for complete matrix    |
| Pet limits                      | entitlement service and pet commands                     | partial until every tier/limit is exercised       |
| Clinic operations               | clinic plan gates and MFA policies                       | partial                                           |
| Store/provider access           | store/provider handlers and plans                        | partial                                           |
| Shelter/municipal access        | role and domain surfaces                                 | `NO_VERIFICADO` for complete entitlement coverage |
| Feature flags                   | no unified runtime feature-flag service verified         | `NO_VERIFICADO`                                   |

Pricing, commercial approval and production activation are outside what source code can prove.
