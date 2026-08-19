# PawTrack CR — Pendientes y Estado del Proyecto

> **Última actualización: 2026-08-19**
> Versión anterior: 2026-08-07

---

## Estado: MVP COMPLETO + ENTERPRISE HARDENED

Todos los módulos de código están implementados. Los únicos pendientes son **operacionales** (configuración en Azure), no de código.

---

## 1. Pendientes operacionales (los únicos reales)

| # | Pendiente | Urgencia | Referencia |
|---|-----------|----------|------------|
| 1 | **GitHub Secrets CI/CD** (10 secrets) | 🔴 Crítico | checklist-lanzamiento.md §Fase 0 |
| 2 | **Dominio pawtrack.cr** + CNAME | 🔴 Crítico | checklist-lanzamiento.md §Fase 4 |
| 3 | **WhatsApp webhook en Meta Cloud API** | 🟡 Importante | RUNBOOK_OPERACIONES.md §3.1 |
| 4 | **VAPID keys en Azure Container App** | 🟡 Importante | RUNBOOK_OPERACIONES.md §3.2 |
| 5 | **Migraciones EF en Azure SQL** | 🔴 Crítico | GUIA_DEPLOY_PASO_A_PASO.md §Paso 7 |
| 6 | **Bot:PhoneHashSecret en Key Vault** | 🟡 Importante | ppsettings.json -> Bot:PhoneHashSecret |

---

## 2. Módulos completados (agosto 2026)

| Módulo | Commit/Sprint | Tests |
|--------|--------------|-------|
| Auth completo (JWT, refresh, lockout) | base | ✅ |
| Mascotas (CRUD, QR, foto, microchip, BOLA) | base | ✅ |
| Pérdida + case room + difusión | base | ✅ |
| Avistamientos + visual IA | base | ✅ |
| Notificaciones (in-app, push SQL-backed) | base + aug | ✅ |
| Chat enmascarado + SignalR real-time | base + aug | ✅ |
| Safety (fraud, handover codes) | base | ✅ |
| Aliados verificados | base | ✅ |
| Custodios temporales | base | ✅ |
| Clínicas B2B (3 tiers, PDF, API keys) | base | ✅ |
| Municipalidades B2G (3 tiers) | base | ✅ |
| Expediente médico digital | base | ✅ |
| Collar GPS (Tractive + genérico) | base | ✅ |
| Bundle GPS on-demand | base | ✅ |
| Sistema de recompensas (Bounty/SINPE) | base | ✅ |
| Familia (multi-usuario, 5 miembros) | base | ✅ |
| Suscripciones (Explorador/Plus/Familia) | base | ✅ |
| **Tiendas de mascotas (Store B2B)** | sprint-stores | ✅ 16 tests |
| **Vallas publicitarias (Billboard)** | 2026-08-19 | — |
| Broadcast multicanal | base | — |
| Bot WhatsApp | base | — |
| Leaderboard + incentivos | base | ✅ |
| Mapa público con stores/clínicas | base + aug | ✅ |
| Predicción de movimiento IA | base | — |

---

## 3. Security hardening completado (agosto 2026)

Todas las siguientes vulnerabilidades han sido encontradas y corregidas:

| # | Vulnerabilidad | Fix |
|---|---------------|-----|
| 1 | ChangePassword usaba Hash() en vez de Verify() — siempre fallaba | Verify(plaintext, storedHash) |
| 2 | decodeRoleFromJwt ignoraba roles Store y Municipality | Añadidos al switch |
| 3 | piClient 401 interceptor sin mutex → múltiples refresh simultáneos | efreshPromise compartido |
| 4 | InMemoryJtiBlocklist no funciona en multi-instancia | DbJtiBlocklist (SQL) |
| 5 | ConfirmBountyDeposit accesible sin autenticación | [Authorize] añadido |
| 6 | Collar GPS history sin verificación de propiedad (BOLA) | Ownership check en queries |
| 7 | RegisterCollar sin verificar propiedad del pet | Ownership check añadido |
| 8 | Leaderboard exponía nombre completo real | Solo primer nombre (max 20 chars) |
| 9 | Push subscription sin verificar userId | Ownership check antes de delete |
| 10 | Phone hash SHA-256 vulnerable a rainbow tables | HMAC-SHA256 con clave secreta |
| 11 | SW open redirect via push notification URL | Validación same-origin |
| 12 | Auth endpoints cacheados por ServiceWorker | Excluidos del NetworkFirst |
| 13 | FamilyInvitation sin verificar email del aceptante | Email match check |
| 14 | window.confirm en chat multi-store | Modal nativo |
| 15 | AllowedHosts: "*" en appsettings.json | Hostnames específicos |

---

## 4. Tests — estado actual

- **916 tests unitarios** — todos pasando ✅
- **62 tests de integración** — todos pasando ✅
- Suites de seguridad: Rounds 1-51+ con regression tests
- Suites nuevas (agosto 2026): Bounties, Collars, Family, Chat, Stores
