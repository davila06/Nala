# NALA Seed Audit

**Status:** `IMPLEMENTADO_Y_VERIFICADO` for the six listed scripts against local `PawTrackDev`; this does not certify other environments.

## Seed sources found

- [`seed-test-users.sql`](../../backend/scripts/seed-test-users.sql)
- [`seed-extended-test-users.sql`](../../backend/scripts/seed-extended-test-users.sql)
- `seed-lost-pets.sql`
- `seed-admin-data.sql`
- `seed-adoption-demo-data.sql`
- `seed-enterprise-demo-data.sql`
- EF model seed data, including health protocol definitions.

The SQL scripts use deterministic IDs and cleanup statements. A complete chain run passed twice against `(localdb)\\MSSQLLocalDB/PawTrackDev` on 2026-09-28. The run exposed and fixed the missing cleanup of `ClinicOrganizationSiteAccess` and `ClinicOrganizationSites` before clinic deletion. The scripts must still be executed only against the explicitly local database.

## Known gaps

- Seeds do not automatically prove every role, tenant, plan, entitlement, expired subscription, suspended user or cross-tenant denial scenario exists.
- The current model has organization/site and tenant-scoped records, but there is no single seed manifest covering all these boundaries.
- SQL seed scripts contain synthetic test credentials for local use; they must never be copied into production configuration or logs.
- Idempotency was verified on the developer LocalDB. A disposable SQL Server run and complete post-seed relational audit remain separate follow-ups.
