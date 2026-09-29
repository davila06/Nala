# NALA User Types

**Status:** extracted from executable role and domain evidence; not a commercial persona catalogue.

| Type                   | Role/data evidence                         | Current test status                  |
| ---------------------- | ------------------------------------------ | ------------------------------------ |
| Pet owner              | `Owner`, pets, family and recovery flows   | partial                              |
| Ally/shelter           | `Ally`, adoption and welfare surfaces      | not verified as isolated tenant      |
| Platform admin         | `Admin`, admin controllers                 | partial integration coverage         |
| Clinic operator        | `Clinic`, clinics, organizations and sites | partial; site tests exist            |
| Municipality operator  | `Municipality`, municipal module           | not verified as paired tenant        |
| Store operator         | `Store`, stores and orders                 | not verified as dedicated auth suite |
| Service provider       | `ServiceProvider`, catalog/bookings        | not verified as dedicated auth suite |
| Support operator       | `Support`, welfare/support endpoints       | not verified as scoped support suite |
| Platform super-admin   | `SuperAdmin`, MFA/platform claim policy    | not verified end-to-end              |
| Anonymous/public actor | public controllers and anonymous routes    | public exception inventory pending   |
| Collar/device identity | device-key middleware and collar routes    | partial; not a human user role       |

A user type is considered complete only when identity, role, resource scope, plan and negative authorization behavior are tested together.
