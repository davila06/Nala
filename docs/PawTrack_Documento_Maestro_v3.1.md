# PawTrack CR — Documento Maestro Consolidado (v4.0)

Proyecto: PawTrack CR  
Autor: Denis Avila Umaña  
Versión: 4.0 (agosto 2026 — producción-ready)  
Fecha de actualización: 2026-08-19  
Estado: MVP completo — enterprise hardened — listo para producción

---

## 1. Propósito de este documento

Fuente única de referencia funcional y técnica del proyecto. Reemplaza todos los documentos de planificación y manuales separados.

Ciclo central del producto:

**Mascota registrada → QR generado → mascota perdida → avistamientos → coordinación en campo → reunificación → leaderboard.**

---

## 2. Estado actual (resumen ejecutivo)

PawTrack CR es un **MVP completo enterprise-hardened** con todos los módulos operativos, 916 tests unitarios pasando y seguridad auditada en múltiples rondas.

### Módulos implementados (agosto 2026)

| Módulo                                  | Estado       |
| --------------------------------------- | ------------ |
| Auth (JWT, refresh, lockout, email)     | ✅           |
| Mascotas (CRUD, QR, foto, microchip)    | ✅           |
| Pérdida (case room, difusión, handover) | ✅           |
| Avistamientos + visual IA               | ✅           |
| Notificaciones (in-app, push, WebSub)   | ✅           |
| Chat enmascarado + SignalR real-time    | ✅           |
| Safety (fraud, handover codes)          | ✅           |
| Aliados verificados                     | ✅           |
| Custodios temporales                    | ✅           |
| Clínicas B2B (3 tiers, PDF, API keys)   | ✅           |
| Municipalidades B2G (3 tiers)           | ✅           |
| Expediente médico digital completo      | ✅           |
| Collar GPS (Tractive + genérico + offline/battery alerts, lost mode, safe zones, handover, audit log, admin dashboard) | ✅ |
| Bundle GPS on-demand                    | ✅           |
| Sistema de recompensas (Bounty/SINPE)   | ✅           |
| Familia (multi-usuario, 5 miembros)     | ✅           |
| Suscripciones (Explorador/Plus/Familia) | ✅           |
| **Tiendas de mascotas B2B**             | ✅ **nuevo** |
| **Vallas publicitarias (Billboard)**    | ✅ **nuevo** |
| Broadcast multicanal (WA/TG/FB/Email)   | ✅           |
| Bot WhatsApp conversacional             | ✅           |
| Leaderboard + incentivos                | ✅           |
| Mapa público con stores + clínicas      | ✅           |
| Predicción de movimiento IA             | ✅           |
| Estadísticas públicas de recuperación   | ✅           |

---

## 3. Arquitectura y principios

### 3.1 Arquitectura general

```
[Frontend React PWA] ←→ [ASP.NET Core API] ←→ [Azure SQL]
                              ↑↓ SignalR         ↑↓ EF Core
                         [Azure Blob Storage]
                         [Azure Key Vault]
                         [Application Insights]
```

- **Monolito modular** con separación por capas: API / Application / Domain / Infrastructure.
- **CQRS via MediatR** — Commands mutan; Queries leen y devuelven DTOs.
- **EF Core code-first** con migraciones secuenciales numeradas.
- **SignalR** en `/hubs/search-coordination` (zonas búsqueda) y `/hubs/chat` (mensajes real-time).

### 3.2 Principios activos

| Principio              | Aplicación                                                       |
| ---------------------- | ---------------------------------------------------------------- |
| Validación en pipeline | FluentValidation behaviors — jamás en handlers                   |
| Errores de dominio     | `Result<T>` o excepciones solo dentro del dominio                |
| IDs                    | `Guid v7` como PK; strings en API responses                      |
| Fotos y binarios       | Siempre en Blob Storage — nunca en DB                            |
| Secretos               | Azure Key Vault — cero secretos hardcodeados                     |
| Módulos                | Comunicación solo por MediatR — no llamadas directas             |
| Rate limiting          | Todas las rutas con política explícita                           |
| Seguridad              | ChangePassword usa `Verify()` (bcrypt) nunca `Hash()`            |
| JTI Blocklist          | SQL-backed (`RevokedTokens` table) — funciona en multi-instancia |
| Hashes PII             | HMAC-SHA256 con clave secreta (teléfonos bot)                    |

---

## 4. Stack técnico

### 4.1 Backend

| Tecnología                             | Versión | Uso                                |
| -------------------------------------- | ------- | ---------------------------------- |
| .NET                                   | 9.0     | Runtime principal                  |
| ASP.NET Core                           | 9.0     | Web API                            |
| MediatR                                | 12.x    | CQRS pipeline                      |
| EF Core                                | 9.x     | ORM + migraciones                  |
| FluentValidation                       | 11.x    | Validación en pipeline             |
| SignalR                                | 9.0     | Real-time (chat + search)          |
| ImageSharp                             | 3.x     | Resize de imágenes (JPEG/PNG/WebP) |
| QuestPDF                               | 2025.x  | Certificados PDF veterinarios      |
| xUnit + NSubstitute + FluentAssertions | —       | 916 tests unitarios                |
| Application Insights                   | —       | Telemetría                         |

### 4.2 Frontend

| Tecnología              | Versión    | Uso                          |
| ----------------------- | ---------- | ---------------------------- |
| React                   | 19         | UI                           |
| TypeScript              | 5.x strict | Tipos                        |
| Vite                    | 6          | Build + HMR                  |
| React Router            | 6          | `createBrowserRouter`        |
| TanStack React Query    | 5          | Server state                 |
| Zustand                 | 5          | UI state (cart, auth)        |
| Leaflet / React-Leaflet | —          | Mapa interactivo             |
| Framer Motion           | —          | Animaciones                  |
| Recharts                | —          | Charts médicos/actividad     |
| @microsoft/signalr      | 10.x       | Chat real-time               |
| vite-plugin-pwa         | —          | PWA (registerType: "prompt") |
| Sonner                  | —          | Toasts                       |

### 4.3 Infraestructura

| Recurso              | Tipo        | Descripción        |
| -------------------- | ----------- | ------------------ |
| App Service          | Linux B3    | API .NET 9         |
| Azure SQL            | Standard S2 | Base de datos      |
| Blob Storage         | LRS         | Fotos y PDFs       |
| Key Vault            | Standard    | Secretos           |
| Application Insights | —           | APM + logs         |
| Log Analytics        | —           | KQL queries        |
| Static Web App       | Free        | Frontend React PWA |
| Container Registry   | Basic       | Imágenes Docker    |

---

## 5. Módulos funcionales — detalle de APIs

### 5.1 Auth (`/api/auth`)

| Endpoint           | Método | Auth   | Descripción                                                    |
| ------------------ | ------ | ------ | -------------------------------------------------------------- |
| `/register`        | POST   | —      | Registro; anti-enumeración (siempre 201)                       |
| `/verify-email`    | GET    | —      | Verificación con token SHA-256                                 |
| `/login`           | POST   | —      | JWT + refresh; lockout tras 5 fallos                           |
| `/refresh`         | POST   | Cookie | Rotación de refresh token; theft detection                     |
| `/logout`          | POST   | JWT    | Blocklist JTI en SQL; revoca refresh                           |
| `/forgot-password` | POST   | —      | Anti-enumeración; HMAC email                                   |
| `/reset-password`  | POST   | —      | Token 30min; revoca todas las sesiones                         |
| `/me`              | GET    | JWT    | Perfil actual                                                  |
| `/me`              | PATCH  | JWT    | Actualizar nombre                                              |
| `/me/password`     | PATCH  | JWT    | Cambio contraseña; revoca todas las sesiones; rate limit 5/min |
| `/me`              | DELETE | JWT    | Soft-delete; elimina fotos en Blob                             |

### 5.2 Pets (`/api/pets`)

| Endpoint                | Método | Auth | Descripción                    |
| ----------------------- | ------ | ---- | ------------------------------ |
| `/`                     | POST   | JWT  | Crear mascota; gating por plan |
| `/{id}`                 | GET    | JWT  | Detalle; ownership check       |
| `/{id}`                 | PUT    | JWT  | Actualizar foto + datos        |
| `/{id}`                 | DELETE | JWT  | Soft-delete + borrar blobs     |
| `/{id}/scan-history`    | GET    | JWT  | Historial escaneos; tiered     |
| `/{id}/whatsapp-avatar` | GET    | —    | Avatar optimizado WhatsApp     |
| `/{id}/reactivate`      | POST   | JWT  | Reunida → Activa               |

### 5.3 LostPets (`/api/lost-pets`)

Reporte con GPS, foto, contacto, recompensa, descripción, notas públicas.

### 5.4 Sightings (`/api/sightings`)

Avistamientos anónimos con PiiScrubber, foto, GPS; visual match por IA.

### 5.5 Stores (`/api/stores`, `/api/store-orders`, `/api/admin/stores`)

| Endpoint                                    | Auth  | Descripción                          |
| ------------------------------------------- | ----- | ------------------------------------ |
| `GET /api/public/stores`                    | —     | Directorio activo; paginado          |
| `GET /api/public/stores/{id}`               | —     | Detalle + productos disponibles      |
| `POST /api/stores/register`                 | —     | Registro tienda; anti-enumeración    |
| `GET /api/stores/mine`                      | Store | Mi tienda                            |
| `PUT /api/stores/profile`                   | Store | Actualizar perfil                    |
| `GET /api/stores/products`                  | Store | Mis productos                        |
| `POST /api/stores/products`                 | Store | Agregar producto                     |
| `PUT /api/stores/products/{id}`             | Store | Actualizar producto                  |
| `DELETE /api/stores/products/{id}`          | Store | Eliminar producto                    |
| `POST /api/stores/products/{id}/image`      | Store | Subir imagen 5MB; resize 800px       |
| `POST /api/store-orders`                    | JWT   | Colocar pedido; plan gate StorePlus+ |
| `GET /api/store-orders/mine`                | JWT   | Mis pedidos (paginado)               |
| `PUT /api/store-orders/{id}/report-payment` | JWT   | Reportar pago SINPE                  |
| `GET /api/store-orders/incoming`            | Store | Pedidos entrantes (paginado)         |
| `PUT /api/store-orders/{id}/confirm`        | Store | Confirmar pedido                     |
| `PUT /api/store-orders/{id}/status`         | Store | Avanzar estado (state machine)       |
| `GET /api/admin/stores/pending`             | Admin | Lista pendientes                     |
| `PUT /api/admin/stores/{id}/review`         | Admin | Aprobar/rechazar tienda              |

#### Estado máquina de pedidos

```
PendingPayment → PaymentReported → Confirmed → Preparing →
  (Delivery) → OutForDelivery → Delivered
  (Pickup)   → ReadyForPickup → Delivered
```

Cancelación desde Confirmed, Preparing, ReadyForPickup, OutForDelivery.

### 5.6 Billboards — Vallas publicitarias (`/api/billboards`)

| Endpoint                            | Auth  | Descripción                 |
| ----------------------------------- | ----- | --------------------------- |
| `GET /api/billboards?placement=Map` | —     | Top 5 activas por placement |
| `GET /api/billboards/admin`         | Admin | Lista paginada              |
| `POST /api/billboards`              | Admin | Crear valla                 |
| `PUT /api/billboards/{id}`          | Admin | Actualizar valla            |
| `PATCH /api/billboards/{id}/status` | Admin | Activar/pausar/expirar      |
| `POST /api/billboards/{id}/image`   | Admin | Imagen 5MB; resize 1200px   |

**Placements disponibles:** `Map`, `Dashboard`, `Directory`, `Feed`

**Estado máquina:** `Draft → Active ↔ Paused → Expired`

### 5.7 Chat (`/api/chat`) + SignalR `/hubs/chat`

Chat enmascarado entre dueño y rescatador. PiiScrubber en mensajes. Guard regex teléfonos/emails. SignalR push + poll 10s fallback.

### 5.8 Notifications (`/api/notifications`)

- Inbox paginado con `unreadCount` en respuesta (1 round-trip menos).
- Push web (VAPID); SQL-backed con ownership check.
- JTI blocklist en SQL (distribuido en multi-instancia).

### 5.9 Collars (`/api/collars`)

- **BOLA protegido**: `GET /pet/{id}` y `GET /pet/{id}/history` verifican ownership del pet.
- Integración Tractive OAuth2, genérico HTTP push.
- Activación por serial + device key (`CollarTag`), inventario admin (registrar/marcar vendido/revocar).
- Alertas de conectividad (offline) y batería baja, con preferencias configurables por usuario.
- Modo perdido (Lost Mode), zonas seguras (geofencing), transferencia segura entre dueños (handover codes) y auditoría de eventos.
- Historial de ubicación por rango de fechas y dashboard de métricas para admin.
- Rate limiting en todos los endpoints.

### 5.10 Bounties (`/api/bounties`)

- `POST /api/bounties` — requiere plan Plus.
- `PUT /api/bounties/confirm-deposit` — requiere autenticación (evita acceso anónimo).
- `PUT /api/bounties/{id}/release` — solo el dueño puede liberar.
- `Claim(Guid? sightingId)` — acepta null (no Guid.Empty).

### 5.11 Family (`/api/family`)

- Max 5 miembros + máx 3 invitaciones pendientes simultáneas.
- Verificación de email al aceptar invitación.
- Token de invitación CSPRNG-backed (`RandomNumberGenerator.GetBytes(16)`).

### 5.12 Incentives (`/api/incentives`)

- Leaderboard público: `DisplayName` (solo primer nombre, max 20 chars) — no nombre completo.
- Max 50 entradas por request.

---

## 6. Frontend — rutas y features

### 6.1 Rutas públicas (sin auth)

```
/                     → Landing/Login redirect
/login                → Login
/register             → Registro
/verify-email         → Verificación correo
/forgot-password      → Recuperación
/reset-password       → Reset con token
/p/:id                → Perfil público mascota
/p/:id/report-sighting → Reportar avistamiento
/mapa                 → Mapa interactivo público
/mapa?storeId=X       → Mapa con tienda pre-seleccionada
/estadisticas         → Stats públicas
/encontre-mascota     → Flujo mascota encontrada
/tienda/registro      → Registro tienda
/tiendas              → Directorio tiendas
```

### 6.2 Rutas autenticadas

```
/dashboard            → Mis mascotas + leaderboard + billboard Dashboard
/pets/new             → Nueva mascota
/pets/:id             → Detalle mascota
/lost/:id             → Caso búsqueda
/notifications        → Inbox notificaciones
/perfil               → Mi perfil
/mis-pedidos          → Mis pedidos de tienda (paginado, con progress bar)
/chat/:eventId/:ownerId/:threadId → Chat seguro
/chat/t/:threadId     → Chat directo (desde notificación)
/familia/invitacion/:token → Aceptar invitación familiar
```

### 6.3 Rutas por rol

```
Store + Admin:
  /tienda/pendiente   → Confirmación registro
  /tienda/portal      → Dashboard tienda
  /tienda/portal/productos → CRUD productos + upload imagen
  /tienda/portal/ordenes   → Gestión pedidos en tiempo real

Ally + Admin:
  /allies/panel       → Panel aliado

Clinic + Admin:
  /clinica/portal     → Portal veterinario

Municipality + Admin:
  /municipalidad/portal → Portal municipal

Admin:
  /admin              → Panel admin (tabs: Aliados, Clínicas, Suscripciones,
                         Bundles, Promociones, Tiendas, Vallas 🆕)
  /estadisticas       → Estadísticas avanzadas
```

### 6.4 Features frontend destacadas

- **BillboardBanner**: componente reutilizable, dismissal por sessionStorage, rotación por prioridad, CTA con validación de URL same-origin/HTTPS.
- **CartStore (Zustand)**: multi-store guard, `storeName` limpio al vaciar, max 100 unidades por ítem.
- **ChatHub SignalR**: `useChatSignalR` hook — push inmediato + poll 10s fallback.
- **UpdateBanner**: detecta nuevo SW esperando, muestra "Actualizar / Después", no fuerza reload.
- **Map deep-link**: `?storeId=X` activa capa tiendas y abre el drawer directamente.
- **StoreDetailSheet**: Modal de confirmación en lugar de `window.confirm` para multi-store.
- **useNotifyTyping**: debounce 500ms (antes: 1 request por tecla).

---

## 7. Seguridad — controles implementados

| Control                   | Implementación                                            |
| ------------------------- | --------------------------------------------------------- |
| JWT Algorithm pinning     | `ValidAlgorithms: [HS256]` — rechaza alg=none             |
| JTI Blocklist distribuido | SQL `RevokedTokens` + cleanup nightly                     |
| bcrypt                    | Work factor 12; `Verify()` never `Hash()` for comparison  |
| Token theft detection     | Refresh rotado detecta replay → revoca todas las sesiones |
| Absolute session max      | 90 días desde `SessionIssuedAt`                           |
| Rate limiting             | Todas las rutas con política nombrada                     |
| BOLA Collars              | Ownership check en GetCollarStatus + GetLocationHistory   |
| BOLA Bounty               | `ConfirmDeposit` requiere auth; solo el dueño confirma    |
| BOLA Family               | Email verificado al aceptar invitación                    |
| Push subscription         | userId ownership check al registrar endpoint              |
| Phone hash                | HMAC-SHA256 con `Bot:PhoneHashSecret` (no SHA-256 plain)  |
| Leaderboard PII           | Solo primer nombre (máx 20 chars) en respuesta pública    |
| Image upload              | Magic bytes validation + MIME check                       |
| SW open redirect          | Validación same-origin en notificationclick               |
| Auth endpoints cache      | Excluidos del ServiceWorker NetworkFirst                  |
| AllowedHosts              | `pawtrack.cr;*.pawtrack.cr;localhost` (no `*`)            |
| CSP (SWA)                 | globalHeaders en staticwebapp.config.json                 |
| ChangePassword            | Invalida todas las sesiones activas                       |

---

## 8. Base de datos — migraciones aplicadas (agosto 2026)

Todas las migraciones están en `backend/src/PawTrack.Infrastructure/Persistence/Migrations/`.

Las más recientes (post-sprint-store):

| Migración          | Descripción                                         |
| ------------------ | --------------------------------------------------- |
| `AddPetStores`     | Stores, StoreProducts, StoreOrders, StoreOrderItems |
| `AddRevokedTokens` | JTI blocklist SQL-backed                            |
| `AddBillboards`    | Vallas publicitarias                                |

**Pendiente en Azure:** aplicar en Azure SQL vía CI/CD o `dotnet ef database update`.

---

## 9. Tests

- **916 tests unitarios** (xUnit + NSubstitute + FluentAssertions) — todos pasando.
- **Suites de seguridad**: Rounds 16, 24, 37, 43, 49, 51+ con regression tests dedicados.
- **Tests de nuevas features** (agosto 2026):
  - `Bounties/BountyTests.cs` — 11 domain + 5 handler
  - `Collars/CollarOwnershipTests.cs` — 4 BOLA guards
  - `Family/FamilyTests.cs` — domain + handlers incluyendo pending limit
  - `Chat/SendChatMessageTests.cs` — 8 handler + 5 guard integration
  - `Stores/StoreOrderTests.cs` — 11 domain + 5 handler

---

## 10. Pendientes operacionales (no código)

| #   | Item                               | Bloqueante               |
| --- | ---------------------------------- | ------------------------ |
| 1   | GitHub Secrets CI/CD (10 secrets)  | Deploy automatizado      |
| 2   | Dominio `pawtrack.cr` + CNAME      | Producción pública       |
| 3   | WhatsApp webhook en Meta Cloud API | Bot WhatsApp             |
| 4   | VAPID keys en Azure Container App  | Push notifications       |
| 5   | Migraciones EF en Azure SQL        | Producción funcional     |
| 6   | `Bot:PhoneHashSecret` en Key Vault | Hash seguro de teléfonos |

Ver [`checklist-lanzamiento.md`](checklist-lanzamiento.md) para el procedimiento completo.

Proyecto: PawTrack CR  
Autor: Denis Avila Umaña  
Versión: 3.2 (actualizada agosto 2026)  
Fecha de actualización: 2026-08-07  
Estado: MVP completo — listo para producción

---

## 1. Propósito de este documento

Este archivo es la fuente única de referencia funcional y técnica del proyecto. Reemplaza documentos de planificación y manuales separados.

Objetivo de producto:

Mascota registrada -> QR generado -> mascota perdida -> avistamientos -> coordinación -> reunificación.

---

## 2. Estado actual (resumen ejecutivo)

PawTrack CR ya no está en un MVP básico. El repositorio implementa un **MVP completo** con todos los módulos operativos:

- Autenticación completa con JWT + refresh token, lockout, verificación de email.
- Gestión de mascotas, QR, perfil público y trazabilidad de escaneos.
- Flujo de pérdida con case room, difusión, checklist y coordinación en campo.
- Avistamientos con matching visual por IA y reporte de mascota encontrada sin QR.
- Notificaciones in-app, push web, preferencias y jobs de seguimiento.
- Seguridad operativa: chat enmascarado, códigos de entrega segura y reporte antifraude.
- Módulos de red: aliados verificados, custodios temporales, clínicas afiliadas.
- Incentivos (leaderboard y score de contribución).
- Estadísticas públicas de recuperación.
- Bot de WhatsApp para captura conversacional de reportes.
- **Sistema de suscripciones** (Explorador/Plus/Familia) con feature gating completo.
- **Módulo Familia** (multi-usuario hasta 5 miembros, plan Familia).
- **Expediente médico digital** completo: CRUD de registros, 7 tipos, campos de medicación, peso por visita, recordatorios veterinarios (con notificación push), vista calendario, dashboard multi-mascota, audit log de acceso de clínicas, acceso tiered por plan (Explorador=count, Plus=3 registros preview, Familia=completo).
- **Sistema de recompensas económicas** (Bounty) con escrow SINPE Móvil + HandoverCode.
- **B2B Clínicas** (Básica/Plus/Partner) con expediente compartido, certificados PDF verificables, portal veterinario.
- **B2G Municipalidades** (Básica/Full/Red Regional) con portal de control animal.
- **Collar GPS** integración Tractive (OAuth2 + polling), soporte OEM genérico, activación por tag/serial, alertas de conectividad y batería, modo perdido, zonas seguras (geofencing), transferencia segura (handover codes), auditoría de eventos, historial de ubicación por rango e inventario/dashboard admin.
- **Bundle GPS** on-demand.
- Infraestructura Azure en Bicep (Container Apps, SQL Serverless, Blob, Key Vault, App Insights).
- CI/CD GitHub Actions (build → test → Docker → ACR → Container App update).

---

## 3. Arquitectura y principios

### 3.1 Arquitectura

- Monolito modular con separación por capas:
  - API (`PawTrack.API`)
  - Application (`PawTrack.Application`)
  - Domain (`PawTrack.Domain`)
  - Infrastructure (`PawTrack.Infrastructure`)
- Patrón CQRS con MediatR.
- EF Core code-first con migraciones.

### 3.2 Principios activos

- Validación en pipeline (FluentValidation), no en handlers.
- Commands mutan estado y devuelven datos mínimos.
- Queries leen y devuelven DTOs.
- Comunicación cruzada de módulos por MediatR, no por llamadas directas entre módulos.
- Fotos y binarios en Blob Storage.
- Secretos por Key Vault (sin secretos hardcodeados en repositorio).

---

## 4. Stack técnico actual del código

### 4.1 Backend

- .NET 9 (`net9.0`) en API, Application e Infrastructure.
- ASP.NET Core Web API.
- MediatR 12.x.
- EF Core 9.x + SQL Server.
- JWT Bearer Auth.
- SignalR (`/hubs/search-coordination`).
- Application Insights.
- Health checks (`/health`, `/health/ready`).
- Rate limiting por políticas.

### 4.2 Frontend

- React 19.
- TypeScript 5.x.
- Vite 6.
- React Router (configurado vía `createBrowserRouter`).
- TanStack React Query 5.
- Zustand 5 (estado UI).
- Leaflet / React-Leaflet.
- PWA (`vite-plugin-pwa`).

### 4.3 Infraestructura declarada

- Bicep en `infra/main.bicep` con:
  - App Service Linux (.NET 9)
  - Azure SQL
  - Blob Storage
  - Key Vault
  - Application Insights + Log Analytics
  - Alertas de monitorización

---

## 5. Módulos funcionales implementados

### 5.1 Auth

- Registro, verificación de correo, login, refresh, logout, perfil actual y update de perfil.
- Endpoint base: `api/auth`.

### 5.2 Pets

- CRUD de mascotas.
- Generación de QR.
- Historial de escaneos (`scan-history`).
- Avatar para WhatsApp (`whatsapp-avatar`) y token de avatar.
- Endpoint base: `api/pets`.

### 5.3 LostPets

- Crear reporte de pérdida.
- Consultar por id y por mascota.
- Obtener contacto controlado.
- Caso operativo (`/case`) y cambio de estado (`/status`).
- Endpoint base: `api/lost-pets`.

### 5.4 Sightings y found flow

- Reporte de avistamiento.
- Matching visual directo y por `sightingId`.
- Consulta de avistamientos por mascota.
- Flujo "encontré una mascota" (`api/found-pets`, público y activo).
- Endpoints base: `api/sightings`, `api/found-pets`.

### 5.5 Public API y mapa

- Perfil público de mascota (`api/public/pets/{id}`).
- Mapa público (`api/public/map`).
- Predicción de movimiento (`api/public/movement/{lostPetEventId}`).
- Estadísticas públicas (`api/public/stats/recovery-rates`, `recovery-overview`).

### 5.6 Notifications

- Inbox de notificaciones.
- Marcar leída, marcar todas leídas.
- Preferencias de notificación.
- Push subscription web.
- Endpoint base: `api/notifications`.

### 5.7 Safety, chat y operación segura

- Chat enmascarado (`api/chat`).
- Códigos de handover (`api/lost-pets/{lostPetEventId}/handover`).
- Reporte antifraude (`api/fraud-reports`).

### 5.8 Red colaborativa y operación extendida

- Allies (`api/allies`): aplicación, alertas, revisión admin.
- Fosters (`api/fosters`): perfil, sugerencias, apertura y cierre de custodia.
- Clinics (`api/clinics`): registro, escaneo y revisión admin.
- Broadcast (`api/broadcast`): difusión de casos.
- Search coordination (`api/search-coordination`) + hub SignalR.
- Incentives (`api/incentives`): leaderboard y score propio.
- Locations (`api/me/location`): preferencias/ubicación para alertas.
- WhatsApp bot (`api/whatsapp/webhook`).

---

## 6. Frontend (estado de rutas)

Rutas públicas principales:

- `/login`, `/register`, `/verify-email`
- `/p/:id`, `/p/:id/report-sighting`
- `/map`, `/map/match`, `/estadisticas`
- `/encontre-mascota`, `/encontre-mascota/resultados`
- `/clinica/registro`, `/clinica/pendiente`

Rutas autenticadas principales:

- `/dashboard`, `/perfil`
- `/pets/new`, `/pets/:id`, `/pets/:id/edit`
- `/pets/:id/report-lost`, `/pets/:id/lost-confirmed`
- `/lost/:id/case`, `/lost/:lostEventId/busqueda`
- `/notifications`
- `/chat/:lostPetEventId/:ownerUserId/:threadId?`
- `/allies/panel`, `/clinica/portal`, `/admin`

---

## 7. Datos, migraciones y persistencia

- Estrategia: EF Core code-first.
- Directorio de migraciones activo: `backend/src/PawTrack.Infrastructure/Persistence/Migrations`.
- El historial de migraciones muestra evolución consistente desde `InitialCreate` hasta módulos avanzados (incentivos, embeddings, bot, found pets, foster, clinics, coordinación, push subscriptions y ajustes recientes).

Regla operativa:

- No editar migraciones ya aplicadas en entornos compartidos.

---

## 8. Seguridad y cumplimiento interno

- JWT y autorización por políticas/roles.
- Rate limiting habilitado.
- Middlewares de correlación y manejo global de excepciones.
- Protección de PII en flujos públicos y antifraude.
- Secretos y credenciales orientados a Key Vault en despliegue Azure.

---

## 9. Entorno local y ejecución

### 9.1 Dependencias locales

- SQL Server y Azurite disponibles por `docker-compose.yml`.

### 9.2 Comandos base esperados

Flujo recomendado en Windows (evita bloqueos de DLL y conflictos de Azurite):

```powershell
cd C:\Nala
.\start-dev.ps1
```

Opciones útiles:

```powershell
# Solo backend
.\start-dev.ps1 -NoFrontend

# Solo frontend
.\start-dev.ps1 -NoBackend

# Reiniciar Azurite si quieres forzar nueva instancia
.\start-dev.ps1 -RestartAzurite
```

Backend:

```bash
cd backend
dotnet restore
dotnet build
dotnet test
dotnet run --project src/PawTrack.API
```

Frontend:

```bash
cd frontend
npm install
npm run dev
npm test
```

---

## 10. Testing

- Backend: suite unitaria extensa por módulos (`Auth`, `Pets`, `LostPets`, `Sightings`, `Safety`, `Notifications`, etc.) y proyecto de integración.
- Frontend: pruebas con Vitest + Testing Library.

Meta de calidad continua:

- Mantener cobertura funcional de flujos críticos (reporte, avistamiento, coordinación, reunificación).

---

## 11. Backlog estratégico vigente

Pendientes de producto de alto nivel (no observados como módulos completos en esta versión):

- Integración con perreras/municipalidades (flujo institucional completo).
- Fortalecimiento continuo de analítica predictiva y automatización operativa.

---

## 12. Decisiones de gobierno documental

- Este archivo (`PawTrack_Documento_Maestro_v3.1.md`) queda como documento maestro único del proyecto.
- Cualquier actualización funcional o técnica debe reflejarse aquí primero.
- La documentación auxiliar temporal debe consolidarse y luego eliminarse para evitar divergencia.

---

## 13. Referencias internas del repo

- `skills.json`
- `.github/copilot-instructions.md`
- `backend/src/PawTrack.API/Program.cs`
- `backend/src/PawTrack.API/Controllers/*`
- `frontend/src/app/routes.tsx`
- `infra/main.bicep`
