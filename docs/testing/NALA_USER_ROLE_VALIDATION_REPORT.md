# NALA User and Role Validation Report

**Cutoff:** 2026-09-28  
**Scope:** local repository, LocalDB `PawTrackDev`, local API/frontend only.

## Executive result

The solution builds and the complete backend solution test run passed **1,820 tests, 0 failures, 0 skips**. The six synthetic SQL seed scripts passed twice after fixing clinic organization site cleanup. The authorization baseline contains five passing tests and the new .NET Playwright smoke project contains one passing test.

This is not a claim of complete enterprise authorization coverage. The remaining matrix is explicitly documented as partial or not verified.

## Inventory

- Roles: `Owner`, `Ally`, `Admin`, `Clinic`, `Municipality`, `Store`, `ServiceProvider`, `Support`, `SuperAdmin`.
- Authentication: JWT bearer with role, session, MFA and platform-role claims observed in API configuration.
- Policies: SuperAdmin, MFA/step-up, health check and rate-limit policy families observed in `Program.cs`.
- Plans: owner, clinic, store, shelter and municipality tiers encoded in `SubscriptionTier`.
- Tenancy: no universal Tenant aggregate verified; isolation is implemented selectively through owner, organization/site and `TenantId`/`OrganizationId` boundaries.
- Feature flags: no unified runtime feature-flag service verified; status is `BLOCKED/NO_VERIFICADO`.

See [`NALA_IDENTITY_ACCESS_INVENTORY.md`](NALA_IDENTITY_ACCESS_INVENTORY.md), [`NALA_ROLE_PERMISSION_MATRIX.md`](NALA_ROLE_PERMISSION_MATRIX.md) and [`NALA_PLAN_FEATURE_MATRIX.md`](NALA_PLAN_FEATURE_MATRIX.md).

## Implemented in this initiative

- `PawTrack.AuthorizationTests` added to [`PawTrack.sln`](../../PawTrack.sln), with anonymous, malformed-token, expired-token, wrong-role and public-route checks.
- Local-only authorization factory using isolated EF Core InMemory and a Blob Storage stub.
- Anonymous admin denial test and public-map authorization discovery test.
- `PawTrack.E2ETests` added with a local public-map Playwright smoke test.
- Local server/database guards added to all six SQL seed scripts.
- Clinic organization site dependents are deleted before clinic rows, restoring seed idempotency.
- Testing inventories, matrices, strategy and local database instructions created under this directory.

## Validation evidence

| Check                        | Result     |
| ---------------------------- | ---------- |
| Solution restore             | PASS       |
| Solution build               | PASS       |
| Full backend solution tests  | 1,820 PASS |
| Authorization baseline       | 5 PASS     |
| .NET Playwright smoke        | 1 PASS     |
| Seed chain, pass 1           | PASS       |
| Seed chain, pass 2           | PASS       |
| External production services | NOT USED   |

## Open blockers

1. Complete endpoint-by-endpoint role, claim, policy, permission and public-exception matrix.
2. Dedicated paired-tenant tests for owner, provider, shelter and municipality resources.
3. Expired-plan, entitlement and feature-flag scenarios.
4. Relational SQL Server authorization fixture with disposable lifecycle.
5. Full browser journeys remain in TypeScript Playwright and are not duplicated in .NET.

These are deliberately not marked complete. The source code and executed tests are the authority; documentation or UI presence alone does not close a security or product-quality gap.
