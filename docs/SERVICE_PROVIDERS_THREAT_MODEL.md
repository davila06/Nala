# ServiceProviders Threat Model

## Scope

This model covers provider registration, public directory, booking, manual payment lifecycle, verification documents, incidents, and administrative/support operations.

## Assets

- Provider and customer identity, pet data, contact data, and exact service locations.
- Verification documents and evidence stored in private Blob containers.
- Booking terms, payment references, disputes, and refund decisions.
- Incident descriptions, evidence references, resolutions, and audit records.
- JWT/refresh sessions and administrative/support privileges.

## Trust boundaries

1. Anonymous browser to public directory and provider detail APIs.
2. Authenticated customer to booking/payment APIs.
3. Authenticated provider to owned profile, catalog, availability, bookings, and incidents.
4. Admin/Support to operational incident APIs. Support is intentionally excluded from provider approval, verification approval, payment confirmation, and suspension endpoints. Support assignment is Admin-only and cannot target an existing Admin.
5. API to SQL Server, Blob Storage, notification services, and future payment providers.

## Threats and controls

| Threat                                 | Control                                                                                           | Verification                                       |
| -------------------------------------- | ------------------------------------------------------------------------------------------------- | -------------------------------------------------- |
| Cross-provider object access           | Handler ownership checks and role-scoped controllers                                              | Unit authorization tests and API integration tests |
| Support privilege escalation           | Dedicated `Support` role, Admin-only provisioning, and `/api/support/provider-incidents` boundary | Role and command tests                             |
| Exact home/service location disclosure | Public DTO rounds coordinates and redacts address                                                 | Public directory tests                             |
| Booking capacity race                  | Serializable SQL transaction and bounded queries                                                  | LocalDB concurrency test                           |
| Duplicate payment intent               | Unique booking and idempotency-key indexes                                                        | Payment handler tests and migration validation     |
| Stale unpaid reservation               | Distributed-locked expiration job for requested/payment-pending bookings                          | Expiration job tests                               |
| Malicious verification upload          | MIME allowlist, magic-byte checks, size cap, private container, audited downloads                 | Upload/download tests and DAST                     |
| XSS in provider text                   | Plain-text bounded fields and React escaping; no raw HTML contract                                | Frontend lint and security review                  |
| Replay/webhook fraud                   | External-provider signature/replay controls remain a release gate                                 | Contract tests required before provider activation |
| Token/session theft                    | HttpOnly refresh cookie, rotation, revocation, rate limits                                        | Auth regression suite                              |
| Audit repudiation                      | Immutable audit entries for payment and incident transitions                                      | Handler tests and audit assertions                 |
| Denial of service                      | Rate limits, pagination caps, request size limits, bounded Blob downloads                         | API tests and DAST                                 |

## Residual risks requiring external decisions

- Acquirer webhook signature and replay contract.
- SINPE reconciliation and real refund rails.
- Legal policy, tax treatment, retention periods, and provider responsibility.
- Malware scanning service or approved internal scanning engine.

## Release gates

- Backend build and unit/integration tests pass.
- Frontend app, worker, and node typechecks pass.
- CodeQL and dependency scanning pass.
- DAST baseline has no high-risk findings.
- CodeQL runs for C# and TypeScript/JavaScript on push, pull request, and schedule.
- `.github/workflows/security-dast.yml` runs an OWASP ZAP baseline against the production frontend build.
- Backend and frontend build/typecheck gates run independently of deployment.
- The Support role is provisioned only by an Admin through `PUT /api/admin/support/users/{userId}/role`.
- Support users cannot access Admin provider-management routes.
- Support role cannot access Admin provider approval/payment/verification endpoints.
- Private verification documents are not anonymously downloadable.
