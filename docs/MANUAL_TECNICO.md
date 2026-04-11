# Manual Técnico — PawTrack CR

**Versión:** 1.0  
**Stack:** .NET 9 · React 19 · Azure  
**Audiencia:** Desarrolladores, arquitectos, equipo DevOps  
**Última actualización:** Abril 2026

---

## Tabla de contenidos

1. [Arquitectura general](#1-arquitectura-general)
2. [Estructura del repositorio](#2-estructura-del-repositorio)
3. [Backend — capas y módulos](#3-backend--capas-y-módulos)
4. [Frontend — estructura y patrones](#4-frontend--estructura-y-patrones)
5. [Base de datos y migraciones](#5-base-de-datos-y-migraciones)
6. [Infraestructura Azure](#6-infraestructura-azure)
7. [Entorno de desarrollo local](#7-entorno-de-desarrollo-local)
8. [Variables de configuración](#8-variables-de-configuración)
9. [Autenticación y autorización](#9-autenticación-y-autorización)
10. [Seguridad operativa](#10-seguridad-operativa)
11. [API REST — referencia de endpoints](#11-api-rest--referencia-de-endpoints)
12. [SignalR — coordinación en tiempo real](#12-signalr--coordinación-en-tiempo-real)
13. [Matching visual por IA](#13-matching-visual-por-ia)
14. [Bot de WhatsApp](#14-bot-de-whatsapp)
15. [Sistema de notificaciones](#15-sistema-de-notificaciones)
16. [Rate limiting y protección DoS](#16-rate-limiting-y-protección-dos)
17. [Testing](#17-testing)
18. [Despliegue en Azure](#18-despliegue-en-azure)
19. [Monitorización y alertas](#19-monitorización-y-alertas)
20. [Decisiones de arquitectura (ADRs)](#20-decisiones-de-arquitectura-adrs)

---

## 1. Arquitectura general

PawTrack CR es un **monolito modular** con separación por capas siguiendo Clean Architecture, preparado para extracción futura de servicios si la carga lo requiere.

```
┌──────────────────────────────────────────────────────────────┐
│                        CLIENTE                               │
│  React 19 PWA  ←──────────────────────── SignalR Hub         │
│  (HTTP/REST)          WebSocket             ↑                │
└────────────────────────────┬─────────────────┼──────────────┘
                             │                 │
                    ┌────────▼─────────────────┼────────┐
                    │       PawTrack.API        │        │
                    │   ASP.NET Core 9          │        │
                    │   Controllers │ Middleware │ Hubs   │
                    └────────────────────────────────────┘
                             │ MediatR
                    ┌────────▼────────────────────────────┐
                    │     PawTrack.Application             │
                    │  Commands │ Queries │ Validators     │
                    │  Handlers │ DTOs │ Interfaces        │
                    └────────────────────────────────────┘
                             │ Interfaces
                    ┌────────▼────────────────────────────┐
                    │      PawTrack.Domain                  │
                    │  Entities │ Value Objects             │
                    │  Domain Events │ Aggregates           │
                    └────────────────────────────────────┘
                             ↑ Implementations
                    ┌────────┴────────────────────────────┐
                    │    PawTrack.Infrastructure            │
                    │  EF Core │ Repositories              │
                    │  Azure Services │ Email │ AI         │
                    └────────────────────────────────────┘
                             │
              ┌──────────────┼──────────────────┐
              ▼              ▼                  ▼
         Azure SQL      Blob Storage      Azure Vision / AI
```

### Principios de la arquitectura

| Principio | Implementación |
|-----------|---------------|
| **CQRS** | Commands mutan estado (devuelven datos mínimos). Queries leen y devuelven DTOs. |
| **Validación en pipeline** | FluentValidation via behavior de MediatR. Nunca en handlers. |
| **Módulos aislados** | Comunicación cruzada solo por MediatR notifications. Sin llamadas directas entre módulos. |
| **Result pattern** | `Result<T>` sin excepciones de negocio cruzando límites de módulo. |
| **IDs** | `Guid` v7 en dominio; `string` en respuestas API. |
| **Binarios** | Siempre en Blob Storage. Jamás en base de datos. |
| **Secretos** | Key Vault en producción. Jamás en archivos de configuración versionados. |

---

## 2. Estructura del repositorio

```
/
├── PawTrack.sln                     # Solution file — todos los proyectos
├── README.md                        # Quickstart y overview
├── NALA.md                          # Visión, propósito y público meta
├── docker-compose.yml               # SQL Server + Azurite para dev local
├── start-dev.ps1                    # Script de arranque local (Windows)
├── secrets/
│   └── sa_password.txt              # (gitignored) contraseña SQL local
├── backend/
│   ├── src/
│   │   ├── PawTrack.API/            # Capa de presentación
│   │   ├── PawTrack.Application/    # Capa de aplicación (CQRS)
│   │   ├── PawTrack.Domain/         # Capa de dominio
│   │   └── PawTrack.Infrastructure/ # Capa de infraestructura
│   └── tests/
│       ├── PawTrack.UnitTests/      # Tests unitarios por módulo
│       └── PawTrack.IntegrationTests/
├── frontend/
│   ├── src/
│   │   ├── app/                     # Router, providers, layouts
│   │   ├── features/                # Feature slices
│   │   └── shared/                  # Componentes y utilidades compartidas
│   └── tests/                       # Vitest + Testing Library
├── infra/
│   ├── main.bicep                   # Infraestructura Azure declarada
│   └── parameters.prod.bicepparam
└── docs/
    ├── MANUAL_USUARIO.md
    └── MANUAL_TECNICO.md
```

---

## 3. Backend — capas y módulos

### 3.1 PawTrack.API

Punto de entrada del sistema. Responsables de:
- Recibir peticiones HTTP y WebSocket
- Ejecutar middleware (correlación, manejo de excepciones, forwarded headers)
- Vincular controllers con MediatR
- Autenticación JWT Bearer
- Rate limiting (politicas por endpoint)
- Health checks `/health` y `/health/ready`
- OpenAPI en `/openapi/v1.json`

**Directorios clave:**

| Directorio | Contenido |
|------------|-----------|
| `Controllers/` | 20 controllers — uno por funcionalidad principal |
| `Hubs/` | `SearchCoordinationHub` (SignalR) |
| `Middleware/` | Correlación de requests, manejo global de excepciones |
| `Filters/` | `ValidateWhatsAppSignatureAttribute` (HMAC-SHA256) |

### 3.2 PawTrack.Application

Lógica de negocio pura. Esta capa no tiene dependencias de infraestructura.

**Estructura por módulo** (ejemplo: `Auth/`):
```
Auth/
├── Commands/
│   ├── RegisterUser/
│   │   ├── RegisterUserCommand.cs        # Record + Handler + Response
│   │   └── RegisterUserCommandValidator.cs
│   ├── LoginUser/
│   └── ...
└── Queries/
    ├── GetCurrentUser/
    └── ...
```

**Módulos implementados:**

| Módulo | Responsabilidad |
|--------|----------------|
| `Auth` | Registro, verificación email, login, refresh, logout, perfil |
| `Pets` | CRUD mascotas, foto, QR, historial escaneos, avatar WhatsApp |
| `LostPets` | Reporte pérdida, case room, estado, coordinación búsqueda |
| `Sightings` | Avistamientos, matcheo visual (VisualMatch/) |
| `Broadcast` | Difusión multi-canal, estado de difusión |
| `Chat` | Mensajes entre dueño y rescatista |
| `Safety` | Handover codes, fraud reports |
| `Notifications` | Inbox, preferencias, push subscriptions |
| `Allies` | Aplicación, alertas, admin review |
| `Fosters` | Perfil, sugerencias, custodia |
| `Clinics` | Registro, escaneo, admin |
| `Incentives` | Leaderboard, mi score |
| `Locations` | Preferencias de ubicación para alertas |
| `Bot` | Sesiones WhatsApp, lógica conversacional |
| `Common` | Interfaces, comportamientos de pipeline, utilidades |

### 3.3 PawTrack.Domain

El corazón del sistema. Sin dependencias externas.

**Entidades principales:**

| Entidad | Descripción |
|---------|-------------|
| `User` | Cuenta de usuario. Bcrypt 12. Lockout. Tokens SHA-256. |
| `RefreshToken` | Token de renovación de sesión JWT. |
| `Pet` | Mascota. Especie, raza, foto, microchip, estado. |
| `QrScanEvent` | Registro de cada escaneo del QR de una mascota. |
| `PetPhotoEmbedding` | Vector de 1024 dimensiones del embedding de la foto. |
| `LostPetEvent` | Reporte de pérdida. Estado, ubicación, contacto, recompensa. |
| `SearchZone` | Zona (300 m) de la cuadrícula de búsqueda. |
| `Sighting` | Avistamiento anónimo. Sin PII del reportante. |
| `FoundPetReport` | Reporte "encontré una mascota sin QR". |
| `ChatMessage` | Mensaje en el chat enmascarado. |
| `HandoverCode` | Código de 4 dígitos para entrega segura. |
| `FraudReport` | Reporte de comportamiento sospechoso. |
| `AllyProfile` | Perfil de organización aliada verificada. |
| `FosterVolunteer` | Voluntario de custodia temporal. |
| `CustodyRecord` | Registro de custodia activa. |
| `ClinicProfile` | Perfil de veterinaria afiliada. |
| `ClinicScanLog` | Registro de escaneo de microchip por clínica. |
| `BotSession` | Sesión conversacional de WhatsApp. |
| `ContributorScore` | Puntaje de reunificaciones del usuario. |
| `BroadcastAttempt` | Registro de intento de difusión por canal. |
| `NotificationItem` | Notificación in-app. |
| `UserLocation` | Preferencia de ubicación y alertas geográficas. |
| `PushSubscription` | Endpoint para notificaciones push web. |

### 3.4 PawTrack.Infrastructure

Implementaciones de interfaces definidas en Application y Domain.

**Servicios externos integrados:**

| Servicio | Clase | Propósito |
|----------|-------|-----------|
| Azure Blob Storage | `BlobStorageService` | Upload/download de fotos |
| Azure Computer Vision 4.0 | `AzureVisionEmbeddingService` | Vectorización de imágenes (1024d) |
| Azure Maps Geocoding | `AzureMapsGeocodingService` | Geocodificación de texto a coordenadas |
| Azure Maps IP Geolocation | `AzureMapsIpGeoLookupService` | Geolocalización de IPs |
| Email (SMTP/SendGrid) | `EmailSender` | Verificación de email, notificaciones |
| Meta Cloud API | (WhatsApp controller) | Envío de mensajes WhatsApp |
| EF Core + SQL Server | `PawTrackDbContext` | Persistencia relacional principal |

**Hosted services:**

| Servicio | Propósito |
|---------|-----------|
| `EmbeddingRefreshHostedService` | Regenera embeddings de fotos de mascotas periódicamente para mantener el índice de matching actualizado |

---

## 4. Frontend — estructura y patrones

### 4.1 Tecnologías

| Librería | Versión | Uso |
|----------|---------|-----|
| React | 19 | Core UI |
| TypeScript | 5.x | Tipado estático strict |
| Vite | 6 | Build tool + dev server |
| vite-plugin-pwa | latest | Service worker + manifest |
| React Router | 6 | Routing con `createBrowserRouter` |
| TanStack React Query | 5 | Server state (fetching, caching, mutations) |
| Zustand | 5 | UI state que persiste entre rutas |
| Leaflet / React-Leaflet | latest | Mapas interactivos |
| Vitest + Testing Library | latest | Testing |

### 4.2 Estructura de features

Cada módulo sigue la misma convención:

```
features/lost-pets/
├── api/          # Funciones de llamada a la API (React Query hooks)
├── components/   # Componentes del módulo
├── hooks/        # Hooks custom del módulo
├── pages/        # Páginas (lazy-loaded desde el router)
└── utils/        # Utilidades locales del módulo
```

### 4.3 Rutas y roles

El router usa layouts anidados con guardas de rol:

```tsx
// Rutas públicas — sin autenticación
/login, /register, /verify-email
/p/:id                    → perfil público de mascota
/p/:id/report-sighting    → reportar avistamiento
/map                      → mapa público
/map/match                → búsqueda visual por foto
/encontre-mascota         → flujo "encontré una mascota"
/clinica/registro         → registro de clínica

// Rutas autenticadas — cualquier usuario logueado
/dashboard
/pets/new, /pets/:id, /pets/:id/edit
/pets/:id/report-lost
/lost/:id/case            → sala de caso
/lost/:lostEventId/busqueda → coordinación de búsqueda
/notifications
/chat/:lostPetEventId/:ownerUserId

// Solo Ally | Admin
/estadisticas
/allies/panel

// Solo Clinic | Admin
/clinica/portal

// Solo Admin
/admin
```

### 4.4 Patrones de datos

- **Server state** (llamadas a la API): `useQuery` y `useMutation` de React Query.
- **UI state** (menú, modales, preferencias de vista): Zustand.
- **Formularios**: React Hook Form o estado local para formularios simples.
- **Mapas**: React-Leaflet con capas de marcadores para avistamientos, mascotas perdidas y aliados.

### 4.5 PWA

- Service worker generado por Vite PWA Plugin.
- Manifiesto incluye iconos, colores y nombre de la app.
- Estrategia de caché: network-first para datos dinámicos, cache-first para assets estáticos.
- El archivo `sw.ts` contiene la lógica del service worker personalizado.

---

## 5. Base de datos y migraciones

### 5.1 Motor y versión

SQL Server 2025 (Docker local) / Azure SQL (producción). EF Core 9 code-first.

### 5.2 DbContext

`PawTrack.Infrastructure.Persistence.PawTrackDbContext`

Todos los módulos comparten un único DbContext en el MVP. Los `DbSet` están organizados por módulo en los archivos de configuración de EF (`Configurations/`).

### 5.3 Historial de migraciones

| Migración | Módulo |
|-----------|--------|
| `InitialCreate` | Auth (Users, RefreshTokens) |
| `AddPets` | Pets (Pets) |
| `AddLostPetEventsAndNotifications` | LostPets, Notifications |
| `AddSightings` | Sightings |
| `AddRecentPhotoToLostPetEvents` | LostPets (photo field) |
| `AddContactToLostPetEvents` | LostPets (contact fields) |
| `AddPublicMessageToLostPetEvents` | LostPets (public message) |
| `AddUserLocations` | Locations |
| `AddBroadcastAttempts` | Broadcast |
| `AddGeofencingAdvanced` | Geofencing (search radius polygon) |
| `AddIncentives` | Incentives (ContributorScore, Badge) |
| `AddPetPhotoEmbeddings` | Sightings AI (embeddings) |
| `AddChatHandoverFraud` | Chat, Safety |
| `AddBotSessions` | Bot |
| `AddRecoveryStatsToLostPetEvents` | LostPets (reunion metrics) |
| `AddQrScanEventsChainOfCustody` | Pets (scan history) |
| `AddFoundPetReports` | Sightings (FoundPetReport) |
| `AddFosterVolunteerNetwork` | Fosters |
| `AddRiskCalendar` | Safety (seasonal risk) |
| `AddSearchCoordination` | LostPets (SearchZone) |
| `AddClinicsModule` | Clinics |
| `AddPushSubscriptions` | Notifications |
| `AddBreedEstimateToFoundPetReport` | Sightings |
| `AddAccountLockout` | Auth |
| `AddRefreshTokenSessionIssuedAt` | Auth |

### 5.4 Comandos de migración

```bash
# Crear migración
dotnet ef migrations add <Nombre> \
  --project backend/src/PawTrack.Infrastructure \
  --startup-project backend/src/PawTrack.API

# Aplicar migraciones
dotnet ef database update \
  --project backend/src/PawTrack.Infrastructure \
  --startup-project backend/src/PawTrack.API

# Revertir última migración
dotnet ef database update <MigraciónAnterior> \
  --project backend/src/PawTrack.Infrastructure \
  --startup-project backend/src/PawTrack.API
```

> **Regla crítica:** Nunca editar una migración ya aplicada en ningún entorno compartido.

### 5.5 Convenciones EF Core

- Todas las PKs son `Guid` v7 (`Guid.CreateVersion7()`).
- `DateTimeOffset` para todos los campos de tiempo (incluye offset de zona horaria).
- Constructores privados en todas las entidades (EF usa reflection).
- Métodos de factory estáticos para creación con invariantes de dominio.
- `AsNoTracking()` en todas las queries de lectura (ReadModel).

---

## 6. Infraestructura Azure

Declarada en `infra/main.bicep` usando Bicep (Azure Resource Manager DSL).

### 6.1 Recursos provisionados

| Recurso | SKU / Tier | Propósito |
|---------|-----------|-----------|
| Log Analytics Workspace | PerGB2018, 30 días retención | Base para App Insights |
| Application Insights | Web | Telemetría y trazas |
| Azure SQL Server | | Motor de base de datos |
| Azure SQL Database | S1 (escalable) | Base de datos PawTrack |
| Storage Account | Standard_LRS | Blob (fotos) |
| Blob Containers | `pet-photos`, `sighting-photos`, `found-pet-photos`, `lost-pet-photos` | Imágenes (público), otras privadas |
| App Service Plan | B2 Linux | Hosting del backend |
| App Service | .NET 9 | API backend |
| Key Vault | Standard | Secretos: JWT, conexiones, API keys |
| Azure Monitor Alert | | Alertas de errores 5xx |

### 6.2 Contenedores de Blob Storage

| Contenedor | Acceso | Uso |
|------------|--------|-----|
| `pet-photos` | Público | Fotos de mascotas en perfil |
| `sighting-photos` | Público | Fotos de avistamientos |
| `found-pet-photos` | Público | Fotos de reportes "encontré mascota" |
| `lost-pet-photos` | Público | Fotos recientes en reporte de pérdida |
| `whatsapp-avatars` | Privado (generado on-demand) | Avatares compuestos |

### 6.3 Key Vault — secretos requeridos

| Secreto | Descripción |
|---------|-------------|
| `Jwt--Key` | Clave de firma JWT (mín. 32 chars, generada con CSPRNG) |
| `ConnectionStrings--DefaultConnection` | String de conexión Azure SQL |
| `Azure--Storage--ConnectionString` | StorageAccount connection string |
| `Azure--Vision--Endpoint` | URL del servicio Computer Vision |
| `Azure--Vision--Key` | Subscription key de Computer Vision |
| `Azure--Maps--SubscriptionKey` | Subscription key de Azure Maps |
| `WhatsApp--AppSecret` | Secret de la app Meta para validar HMAC |
| `WhatsApp--VerifyToken` | Token de verificación del webhook Meta |
| `Email--SmtpPassword` | Contraseña SMTP / API key SendGrid |

---

## 7. Entorno de desarrollo local

### 7.1 Dependencias de entorno

| Herramienta | Versión mínima |
|-------------|---------------|
| .NET SDK | 9.x |
| Node.js | 20 LTS |
| Docker Desktop | 4.x |
| PowerShell | 7.x |
| EF Core CLI | 9.x (`dotnet tool install -g dotnet-ef`) |

### 7.2 Arranque rápido

```powershell
# 1. Preparar secreto de Docker
"TuContraseñaFuerte123!" | Out-File secrets/sa_password.txt -NoNewline -Encoding utf8

# 2. Arrancar todo
.\start-dev.ps1

# Opciones
.\start-dev.ps1 -NoFrontend      # Solo backend
.\start-dev.ps1 -NoBackend       # Solo frontend
.\start-dev.ps1 -RestartAzurite  # Reinicia Azurite
```

### 7.3 Configuración del backend (`appsettings.Development.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=PawTrackDev;User Id=sa;Password=TuContraseñaFuerte123!;TrustServerCertificate=true"
  },
  "Jwt": {
    "Key": "development-key-min-32-chars-ok",
    "Issuer": "PawTrack.API",
    "Audience": "PawTrack.Client"
  },
  "Azure": {
    "Storage": {
      "ConnectionString": "UseDevelopmentStorage=true",
      "BlobServiceUrl": "http://127.0.0.1:10000/devstoreaccount1"
    },
    "Vision": {
      "Endpoint": "",
      "Key": ""
    },
    "Maps": {
      "SubscriptionKey": ""
    }
  },
  "WhatsApp": {
    "AppSecret": "dev-secret",
    "VerifyToken": "dev-verify-token",
    "PhoneNumberId": "",
    "AccessToken": ""
  },
  "Email": {
    "SmtpHost": "localhost",
    "SmtpPort": 1025
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173"]
  }
}
```

> **Nota:** En dev local, Azure Vision y Maps son opcionales. El matching visual devuelve lista vacía si no hay clave configurada.

### 7.4 Docker Compose — servicios locales

```
SQL Server:  localhost:1433  (usuario: sa, contraseña en secrets/sa_password.txt)
Azurite:     localhost:10000 (Blob)
             localhost:10001 (Queue)
             localhost:10002 (Table)
```

---

## 8. Variables de configuración

Todas las variables siguen la convención de ASP.NET Core con doble guión como separador de sección en variables de entorno.

| Variable de entorno | Sección JSON | Descripción |
|--------------------|-------------|-------------|
| `ConnectionStrings__DefaultConnection` | `ConnectionStrings:DefaultConnection` | SQL Server |
| `Jwt__Key` | `Jwt:Key` | JWT signing key |
| `Jwt__Issuer` | `Jwt:Issuer` | JWT issuer |
| `Jwt__Audience` | `Jwt:Audience` | JWT audience |
| `Azure__Storage__ConnectionString` | `Azure:Storage:ConnectionString` | Blob Storage |
| `Azure__Vision__Endpoint` | `Azure:Vision:Endpoint` | Computer Vision |
| `Azure__Vision__Key` | `Azure:Vision:Key` | Computer Vision |
| `Azure__Maps__SubscriptionKey` | `Azure:Maps:SubscriptionKey` | Azure Maps |
| `WhatsApp__AppSecret` | `WhatsApp:AppSecret` | Meta HMAC secret |
| `WhatsApp__VerifyToken` | `WhatsApp:VerifyToken` | Meta webhook verify token |
| `WhatsApp__PhoneNumberId` | `WhatsApp:PhoneNumberId` | Meta phone number ID |
| `WhatsApp__AccessToken` | `WhatsApp:AccessToken` | Meta access token |
| `Email__SmtpHost` | `Email:SmtpHost` | SMTP host |
| `Email__SmtpPort` | `Email:SmtpPort` | SMTP port |
| `Email__SmtpUser` | `Email:SmtpUser` | SMTP user |
| `Email__SmtpPassword` | `Email:SmtpPassword` | SMTP password |
| `Cors__AllowedOrigins__0` | `Cors:AllowedOrigins[0]` | Frontend origin permitido |
| `VisualMatch__BaseUrl` | `VisualMatch:BaseUrl` | URL base para links de perfil en resultados |
| `ApplicationInsights__ConnectionString` | — | Application Insights |

---

## 9. Autenticación y autorización

### 9.1 JWT

- **Algoritmo:** HS256 (`ValidAlgorithms = [HmacSha256]` — algorithm confusion prevention)
- **Expiración:** 15 minutos
- **Claims incluidos:** `sub` (userId), `email`, `role`, `jti` (JWT ID único)
- **ClockSkew:** `TimeSpan.Zero` — sin tolerancia de tiempo

### 9.2 Refresh tokens

- Tokens de renovación almacenados en la base de datos (tabla `RefreshTokens`)
- Expiración configurable (por defecto 7 días)
- Rotación: al renovar, el token viejo se invalida y se emite uno nuevo
- Campo `SessionIssuedAt` para tracking de sesión multidevice

### 9.3 JTI Blocklist

Un middleware `OnTokenValidated` verifica si el `jti` del token está en la tabla de tokens revocados (blocklist). Si el token fue usado para logout, se rechaza aunque todavía sea válido en cuanto a firma y tiempo.

### 9.4 Bloqueo de cuenta

- 5 intentos fallidos de login → bloqueo por 15 minutos
- Estado almacenado en `User.FailedLoginAttempts` y `User.LockoutEnd`
- Reset al primer login exitoso

### 9.5 Roles

| Rol | Valor en DB | Acceso |
|-----|-------------|--------|
| `Owner` | 0 | Dueño de mascotas — rol por defecto |
| `Ally` | 1 | Aliado verificado |
| `Clinic` | 2 | Veterinaria afiliada |
| `Admin` | 3 | Administrador de plataforma |

### 9.6 Políticas de autorización

Los endpoints usan `[Authorize]` con `[Authorize(Roles = "...")]` cuando se requiere un rol específico. El `RoleGuard` en el frontend complementa esto para prevenir acceso a páginas según el rol del token local.

---

## 10. Seguridad operativa

### 10.1 Contraseñas

- Bcrypt con cost factor mínimo 12
- La librería `BCrypt.Net-Next` se usa en `Infrastructure/Auth`

### 10.2 Tokens de verificación de email

- Token de 32 bytes generado con `RandomNumberGenerator.GetBytes(32)` (CSPRNG)
- Codificado en URL-safe Base64
- Solo el **hash SHA-256** se almacena en DB (nunca el token raw)
- Expiración de 24 horas

### 10.3 Privacidad en avistamientos

El `PiiScrubber` (Application layer) procesa las notas de avistamiento antes de persistirlas, eliminando patrones que parezcan email, teléfonos o datos personales.

Los avistamientos **nunca** almacenan datos del reportante. Solo persisten: `lat`, `lng`, `photoUrl`, `note` (sanitizada), `sightedAt`.

### 10.4 Bot de WhatsApp — privacidad

El `BotSession.PhoneNumberHash` es el SHA-256 del número E.164 del usuario. El número real **nunca** se almacena.

### 10.5 Validación HMAC del webhook de WhatsApp

`ValidateWhatsAppSignatureAttribute` lee el body del request (con `Request.EnableBuffering()`) y calcula HMAC-SHA256 con el `AppSecret`. Usa `CryptographicOperations.FixedTimeEquals` para comparación en tiempo constante (previene timing attacks).

### 10.6 Kestrel — límite global de request body

```csharp
serverOptions.Limits.MaxRequestBodySize = 1_048_576; // 1 MB global
```

Los endpoints que reciben fotos tienen override explícito con `[RequestSizeLimit(5_242_880)]` (5 MB).

### 10.7 Forwarded headers

Configurado con `KnownNetworks` restringido a rangos RFC-1918 (Azure VNET) para prevenir IP spoofing via `X-Forwarded-For`. Solo se propaga el primer hop.

### 10.8 Blob Storage — acceso por defecto privado

`BlobStorageService` usa una allowlist (`_knownPublicContainers`) de contenedores que pueden ser públicos. Los contenedores desconocidos son privados por defecto.

---

## 11. API REST — referencia de endpoints

### Base URL

- Local: `http://localhost:5000`
- Producción: `https://<app-service-name>.azurewebsites.net`

### Autenticación

Todos los endpoints protegidos requieren:
```
Authorization: Bearer <jwt-token>
```

### Módulo Auth — `/api/auth`

| Método | Endpoint | Auth | Descripción |
|--------|----------|------|-------------|
| POST | `/register` | No | Crear cuenta |
| POST | `/verify-email` | No | Verificar email con token |
| POST | `/login` | No | Login → JWT + refresh token |
| POST | `/refresh` | No | Renovar JWT con refresh token |
| POST | `/logout` | Sí | Invalidar sesión |
| GET | `/me` | Sí | Perfil del usuario actual |
| PUT | `/me` | Sí | Actualizar perfil |

### Módulo Pets — `/api/pets`

| Método | Endpoint | Auth | Descripción |
|--------|----------|------|-------------|
| GET | `/` | Sí | Mis mascotas |
| POST | `/` | Sí | Crear mascota |
| GET | `/:id` | Sí | Detalle de mascota |
| PUT | `/:id` | Sí | Actualizar mascota |
| DELETE | `/:id` | Sí | Eliminar mascota |
| POST | `/:id/photo` | Sí | Subir foto (multipart, max 5 MB) |
| GET | `/:id/qr` | Sí | Generar imagen QR |
| GET | `/:id/scan-history` | Sí | Historial de escaneos |
| GET | `/:id/whatsapp-avatar` | No | Imagen avatar composición QR + foto |

### Módulo Public — `/api/public`

| Método | Endpoint | Auth | Descripción |
|--------|----------|------|-------------|
| GET | `/pets/:id` | Opcional | Perfil público de mascota (+ registra scan) |
| GET | `/map` | No | Datos del mapa público |
| GET | `/movement/:lostEventId` | No | Predicción de movimiento |

### Módulo Public Stats — `/api/public/stats`

| Método | Endpoint | Auth | Descripción |
|--------|----------|------|-------------|
| GET | `/recovery-rates` | No | Tasas de recuperación (filtros: species, breed, canton) |
| GET | `/recovery-overview` | No | Resumen general de recuperación |

### Módulo Lost Pets — `/api/lost-pets`

| Método | Endpoint | Auth | Descripción |
|--------|----------|------|-------------|
| POST | `/` | Sí | Crear reporte de pérdida |
| GET | `/:id` | Sí | Detalle del reporte |
| GET | `/by-pet/:petId` | Sí | Reporte activo para una mascota |
| GET | `/:id/contact` | Sí | Contacto controlado del dueño |
| GET | `/:id/case` | Sí | Datos de la sala de caso |
| PATCH | `/:id/status` | Sí | Cambiar estado del reporte |
| POST | `/:id/handover/code` | Sí | Generar código de entrega |
| POST | `/:id/handover/verify` | Sí | Verificar código de entrega |

### Módulo Sightings — `/api/sightings`

| Método | Endpoint | Auth | Descripción |
|--------|----------|------|-------------|
| POST | `/` | No | Reportar avistamiento (anónimo) |
| GET | `/by-pet/:petId` | Sí | Avistamientos de una mascota |
| POST | `/visual-match` | No | Matching visual por foto (multipart) |
| GET | `/visual-match/:sightingId` | No | Matching por avistamiento existente |

### Módulo Found Pets — `/api/found-pets`

| Método | Endpoint | Auth | Descripción |
|--------|----------|------|-------------|
| POST | `/` | No | Reportar mascota encontrada sin QR |
| GET | `/:id/matches` | No | Candidatos de matching para reporte |

### Módulo Broadcast — `/api/broadcast`

| Método | Endpoint | Auth | Descripción |
|--------|----------|------|-------------|
| POST | `/lost-pets/:lostEventId` | Sí | Disparar difusión multi-canal |
| GET | `/lost-pets/:lostEventId` | Sí | Estado de la difusión |

### Módulo Search Coordination — `/api/search-coordination`

| Método | Endpoint | Auth | Descripción |
|--------|----------|------|-------------|
| POST | `/:lostEventId/activate` | Sí | Activar cuadrícula de zonas |
| GET | `/:lostEventId/zones` | Sí | Obtener zonas actuales |

_(El claim/clear/release de zonas va por SignalR — ver sección 12)_

### Módulo Chat — `/api/chat`

| Método | Endpoint | Auth | Descripción |
|--------|----------|------|-------------|
| POST | `/:lostEventId/:ownerUserId` | Sí | Enviar mensaje |
| GET | `/:lostEventId/:ownerUserId` | Sí | Historial de mensajes |
| GET | `/:lostEventId/:ownerUserId/threads` | Sí | Hilos de conversación |

### Módulo Fraud Reports — `/api/fraud-reports`

| Método | Endpoint | Auth | Descripción |
|--------|----------|------|-------------|
| POST | `/` | Sí | Crear reporte de fraude |

### Módulo Notifications — `/api/notifications`

| Método | Endpoint | Auth | Descripción |
|--------|----------|------|-------------|
| GET | `/` | Sí | Inbox de notificaciones |
| POST | `/:id/read` | Sí | Marcar como leída |
| POST | `/read-all` | Sí | Marcar todas como leídas |
| GET | `/preferences` | Sí | Obtener preferencias |
| PUT | `/preferences` | Sí | Actualizar preferencias |
| POST | `/push-subscription` | Sí | Registrar endpoint push |

### Módulo Allies — `/api/allies`

| Método | Endpoint | Auth | Descripción |
|--------|----------|------|-------------|
| POST | `/apply` | Sí | Postularse como aliado |
| GET | `/me` | Ally | Ver mi perfil de aliado |
| GET | `/pending` | Admin | Solicitudes pendientes |
| POST | `/:userId/review` | Admin | Aprobar/rechazar aliado |

### Módulo Fosters — `/api/fosters`

| Método | Endpoint | Auth | Descripción |
|--------|----------|------|-------------|
| GET | `/me` | Sí | Mi perfil de custodio |
| PUT | `/me` | Sí | Crear/actualizar perfil de custodio |
| GET | `/suggestions/from-found-report/:id` | Sí | Custodios sugeridos |
| POST | `/custody/start` | Sí | Iniciar custodia |
| POST | `/custody/:id/close` | Sí | Cerrar custodia |

### Módulo Clinics — `/api/clinics`

| Método | Endpoint | Auth | Descripción |
|--------|----------|------|-------------|
| POST | `/register` | No | Registrar clínica |
| GET | `/me` | Clinic | Mi perfil de clínica |
| POST | `/scan` | Clinic | Registrar escaneo de microchip |
| GET | `/pending` | Admin | Clínicas pendientes de aprobación |
| POST | `/:clinicId/review` | Admin | Aprobar/rechazar clínica |

### Módulo Incentives — `/api/incentives`

| Método | Endpoint | Auth | Descripción |
|--------|----------|------|-------------|
| GET | `/leaderboard` | No | Top N contribuidores |
| GET | `/my-score` | Sí | Mi puntaje e insignia |

### Módulo Locations — `/api/me/location`

| Método | Endpoint | Auth | Descripción |
|--------|----------|------|-------------|
| GET | `/` | Sí | Mis preferencias de ubicación |
| PUT | `/` | Sí | Actualizar preferencias de ubicación |

### Módulo WhatsApp — `/api/whatsapp`

| Método | Endpoint | Auth | Descripción |
|--------|----------|------|-------------|
| GET | `/webhook` | No | Handshake de verificación Meta |
| POST | `/webhook` | No (HMAC) | Recibir mensajes entrantes |

---

## 12. SignalR — coordinación en tiempo real

### Hub: `SearchCoordinationHub`

**URL:** `/hubs/search-coordination`  
**Auth:** JWT Bearer (mismo token de la API REST)

El hub gestiona el estado de las zonas de búsqueda en tiempo real. Los cambios disparados via API REST también se notifican a todos los clientes conectados al grupo del evento.

### Métodos del servidor (llamados por el cliente)

| Método | Parámetros | Acción |
|--------|-----------|--------|
| `ClaimZone` | `lostEventId, zoneId` | El voluntario reclama una zona |
| `ClearZone` | `lostEventId, zoneId` | El voluntario marca la zona como revisada |
| `ReleaseZone` | `lostEventId, zoneId` | El voluntario libera la zona |
| `JoinCase` | `lostEventId` | Unirse al grupo del caso (para recibir updates) |
| `LeaveCase` | `lostEventId` | Salir del grupo del caso |

### Eventos del hub (recibidos por el cliente)

| Evento | Payload | Cuándo se dispara |
|--------|---------|------------------|
| `ZoneUpdated` | `{ zoneId, status, claimedByUserId }` | Cuando cualquier zona cambia de estado |
| `SearchActivated` | `{ lostEventId, zones[] }` | Cuando se activa una nueva cuadrícula |

---

## 13. Matching visual por IA

### Flujo técnico

```
Cliente → POST /api/sightings/visual-match (foto multipart + lat/lng)
    ↓
MatchSightingPhotoQueryHandler
    ↓
AzureVisionEmbeddingService.VectorizeStreamAsync()
    → Azure Computer Vision 4.0 API (Image Retrieval)
    → Embedding float[1024]
    ↓
IVisualMatchRepository.GetActiveLostPetProfilesAsync()
    → Todos los perfiles activos con embedding calculado
    ↓
VectorMath.CosineSimilarity() para cada candidato
    ↓
Score combinado: (cosine × 0.70) + (geo_score × 0.30)
    ↓
Filtrado: score ≥ 0.40
Ordenado: mayor score primero
Retornado: top 35 candidatos como VisualMatchDto[]
```

### Embeddings periódicos

`EmbeddingRefreshHostedService` (Hosted Service) regenera los embeddings de mascotas cuyas fotos fueron actualizadas recientemente. Esto mantiene el índice de cosine similarity actualizado sin bloquear los requests del usuario.

### Modelo de embedding

- **Servicio:** Azure Computer Vision 4.0 — Image Retrieval API
- **Dimensiones:** 1024 flotantes (`float[]`)
- **Almacenamiento:** Tabla `PetPhotoEmbeddings` en SQL Server (columna como JSON serializado)
- **Actualización:** Disparada en background cuando `Pet.PhotoUrl` cambia

### Configuración de scoring

```csharp
private const float MinSimilarityThreshold = 0.40f;
private const float CosineWeight           = 0.70f;
private const float GeoWeight              = 0.30f;
private const int   TopK                   = 35;
```

---

## 14. Bot de WhatsApp

### Integración Meta Cloud API

El bot usa la **Meta Cloud API (Business Messaging)** en modo webhook:

1. Meta envía webhooks `POST /api/whatsapp/webhook` por cada mensaje entrante
2. El filtro HMAC valida la firma (`X-Hub-Signature-256` header)
3. El handler `HandleWhatsAppWebhookCommand` procesa el mensaje

### Máquina de estados de la sesión

```
AwaitingPetName
    → (recibe nombre) → AwaitingLastSeen
AwaitingLastSeen
    → (recibe cuándo) → AwaitingLocation
AwaitingLocation
    → (recibe dónde + geocodifica) → AwaitingPhoto
AwaitingPhoto
    → (recibe foto) → Completed
              ↓
    Crea User (guest) + Pet + LostPetEvent
    Envía enlace al perfil público
```

- Sesiones expiran a las 24 horas de creación
- Las sesiones completadas no aceptan más mensajes
- Los mensajes duplicados (re-delivery de Meta) son ignorados via `ProcessedMessageIds`

---

## 15. Sistema de notificaciones

### Tipos de notificación in-app

| Tipo | Origen | Destinatario |
|------|--------|-------------|
| `SightingReceived` | Nuevo avistamiento | Dueño de la mascota |
| `ChatMessage` | Nuevo mensaje de chat | Participante del chat |
| `PetLostNearby` | Mascota perdida en área | Usuarios con alertas geográficas activas |
| `AllyAlert` | Mascota perdida en zona de cobertura | Aliados verificados |
| `ZoneUpdate` | Cambio de zona de búsqueda | Via SignalR (no in-app) |

### Push Notifications (Web Push)

1. Frontend registra un endpoint push via `POST /api/notifications/push-subscription`
2. El endpoint se almacena en tabla `PushSubscriptions`
3. Notificaciones del servidor usan la Web Push Protocol (VAPID)

### Preferencias

Cada usuario puede controlar por canal (in-app / push / email) qué tipos de notificaciones recibe vía `PUT /api/notifications/preferences`.

---

## 16. Rate limiting y protección DoS

Rate limiting implementado con `Microsoft.AspNetCore.RateLimiting` (sliding window por IP).

### Políticas activas

| Política | Límite | Usado en |
|----------|--------|---------|
| `register` | 5 req/10 min por IP | `/api/auth/register`, `/api/clinics/register` |
| `login` | 10 req/min por IP | `/api/auth/login` |
| `public-api` | 30 req/min por IP | La mayoría de endpoints públicos y autenticados |
| `sightings` | 20 req/min por IP | `/api/sightings`, webhook WhatsApp |
| `broadcast` | 3 req/10 min por IP | `/api/broadcast` |

La clave de IP se extrae de `HttpContext.Connection.RemoteIpAddress` post-forwarded-headers, garantizando que se use la IP real del cliente y no la del proxy Azure.

---

## 17. Testing

### Backend — PawTrack.UnitTests

Tests unitarios por módulo, organizados en carpetas paralelas a los módulos de Application/Domain.

**Framework:** xUnit + Moq + FluentAssertions

**Cobertura objetivo:** Flujos de negocio críticos (registration, login, lost report, sighting, visual match, handover).

```bash
dotnet test backend/tests/PawTrack.UnitTests
```

### Backend — PawTrack.IntegrationTests

Tests de integración que levantan la API completa con base de datos real (requiere Docker).

```bash
# Con Docker activo:
dotnet test backend/tests/PawTrack.IntegrationTests
```

### Frontend — Vitest + Testing Library

```bash
cd frontend
npm test        # run once
npm run test:ui # UI de Vitest
```

Tests se ubican en `frontend/tests/` con la misma estructura de `features/`.

---

## 18. Despliegue en Azure

### Prerrequisitos

- Azure CLI autenticado (`az login`)
- Resource group creado
- Secretos de Key Vault pre-poblados

### Infraestructura (Bicep)

```bash
az deployment group create \
  --resource-group pawtrack-prod \
  --template-file infra/main.bicep \
  --parameters infra/parameters.prod.bicepparam \
  --parameters sqlAdminPassword="<strong-password>" \
               alertEmailAddress="devops@pawtrack.cr"
```

### Backend (App Service)

```bash
cd backend
dotnet publish src/PawTrack.API -c Release -o ../publish/api/

az webapp deployment source config-zip \
  --resource-group pawtrack-prod \
  --name pawtrack-prod-api \
  --src ../publish/api.zip
```

### Frontend (Static Web App o Blob + CDN)

```bash
cd frontend
npm run build   # genera dist/

# Subir a Blob + CDN o Azure Static Web Apps
az staticwebapp deploy \
  --app-name pawtrack-prod-frontend \
  --source dist/
```

### Migraciones en producción

Ejecutar antes del despliegue del backend:

```bash
dotnet ef database update \
  --project backend/src/PawTrack.Infrastructure \
  --startup-project backend/src/PawTrack.API \
  --connection "<production-connection-string>"
```

---

## 19. Monitorización y alertas

### Application Insights

Activado via `builder.Services.AddApplicationInsightsTelemetry()` con la connection string de Key Vault.

Telemetría automática:
- Request/response time y status codes
- Dependency calls (SQL, Blob, Computer Vision, Maps)
- Exceptions
- Custom events y métricas

### Alertas configuradas en Bicep

| Alerta | Condición | Acción |
|--------|-----------|--------|
| `High5xxRate` | Tasa de errores 5xx > umbral por 5 min | Email a `alertEmailAddress` |

### Health checks

| Endpoint | Descripción |
|----------|-------------|
| `/health` | Estado básico de la app |
| `/health/ready` | Verifica conectividad a SQL Server y dependencias |

---

## 20. Decisiones de arquitectura (ADRs)

### ADR-001: Monolito modular vs. microservicios

**Decisión:** Monolito modular con separación de módulos via carpetas y MediatR.  
**Razón:** El equipo es pequeño, el MVP necesita velocidad. Los módulos están limpiamente separados para extracción futura si la carga lo justifica.

### ADR-002: EF Core code-first con SqlServer

**Decisión:** Un único DbContext para todos los módulos.  
**Razón:** La complejidad de múltiples DbContexts no se justifica en MVP. Los módulos aún comparten datos referenciales (Users, Pets) frecuentemente.

### ADR-003: JWT HS256 con JTI blocklist

**Decisión:** HS256 simétrico con blocklist en DB para logout y revolución de sesión.  
**Razón:** RS256 requiere infraestructura de par de claves adicional. Para MVP con un solo servicio, HS256 con Key Vault es suficiente y seguro.

### ADR-004: Avistamientos 100% anónimos

**Decisión:** La entidad `Sighting` nunca almacena datos del reportante.  
**Razón:** La privacidad del reportante es condición sine qua non para que la comunidad colabore sin miedo. El riesgo de PII en producción es mayor que la pérdida de trazabilidad del reportante.

### ADR-005: Guid v7 como PK

**Decisión:** `Guid.CreateVersion7()` para todas las PKs.  
**Razón:** Guid v7 es ordenado cronológicamente, lo que mejora la localidad de caché en índices B-tree de SQL Server versus UUID aleatorio (v4). Evita hot pages y mejora la performance de inserción.

### ADR-006: Result[T] sin excepciones de negocio

**Decisión:** Los handlers devuelven `Result<T>`. Sin excepciones de dominio cruzando módulos.  
**Razón:** Las excepciones son costosas como flujo de control. `Result<T>` es explícito, composable y facilita el manejo uniforme de errores en los controllers.

### ADR-007: Fotos en Blob Storage publico — PII en URL

**Decisión:** Las fotos de mascotas y avistamientos se sirven desde URLs públicas de Blob.  
**Razón:** Las fotos de mascotas son intencionalmente públicas (parte del perfil público). Las fotos de sighting son anónimas (no están ligadas a un usuario). No hay PII en las URLs de blob.

---

*PawTrack CR — Manual Técnico · Versión 1.0 · Abril 2026*
