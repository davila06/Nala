# NALA Identity and Access Inventory

**Cutoff:** 2026-09-28  
**Status:** `IMPLEMENTADO_SIN_PRUEBAS` as an inventory; endpoint-level coverage is incomplete.

## Roles found in code

`Owner`, `Ally`, `Admin`, `Clinic`, `Municipality`, `Store`, `ServiceProvider`, `Support`, and `SuperAdmin` are defined in [`UserRole.cs`](../../backend/src/PawTrack.Domain/Auth/UserRole.cs).

The role enum is evidence of declared roles only. A role is not considered fully validated until registration, assignment, endpoint enforcement, seed presence and positive/negative tests all agree.

## Authentication and claims

The API uses JWT bearer authentication configured in [`Program.cs`](../../backend/src/PawTrack.API/Program.cs). The observed claims include the user identifier, role, session identifier (`sid`), MFA (`mfa`) and platform role (`platform_role`). Token revocation/blocklist behavior is implemented through the authentication/session infrastructure, but complete revocation coverage is `NO_VERIFICADO` until the dedicated suite runs.

## Policies and enforcement

Observed policy families include:

- `SuperAdmin`: role, `platform_role=SuperAdmin` and `mfa=true`.
- Clinic and MFA policies: `ClinicFinanceMfa`, `ClinicStaffMfa`, `ClinicOperationsMfa`, `ClinicAdminMfa`, `MfaStepUp`.
- `HealthCheckPolicy`: Admin role.
- Rate-limit policies such as login, registration, refresh, data export, sightings, billboard events and handover verification.

Controller attributes and middleware are the effective enforcement surface. Frontend route guards and menus are not authorization evidence.

## Tenancy and ownership

No single universal `Tenant` aggregate was found. Isolation is implemented selectively through owner IDs, clinic organizations/sites/memberships, and explicit `TenantId`/`OrganizationId` fields where those bounded areas need them. The main persistence context is shared by these modules. Cross-tenant coverage must therefore be tested per resource family, not inferred from one global middleware.

## Unknowns requiring tests

- Complete role-to-endpoint matrix.
- Permission model versus role-only enforcement.
- Every public endpoint exception and its justification.
- Cross-tenant behavior for municipal, shelter, provider and clinic resources.
- Subscription expiry and entitlement behavior at each gate.
- Feature flag registration and runtime enforcement.
- Whether every seeded role is represented in the active local database.
