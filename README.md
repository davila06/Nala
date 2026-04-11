# PawTrack CR

**Pet identity + lost-pet recovery platform for Costa Rica.**

Register your pet → Generate a QR → Report it lost → Log sightings → Coordinate the search → Reunite.

---

## What is this?

PawTrack CR is a full-stack Progressive Web App (PWA) that provides structured, real-time tooling for the entire lifecycle of a lost-pet event — from QR-based identity and sighting reports to AI-powered visual matching, field coordination, and secure physical handover.

For the full product vision, target audience, and how everything works see [NALA.md](./NALA.md).  
For the complete functional and technical specification see [PawTrack_Documento_Maestro_v3.1.md](./PawTrack_Documento_Maestro_v3.1.md).

---

## Feature overview

| Module | What it does |
|--------|-------------|
| **Auth** | Registration, email verification, JWT + refresh tokens, account lockout |
| **Pets** | CRUD, microchip, QR code, photo upload, scan history |
| **Lost Pets** | Loss reports, Case Room, status state machine, reward, contact |
| **Sightings** | Anonymous geo-tagged sighting reports, photo upload |
| **Visual Match** | Azure Computer Vision embeddings — cosine similarity × geo proximity |
| **Found Pets** | QR-less "I found a pet" flow with AI matching |
| **Broadcast** | Multi-channel diffusion (Email, WhatsApp, Telegram, Facebook) |
| **Search Coordination** | 7×7 zone grid, real-time zone claim/clear/release via SignalR |
| **Chat** | Masked chat between owner and finder — no PII exposed |
| **Handover** | 4-digit delivery code for safe physical reunion |
| **Fraud Reports** | Anti-fraud reporting on suspicious interactions |
| **Allies** | Verified rescue orgs — geo-alerts, admin review |
| **Fosters** | Temporary custody volunteers, nearest-foster suggestions |
| **Clinics** | Affiliated vet clinics, microchip scan log |
| **WhatsApp Bot** | Conversational report flow via Meta Cloud API |
| **Notifications** | In-app inbox, web push, per-channel preferences |
| **Incentives** | Reunification leaderboard with badge tiers |
| **Public Stats** | Recovery rates by species, breed, and canton |
| **Public Map** | Live sighting map + movement prediction |

---

## Tech stack

### Backend
- **.NET 9** — ASP.NET Core Web API
- **Clean Architecture** — API / Application / Domain / Infrastructure
- **CQRS** via MediatR 12
- **EF Core 9** — Code-first with SQL Server
- **SignalR** — real-time search coordination hub
- **FluentValidation** — pipeline behavior validators
- **Application Insights** — telemetry and alerting

### Frontend
- **React 19** + **TypeScript 5**
- **Vite 6** + **vite-plugin-pwa** (PWA / service worker)
- **TanStack React Query 5** — server-state management
- **Zustand 5** — UI-only client state
- **React Router 6** — file-based routing conventions
- **Leaflet / React-Leaflet** — interactive maps

### Cloud (Azure)
- **App Service** (Linux, .NET 9)
- **Azure SQL** (SQL Server)
- **Blob Storage** — all photos and binaries
- **Key Vault** — secrets (JWT key, connection strings, API keys)
- **Computer Vision** — 1024-dimensional image embeddings for visual match
- **Application Insights + Log Analytics** — monitoring
- **Notification Hubs** — push notifications

### Local dev
- **Docker Compose** — SQL Server 2025 + Azurite (Blob/Queue/Table emulator)

---

## Project structure

```
PawTrack.sln
├── backend/
│   ├── src/
│   │   ├── PawTrack.API/          # Controllers, middleware, hubs, filters
│   │   ├── PawTrack.Application/  # Commands, queries, validators, handlers
│   │   ├── PawTrack.Domain/       # Entities, value objects, domain events
│   │   └── PawTrack.Infrastructure/ # EF Core, repos, Azure services
│   └── tests/
│       ├── PawTrack.UnitTests/
│       └── PawTrack.IntegrationTests/
├── frontend/
│   └── src/
│       ├── app/           # Router, layout, providers
│       ├── features/      # Feature slices (auth, pets, lost-pets, …)
│       └── shared/        # Shared UI components and utilities
├── infra/
│   ├── main.bicep         # Azure infrastructure declaration
│   └── parameters.prod.bicepparam
├── docs/
│   ├── MANUAL_USUARIO.md  # User manual (Spanish)
│   └── MANUAL_TECNICO.md  # Technical manual
├── NALA.md                # Vision, purpose, and target audience
└── docker-compose.yml     # Local SQL Server + Azurite
```

---

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 9.x |
| Node.js | 20 LTS or 22 LTS |
| Docker Desktop | 4.x or later |
| PowerShell | 7.x (Windows) |

---

## Quick start (local development)

### 1. Clone and prepare secrets

```powershell
git clone <repo-url>
cd Nala

# Create the SQL SA password file (docker-compose secret)
"YourStrongPassword123!" | Out-File secrets/sa_password.txt -NoNewline -Encoding utf8
```

### 2. Configure backend

Copy `backend/src/PawTrack.API/appsettings.Development.json` and fill in:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=PawTrackDev;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=true"
  },
  "Jwt": {
    "Key": "<minimum-32-char-secret>",
    "Issuer": "PawTrack.API",
    "Audience": "PawTrack.Client"
  },
  "Azure": {
    "Storage": { "ConnectionString": "UseDevelopmentStorage=true" }
  }
}
```

### 3. Start everything (recommended)

```powershell
# Starts Docker deps + backend + frontend in parallel
.\start-dev.ps1
```

Options:
```powershell
.\start-dev.ps1 -NoFrontend      # Backend only
.\start-dev.ps1 -NoBackend       # Frontend only
.\start-dev.ps1 -RestartAzurite  # Force a fresh Azurite instance
```

### 4. Manual start (alternative)

```powershell
# Start Docker dependencies
docker compose up -d

# Backend
cd backend
dotnet restore
dotnet ef database update --project src/PawTrack.Infrastructure --startup-project src/PawTrack.API
dotnet run --project src/PawTrack.API

# Frontend (new terminal)
cd frontend
npm install
npm run dev
```

**URLs:**
- Frontend: `http://localhost:5173`
- API: `http://localhost:5000` (or port shown on startup)
- API docs (OpenAPI): `http://localhost:5000/openapi/v1.json`
- Health: `http://localhost:5000/health`

---

## Running tests

```bash
# Backend unit tests
cd backend
dotnet test tests/PawTrack.UnitTests

# Backend integration tests (requires Docker deps running)
dotnet test tests/PawTrack.IntegrationTests

# Frontend tests
cd frontend
npm test
```

---

## Database migrations

```bash
cd backend

# Add a new migration
dotnet ef migrations add <MigrationName> \
  --project src/PawTrack.Infrastructure \
  --startup-project src/PawTrack.API

# Apply pending migrations
dotnet ef database update \
  --project src/PawTrack.Infrastructure \
  --startup-project src/PawTrack.API
```

> **Rule:** Never edit a migration that has been applied to any shared environment.

---

## Architecture principles

- **CQRS:** Commands mutate state and return minimal data. Queries are read-only and return DTOs.
- **Validation:** FluentValidation in MediatR pipeline behaviors — never validate inside handlers.
- **Cross-module communication:** MediatR notifications only — no direct service calls between modules.
- **Errors:** `Result<T>` pattern or RFC 7807 Problem Details — no raw exceptions in responses.
- **IDs:** `Guid` v7 in domain; strings in API responses.
- **Photos:** Always Azure Blob Storage — never binary in the database.
- **Secrets:** Azure Key Vault in production — never in `appsettings.json` or committed env files.

---

## Security model

- JWT HS256 with `ValidAlgorithms` pin (algorithm-confusion attack prevention)
- JTI blocklist for token revocation
- Account lockout: 5 failed attempts → 15-minute lockout
- Rate limiting on all endpoints (IP-keyed sliding window policies)
- bcrypt cost factor 12 for password hashing
- Email verification tokens stored as SHA-256 hex hashes
- Anonymous sightings: PII stripped by `PiiScrubber` before persistence
- WhatsApp bot: phone numbers stored as SHA-256 hashes only
- HMAC-SHA256 signature validation on WhatsApp webhooks
- Masked chat: no personal data exchanged between users
- 4-digit handover code for physical delivery verification
- Kestrel global request body cap: 1 MB (per-endpoint overrides where needed)

---

## Deployment (Azure)

Infrastructure is declared in `infra/main.bicep`. Resources provisioned:

- App Service (Linux, .NET 9)
- Azure SQL Server + Database
- Storage Account (Blob)
- Key Vault
- Application Insights + Log Analytics Workspace
- Azure Monitor alerts

```bash
# Provision infrastructure
az deployment group create \
  --resource-group <rg-name> \
  --template-file infra/main.bicep \
  --parameters infra/parameters.prod.bicepparam

# Publish backend
dotnet publish backend/src/PawTrack.API -c Release -o publish/
az webapp deploy --resource-group <rg> --name <app-name> --src-path publish/

# Build and deploy frontend
cd frontend && npm run build
az staticwebapp deploy --source dist/
```

---

## Documentation

| Document | Location | Audience |
|----------|----------|----------|
| Vision + target audience | [NALA.md](./NALA.md) | Everyone |
| Full product specification | [PawTrack_Documento_Maestro_v3.1.md](./PawTrack_Documento_Maestro_v3.1.md) | Product + Dev |
| User manual | [docs/MANUAL_USUARIO.md](./docs/MANUAL_USUARIO.md) | End users |
| Technical manual | [docs/MANUAL_TECNICO.md](./docs/MANUAL_TECNICO.md) | Developers |

---

## License

Private repository — © 2026 Denis Avila Umaña. All rights reserved.
