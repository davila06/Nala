# PawTrack CR — Workspace Instructions

> Full project spec: [`PawTrack_Documento_Maestro_v3.1.md`](../docs/PawTrack_Documento_Maestro_v3.1.md)
> Skills manifest: [`skills.json`](../skills.json)

---

## Project overview

PawTrack CR is a **pet identity + lost-pet recovery platform** for Costa Rica.  
Core loop: _register pet → generate QR → report lost → log sighting → reunite_.

**Current phase:** Product consolidation and enterprise readiness (September 2026).

## AI knowledge and evidence

- For repository/product/architecture questions, start at [`ai/README.md`](../ai/README.md) and follow its links to the canonical document for the requested domain.
- Apply **NO OPINION WITHOUT EVIDENCE**: cite repository paths and distinguish implemented behavior, tests actually run, configuration, proposal, roadmap, and production operation. Mark insufficient evidence `NO_VERIFICADO`.
- Preserve user changes and document ownership. Do not infer deployment, provider approval, legal compliance, commercial approval, AI/RAG/agent availability, or veterinary outcomes from a class, skill, UI, plan, package, or infrastructure declaration alone.
- Keep secrets, personal data, clinical records, and exact GPS coordinates out of prompts, agent memory, examples, and logs. AI must not provide veterinary diagnosis or autonomously perform sensitive actions.

---

## Primary stack

| Layer    | Technology                                                                                 |
| -------- | ------------------------------------------------------------------------------------------ |
| Backend  | .NET 9 · Clean Architecture · CQRS via MediatR                                             |
| Frontend | React PWA · TypeScript; marketing landing · Next.js static export                           |
| Database | Azure SQL · EF Core                                                                        |
| Cloud    | Azure Container Apps · Blob Storage · Notification Hubs · Key Vault · Application Insights |

---

## Architecture

**Pattern:** Modular monolith prepared for future service extraction.  
**Approach:** Clean Architecture + CQRS (no event sourcing in MVP).

### Backend module boundaries (MVP)

| Module          | Responsibility                                      |
| --------------- | --------------------------------------------------- |
| `Auth`          | Registration, email verification, login, JWT        |
| `Pets`          | CRUD, photo upload, QR generation, public profile   |
| `LostPets`      | Loss reports, status state machine                  |
| `Sightings`     | Geo-tagged sightings, anonymous protection, photo   |
| `Notifications` | Push / email / in-app delivery, notification center |

Each module owns its own EF Core entities, commands, queries, and validators — **never reach across module boundaries directly; use MediatR notifications or domain events for cross-module communication**.

### Frontend structure

- `frontend/` is the React PWA product application.
- `landing/frontend/` is an independent Next.js marketing site exported as static files; it links to product flows and must not collect product, clinical or location data itself.
- TypeScript strict mode.
- Co-locate component, styles, and tests.
- State: prefer server state (React Query) over client state; use Zustand only for UI state that truly needs to persist across routes.

---

## Conventions that differ from defaults

- **CQRS split:** Commands mutate state and return minimal data; Queries are read-only and return DTOs. No command should return a full entity.
- **Validation:** FluentValidation in MediatR pipeline behaviors — never validate manually inside handlers.
- **Errors:** Use a `Result<T>` pattern or Problem Details (RFC 7807); do not throw business exceptions across module boundaries.
- **IDs:** Use `Guid` (v7) as primary keys in the domain; expose them as strings in API responses.
- **Migrations:** EF Core code-first. Never edit a migration that has been applied to any shared environment.
- **Photos:** Always route through Azure Blob Storage — never store binaries in the DB.
- **QR:** One QR code per pet, generated on-demand, encodes the public profile URL.
- **Anonymous sightings:** Strip PII from sighting reporter before persisting; store only GPS + timestamp + photo URL.

---

## Build & test commands

```bash
# Backend (from /backend)
dotnet restore
dotnet build
dotnet test

# Product PWA (from /frontend)
npm ci
npm run dev
npm test

# Marketing landing (from /landing/frontend)
npm ci
npm run lint
npm test
npm run build
```

---

## Skills (per-sprint rotation)

Active skills are declared in [`skills.json`](../skills.json).  
Install only the skills for the **current sprint** to avoid context rot:

```bash
# Example — list available skills from a source repo
npx skills add Aaronontheweb/dotnet-skills --list

# Install sprint-specific skills
npx skills add Aaronontheweb/dotnet-skills --skill project-structure --skill csharp-coding-standards
```

See `skills.json → sprints` for the skill set per sprint.

---

## Security reminders

- Secrets go in **Azure Key Vault** — never in `appsettings.json` or committed env files.
- JWT signing keys, connection strings, and storage keys are Key Vault references only.
- Sighting coordinates are public; user home address and contact info are private and require authenticated access + consent grants (v2.0).

---

## What _not_ to do

- Do not add EF Core `DbSet`s to a module's `DbContext` that belong to another module.
- Do not call a module's internal services directly from another module — use MediatR.
- Do not store secrets in `appsettings.json` or `.env` files.
- Do not return raw `Exception` messages in API responses.
