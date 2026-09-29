# NALA Test Strategy

## Current strategy

- xUnit is the existing backend test framework.
- Unit tests cover application/domain behavior.
- Integration tests use `WebApplicationFactory<Program>` and currently replace SQL Server with isolated EF Core InMemory.
- Frontend uses Vitest for component/unit tests and Playwright TypeScript under `frontend/e2e` for browser E2E.

## Enterprise additions required

1. Keep the dedicated authorization suite aligned with the existing test host patterns and local-only fixture.
2. Keep the existing frontend Playwright suite as the primary browser E2E implementation; the .NET project intentionally provides selected smoke coverage.
3. Add endpoint discovery and public-exception assertions so authorization drift is visible.
4. Add paired-resource tests for owner, clinic organization/site, provider, shelter and municipality boundaries.
5. Add active, expired and insufficient-plan scenarios.
6. Record coverage and failures without converting unverified paths into passes.

## Quality rule

No test may disable authorization, use `Skip` to hide a failure, or call production/external services. A passing test with InMemory does not certify SQL Server constraints, migrations, or production integrations.
