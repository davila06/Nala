# B2B E2E seed contract

The opt-in B2B Playwright tests require:

- `E2E_B2B_ENABLED=1`;
- `clinica_partner@test.cr` with an active ClinicPartner subscription;
- `tienda_activa@test.cr` with an active StorePartner subscription;
- at least one StoreProduct owned by that store;
- the backend, SQL Server and Azurite running at the configured E2E URLs.

The always-on negative authorization test does not require these records. It
must remain runnable against the minimal development seed.
