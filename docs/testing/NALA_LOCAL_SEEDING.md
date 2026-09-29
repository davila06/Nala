# NALA Local Seeding

## Approved local targets

- Development LocalDB: `(localdb)\\MSSQLLocalDB`, database `PawTrackDev`, configured in [`appsettings.Development.json`](../../backend/src/PawTrack.API/appsettings.Development.json).
- Testing: use the existing test host and its isolated EF Core InMemory database unless a dedicated disposable SQL Server fixture is explicitly introduced.
- Docker Compose SQL Server/Azurite are local services declared in [`docker-compose.yml`](../../docker-compose.yml); their use must be confirmed before execution.

## Safety gate

Before running a seed, verify `ASPNETCORE_ENVIRONMENT` is `Development`, `Local` or `Testing`, the server is local/loopback, and the database name is an approved test name. Stop if any check fails. Never use the production `appsettings.json` connection string or Key Vault references for seeds.

## Required validation

1. Start only local dependencies.
2. Apply the existing schema/migrations through the documented local workflow.
3. Run the selected seed once.
4. Record row counts and synthetic users.
5. Run it a second time.
6. Confirm no duplicate deterministic rows and no invalid foreign keys.
7. Keep output free of passwords, tokens, personal data and exact GPS details.

The complete six-script chain passed twice against verified local `PawTrackDev` on 2026-09-28. A separate disposable SQL Server run remains useful for relational migration/constraint evidence.
