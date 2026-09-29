# NALA Test Architecture Inventory

**Cutoff:** 2026-09-28  
**Status:** `IMPLEMENTED_SIN_PRUEBAS` for inventory; this document does not certify production.

## Current projects

| Project                     | Target           | Framework/pattern                                         | Current role                  |
| --------------------------- | ---------------- | --------------------------------------------------------- | ----------------------------- |
| `PawTrack.UnitTests`        | `net9.0`         | xUnit 2.9.2, FluentAssertions, NSubstitute                | Application/domain/unit tests |
| `PawTrack.IntegrationTests` | `net9.0`         | xUnit, `WebApplicationFactory<Program>`, EF Core InMemory | HTTP/API integration tests    |
| `frontend`                  | React/TypeScript | Vitest, Testing Library, MSW                              | Component/unit tests          |
| `frontend/e2e`              | Browser          | Playwright for Node/TypeScript                            | Existing browser E2E suite    |

The solution currently contains API, Application, Domain, Infrastructure, UnitTests and IntegrationTests in [`PawTrack.sln`](../../PawTrack.sln). `backend/HashGen` exists outside the solution and is treated as a utility, not a product test project.

## Runtime and persistence

- API target: ASP.NET Core .NET 9; composition is in [`Program.cs`](../../backend/src/PawTrack.API/Program.cs).
- Application registers MediatR, FluentValidation and pipeline behaviors in [`ApplicationServiceCollectionExtensions.cs`](../../backend/src/PawTrack.Application/ApplicationServiceCollectionExtensions.cs).
- Production-shaped persistence is SQL Server through [`PawTrackDbContext`](../../backend/src/PawTrack.Infrastructure/Persistence/PawTrackDbContext.cs).
- Existing integration tests replace that context with EF Core InMemory in [`PawTrackWebApplicationFactory`](../../backend/tests/PawTrack.IntegrationTests/Infrastructure/PawTrackWebApplicationFactory.cs).
- Local Development configuration uses `(localdb)\\MSSQLLocalDB`, database `PawTrackDev`, and Azurite for local Blob Storage. See [`appsettings.Development.json`](../../backend/src/PawTrack.API/appsettings.Development.json), [`docker-compose.yml`](../../docker-compose.yml), and [`start-dev.ps1`](../../start-dev.ps1).

## Existing limitations

- `PawTrack.AuthorizationTests` and `PawTrack.E2ETests` now exist in the solution as focused additions from this validation initiative.
- Authorization coverage is distributed through integration tests and feature tests; it is not a complete endpoint-by-endpoint authorization matrix.
- The existing frontend Playwright suite is the established E2E implementation. A second .NET browser suite would duplicate infrastructure unless it is intentionally limited to smoke tests.
- No dedicated relational, disposable authorization database fixture exists. Do not claim SQL Server constraint or transaction behavior from InMemory tests.
- No single feature-flag service was verified in the current backend search. Entitlement and plan gates must be traced per feature.

## Local-only rule

Tests and seed scripts in this initiative must use `Testing`, `Development` or `Local` configuration, reject non-local hosts/database names, use synthetic identities, and never send real external communications. Production availability, Azure connectivity and provider contracts remain `NO_VERIFICADO`.
