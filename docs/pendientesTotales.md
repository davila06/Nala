# PawTrack CR — Pendientes y Estado del Proyecto

> **Última actualización: 2026-08-19**
> Versión anterior: 2026-08-07

---

## Estado: MVP COMPLETO + ENTERPRISE HARDENED

Todos los módulos de código están implementados. Los únicos pendientes son **operacionales** (configuración en Azure), no de código.

---

## 1. Pendientes operacionales (los únicos reales)

| #   | Pendiente                              | Urgencia      | Referencia                             |
| --- | -------------------------------------- | ------------- | -------------------------------------- |
| 1   | **GitHub Secrets CI/CD** (10 secrets)  | 🔴 Crítico    | checklist-lanzamiento.md §Fase 0       |
| 2   | **Dominio pawtrack.cr** + CNAME        | 🔴 Crítico    | checklist-lanzamiento.md §Fase 4       |
| 3   | **WhatsApp webhook en Meta Cloud API** | 🟡 Importante | RUNBOOK_OPERACIONES.md §3.1            |
| 4   | **VAPID keys en Azure Container App**  | 🟡 Importante | RUNBOOK_OPERACIONES.md §3.2            |
| 5   | **Migraciones EF en Azure SQL**        | 🔴 Crítico    | GUIA_DEPLOY_PASO_A_PASO.md §Paso 7     |
| 6   | **Bot:PhoneHashSecret en Key Vault**   | 🟡 Importante | ppsettings.json -> Bot:PhoneHashSecret || 7   | **Registro de bases de datos ante PRODHAB** (Ley 8968) | 🟡 Importante | CUMPLIMIENTO_PROTECCION_DATOS.md §4 |
| 8   | **Confirmar DPA de Microsoft Azure** (transferencia internacional de datos) | 🟢 Deseable | CUMPLIMIENTO_PROTECCION_DATOS.md §4 |
---

## 2. Módulos completados (agosto 2026)

| Módulo                                     | Commit/Sprint | Tests       |
| ------------------------------------------ | ------------- | ----------- |
| Auth completo (JWT, refresh, lockout)      | base          | ✅          |
| Mascotas (CRUD, QR, foto, microchip, BOLA) | base          | ✅          |
| Pérdida + case room + difusión             | base          | ✅          |
| Avistamientos + visual IA                  | base          | ✅          |
| Notificaciones (in-app, push SQL-backed)   | base + aug    | ✅          |
| Chat enmascarado + SignalR real-time       | base + aug    | ✅          |
| Safety (fraud, handover codes)             | base          | ✅          |
| Aliados verificados                        | base          | ✅          |
| Custodios temporales                       | base          | ✅          |
| Clínicas B2B (3 tiers, PDF, API keys)      | base          | ✅          |
| Municipalidades B2G (3 tiers)              | base          | ✅          |
| Expediente médico digital                  | base          | ✅          |
| Collar GPS (Tractive + genérico)           | base          | ✅          |
| Bundle GPS on-demand                       | base          | ✅          |
| Sistema de recompensas (Bounty/SINPE)      | base          | ✅          |
| Familia (multi-usuario, 5 miembros)        | base          | ✅          |
| Suscripciones (Explorador/Plus/Familia)    | base          | ✅          |
| **Tiendas de mascotas (Store B2B)**        | sprint-stores | ✅ 16 tests |
| **Vallas publicitarias (Billboard)**       | 2026-08-19    | —           |
| Broadcast multicanal                       | base          | —           |
| Bot WhatsApp                               | base          | —           |
| Leaderboard + incentivos                   | base          | ✅          |
| Mapa público con stores/clínicas           | base + aug    | ✅          |
| Predicción de movimiento IA                | base          | —           |

---

## 3. Security hardening completado (agosto 2026)

Todas las siguientes vulnerabilidades han sido encontradas y corregidas:

| #                        | Vulnerabilidad                                                                                                                                                                                          | Fix                                                                            |
| ------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------ |
| 1                        | ChangePassword usaba Hash() en vez de Verify() — siempre fallaba                                                                                                                                        | Verify(plaintext, storedHash)                                                  |
| 2                        | decodeRoleFromJwt ignoraba roles Store y Municipality                                                                                                                                                   | Añadidos al switch                                                             |
| 3                        | piClient 401 interceptor sin mutex → múltiples refresh simultáneos                                                                                                                                      |
| efreshPromise compartido |
| 4                        | InMemoryJtiBlocklist no funciona en multi-instancia                                                                                                                                                     | DbJtiBlocklist (SQL)                                                           |
| 5                        | ConfirmBountyDeposit accesible sin autenticación                                                                                                                                                        | [Authorize] añadido                                                            |
| 6                        | Collar GPS history sin verificación de propiedad (BOLA)                                                                                                                                                 | Ownership check en queries                                                     |
| 7                        | RegisterCollar sin verificar propiedad del pet                                                                                                                                                          | Ownership check añadido                                                        |
| 8                        | Leaderboard exponía nombre completo real                                                                                                                                                                | Solo primer nombre (max 20 chars)                                              |
| 9                        | Push subscription sin verificar userId                                                                                                                                                                  | Ownership check antes de delete                                                |
| 10                       | Phone hash SHA-256 vulnerable a rainbow tables                                                                                                                                                          | HMAC-SHA256 con clave secreta                                                  |
| 11                       | SW open redirect via push notification URL                                                                                                                                                              | Validación same-origin                                                         |
| 12                       | Auth endpoints cacheados por ServiceWorker                                                                                                                                                              | Excluidos del NetworkFirst                                                     |
| 13                       | FamilyInvitation sin verificar email del aceptante                                                                                                                                                      | Email match check                                                              |
| 14                       | window.confirm en chat multi-store                                                                                                                                                                      | Modal nativo                                                                   |
| 15                       | AllowedHosts: "\*" en appsettings.json                                                                                                                                                                  | Hostnames específicos                                                          |
| 16                       | Migración `AddClinicApiKeyExpirationAndRotation` usaba `defaultValueSql` con referencia cruzada de columna (SQL Server inválido) — fallaría en cualquier BD nueva al correr migraciones completas       | Reescrita en 3 pasos: `AddColumn` nullable → `UPDATE` → `AlterColumn` NOT NULL |
| 17                       | `MigrationHelper`: `sp_getapplock`/`sp_releaseapplock` en conexiones distintas (el pool cerraba/reseteaba la conexión entre ambas llamadas), causando crash en cada arranque con migraciones pendientes | Una sola conexión abierta durante todo el ciclo acquire→migrate→release        |

---

## 3.1 Bugs reales encontrados via E2E testing (2026-09-02)

Además de la suite de seguridad, la validación E2E contra un stack completo (SQL Server + backend + frontend) del módulo Collar GPS encontró 2 bugs de producción adicionales en el frontend:

| #   | Bug                                                                                                                                                                     | Fix                                                                            |
| --- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------ |
| 1   | `RoleGuard.tsx` no esperaba `isInitializing` — un refresh de página en rutas admin redirigía a `/login` antes de que el refresh silencioso (cookie httpOnly) resolviera | Añadido spinner mientras `isInitializing`, igual que `AuthenticatedLayout.tsx` |
| 2   | `apiClient.ts`: el interceptor de 401 trataba un login fallido igual que una sesión expirada — hacía hard-redirect y borraba el mensaje de error en pantalla            | Excluidas las requests a `/auth/login` del flujo de refresh/redirect           |

## 3.2 Security pass dedicado al módulo Collar GPS (2026-09-02)

| #   | Vulnerabilidad                                                                                                                                                                                                                                                | Fix                                                                                                                                                                                                                                                                                              |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 1   | **BOLA crítico:** `POST /api/collars/pet/{petId}/location` no verificaba ownership — cualquier usuario autenticado podía inyectar una ubicación GPS falsa para el collar de **cualquier** mascota, incluyendo activar/mover falsamente el estado de "perdido" | Verificación explícita: si la request viene autenticada por device key (`X-Collar-Key`), el `CollarId` del claim debe coincidir con el collar del `petId`; si viene autenticada por JWT de usuario, `collar.OwnerId` debe coincidir. Otro caso → 403. Cubierto con 2 tests de integración nuevos |
| 2   | La política de rate limit `location-update` (60/min por IP) estaba definida en `Program.cs` pero **nunca aplicada** a ningún endpoint — ni el endpoint de arriba ni el de ingesta de dispositivos (`POST /api/collars/ingest`) tenían rate limit alguno       | `[EnableRateLimiting("location-update")]` añadido a ambos endpoints                                                                                                                                                                                                                              |
| 3   | `POST /api/collars/tag/{serial}/activate` y `DELETE /api/collars/tag/{serial}/deactivate` no tenían rate limiting — permitía fuerza bruta de seriales sin fricción                                                                                            | `[EnableRateLimiting("collar-serial-check")]` añadido a ambos                                                                                                                                                                                                                                    |

> Revisado y confirmado correcto (sin cambios): hashing de device keys (SHA-256, mismo patrón que otras API keys del sistema), lockout + rate limit de PIN de handover (`RedeemCollarHandoverCode`: máximo de intentos + `handover-verify` 5/min), verificación de serial contra credencial en `IngestCollarLocationCommand`, y autorización por rol (`[Authorize(Roles = "Admin")]`) en `CollarTagAdminController`.

## 4. Tests — estado actual

- **916 tests unitarios** — todos pasando ✅
- **62 tests de integración** — todos pasando ✅
- Suites de seguridad: Rounds 1-51+ con regression tests
- Suites nuevas (agosto 2026): Bounties, Collars, Family, Chat, Stores
