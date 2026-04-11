# PawTrack CR — Documento Maestro Consolidado (v3.1)

Proyecto: PawTrack CR  
Autor: Denis Avila Umaña  
Versión: 3.1 (consolidada y actualizada)  
Fecha de actualización: 2026-04-06  
Estado: Desarrollo activo (MVP ampliado)

---

## 1. Propósito de este documento

Este archivo es la fuente única de referencia funcional y técnica del proyecto. Reemplaza documentos de planificación y manuales separados.

Objetivo de producto:

Mascota registrada -> QR generado -> mascota perdida -> avistamientos -> coordinación -> reunificación.

---

## 2. Estado actual (resumen ejecutivo)

PawTrack CR ya no está en un MVP básico. El repositorio implementa un MVP ampliado con módulos operativos para:

- Autenticación completa con JWT + refresh token.
- Gestión de mascotas, QR, perfil público y trazabilidad de escaneos.
- Flujo de pérdida con case room, difusión, checklist y coordinación en campo.
- Avistamientos con matching visual por IA y reporte de mascota encontrada sin QR.
- Notificaciones in-app, push web, preferencias y jobs de seguimiento.
- Seguridad operativa: chat enmascarado, códigos de entrega segura y reporte antifraude.
- Módulos de red: aliados verificados, custodios temporales, clínicas afiliadas.
- Incentivos (leaderboard y score de contribución).
- Estadísticas públicas de recuperación.
- Bot de WhatsApp para captura conversacional de reportes.

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
