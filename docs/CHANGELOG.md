# Changelog

## 2026-09-09

### Clinic enterprise

- Added append-only medical record supersession and revision metadata.
- Added expiring clinical grants with separate read, write and export permissions.
- Added formal clinic profile-change submission and administrative approval/rejection workflow.
- Added persisted granular permissions for clinic veterinarians.
- Documented the Clinic Partner `/api/v1` contract, scopes and sandbox deployment boundary.
- Added `appsettings.Sandbox.json` as a fail-closed sandbox configuration.
- Added veterinarian scheduling with overlap protection and owner-managed granular permissions.
- Added optional RSA-SHA256 detached certificate signatures backed by injected private-key configuration.
- Added auditable clinic medical exports with `medical:export`, consent enforcement, monthly/per-pet quotas, private Blob storage, and 24-hour expiry.
- Added outbound webhook subscriptions and delivery with HTTPS validation, protected secrets, HMAC signatures, idempotency headers, distributed locking, timeouts, exponential retry, and terminal failure state.
- Connected accepted Product Analytics events to webhook fanout and added low-cardinality delivery metrics.
- Restored Generic/OEM collar registration and device-key generation while keeping external provider integrations disabled.

Breaking changes require a new API version. SENASA connectivity remains intentionally excluded pending commercial and legal agreement.
