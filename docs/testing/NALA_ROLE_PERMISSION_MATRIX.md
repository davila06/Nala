# NALA Role and Permission Matrix

**Status:** baseline extracted from executable authorization attributes; not a complete permission certification.

| Role            | Evidence observed                                                                            | Initial protected surfaces                       | Validation                                 |
| --------------- | -------------------------------------------------------------------------------------------- | ------------------------------------------------ | ------------------------------------------ |
| Owner           | `UserRole`, owner endpoints                                                                  | pets, family, sightings and personal data        | `SIN_PRUEBA_DE_SUITE_DEDICADA`             |
| Ally            | `UserRole`, controller attributes                                                            | adoptions, campaigns, welfare/ally operations    | `SIN_PRUEBA_DE_SUITE_DEDICADA`             |
| Admin           | controller role attributes and admin controllers                                             | administration, plans, providers, stores, audit  | partial integration coverage               |
| Clinic          | clinic controllers and clinic policies                                                       | clinical records, clinic operations and patients | partial; MFA/site scope requires expansion |
| Municipality    | municipal controllers                                                                        | municipal dashboard and campaigns                | `SIN_PRUEBA_DE_SUITE_DEDICADA`             |
| Store           | stores/orders controllers                                                                    | catalog and store orders                         | `SIN_PRUEBA_DE_SUITE_DEDICADA`             |
| ServiceProvider | provider controllers                                                                         | services, availability and bookings              | `SIN_PRUEBA_DE_SUITE_DEDICADA`             |
| Support         | welfare/support controllers                                                                  | restricted support operations                    | `SIN_PRUEBA_DE_SUITE_DEDICADA`             |
| SuperAdmin      | [`SuperAdminController`](../../backend/src/PawTrack.API/Controllers/SuperAdminController.cs) | exceptional platform operations                  | MFA/claim tests pending                    |

The code primarily enforces roles and policies; a repository-wide standalone permission catalog was not verified. Do not treat UI entitlements or documentation labels as backend permissions without a traced handler and test.
