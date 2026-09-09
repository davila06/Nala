# Clinic Partner API v1

## Contract

Base URL: `/api/v1`.

Authentication uses `X-PawTrack-Key`. Keys are created only for an active `ClinicPartner` subscription and must carry the minimum scopes required by the integration.

Current machine-to-machine endpoint:

- `GET /api/v1/pets/lookup?chip={chip}`
- `GET /api/v1/pets/lookup?qr={publicQrUrl}`

Required scope: `scan`.

Responses use JSON and RFC 7807-compatible problem details for validation and authorization errors. The public OpenAPI document is exposed at `/openapi/v1.json` while the API remains versioned under `/api/v1`.

## Scopes

- `scan`: QR/RFID lookup and scan operations.
- `medical:read`: read clinical history.
- `medical:write`: create clinical records.
- `medical:export`: create and download a bounded clinical PDF export.
- `certificates`: certificate operations.
- `analytics`: scan and visibility analytics.

Keys expire after one year, can be rotated or revoked, and their raw value is shown only once. Existing keys are backfilled with the export scope by migration and can be narrowed by rotation.

## Clinical exports

`POST /api/clinics/patients/{petId}/medical/export` creates an export request.
The caller must own the clinic, hold an active grant with `export` permission,
and use a key with `medical:export` when authenticating machine-to-machine.

Quotas are enforced server-side: 20 exports per clinic per calendar month and
one export per pet in a rolling 24-hour window. The generated PDF is stored in
the private `clinic-medical-exports` container, expires after 24 hours, and is
downloaded through `GET /api/clinics/medical-exports/{exportId}/download`.
Each export stores clinic, pet, actor, count, timestamps, status and Blob URL
metadata for audit and incident response.

## Certificates

When `Certificates:SigningPrivateKeyBase64` is configured from Key Vault or an
equivalent secret provider, generated PDFs receive an RSA-SHA256 detached
signature sidecar. The certificate stores the signature URL and algorithm. If
the key is absent, the deployment remains unsigned and must not advertise
cryptographic verification.

## Sandbox

A sandbox is defined as a deployment of the same API with:

- `ASPNETCORE_ENVIRONMENT=Sandbox`;
- synthetic clinics, pets and certificates only;
- external notification providers disabled;
- a separate database and Blob container;
- non-production API keys and rate-limit settings.

## Seguridad de API keys

- Las keys se almacenan únicamente como hash y expiran por defecto en 365 días.
- La rotación conserva exactamente los scopes de la key anterior y rechaza keys
  revocadas o expiradas.
- Scopes desconocidos se rechazan al crear una key; no se filtran silenciosamente.
- Una key M2M no puede acceder a gestión de perfil, veterinarios, citas ni API
  keys de la clínica.
- La descarga de un export médico exige `medical:export`, además de la
  autorización de clínica y la expiración del archivo.
- `LastUsedAt` se persiste dentro del ciclo de la solicitud y no mediante tareas
  fuera del scope del `DbContext`.

The repository includes `appsettings.Sandbox.json` with external delivery and
external provider integrations disabled. Deployment must inject a sandbox-only
connection string and storage credentials; the empty connection string is
intentional and prevents accidental use of a production database.

Production credentials must never be accepted by a sandbox deployment. The sandbox is a deployment/configuration boundary, not a shared production flag.

## Compatibility

`/api/v1` is additive-only for the current contract. Breaking changes require `/api/v2`, a migration notice, and a deprecation period documented in the changelog.
