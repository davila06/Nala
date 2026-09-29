# NALA Local Database Setup

## Verified environment

- Server: `(localdb)\\MSSQLLocalDB` (reported local instance containing `LOCALDB`).
- Database: `PawTrackDev`.
- Provider for the running Development API: SQL Server.
- Provider for existing integration/authorization test hosts: EF Core InMemory.
- Local Blob dependency: Azurite when storage behavior is required.

## Safe seed execution

All six SQL seed scripts now reject unapproved database names or non-local SQL Server instance names. The complete chain was run twice with UTF-8 input against `PawTrackDev` and passed both times.

The scripts are synthetic and local-only. No production, Azure database, real email, SMS, WhatsApp, push, payment or external AI service was used in this validation.

## Important limitation

`PawTrackDev` is a developer database, not a disposable CI database. Destructive reseeding should be performed only after confirming the target identity. The authorization suite remains isolated in-memory; SQL Server-specific authorization and migration behavior needs a separate disposable local SQL Server fixture.
