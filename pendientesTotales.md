# PawTrack CR — Pendientes Totales Consolidados

> Versión: 2026-07-30 (fuente única — reemplaza `erroresConsolidado.md`, `PENDIENTES_BETA.md`, `collares.md`, `mejorasUI.md`, `multi-idioma.md`)  
> Ambiente de referencia: **PawnTrackBeta** — Container App + Azure SQL + Static Web App

---

## 1. Matriz de avance

| Categoría                | Items    | ✅ Hecho | 🔄 Parcial | ⛔ Pendiente |
| ------------------------ | -------- | -------- | ---------- | ------------ |
| Infraestructura / DevOps | 6        | 4        | 0          | 2            |
| Seguridad / Canales      | 2        | 1        | 1          | 0            |
| Features backend         | 7        | 5        | 0          | 2            |
| Features frontend        | 9        | 7        | 0          | 2            |
| Internacionalización     | 4        | 4        | 0          | 0            |
| UI/UX sistema de diseño  | 6 fases  | 3 fases  | 1          | 2 fases      |
| Módulo Collar GPS        | 1        | 0        | 0          | 1            |
| Monetización             | 5 líneas | 0        | 1 banner   | 4            |
| **TOTAL**                | **40+**  | **24**   | **2**      | **11+**      |

---

## 2. Infraestructura y DevOps

### ✅ 1.1 backend.yml CI/CD corregido

Commit `aeff566`. Deploy job: `docker build` → `push ACR` → `az containerapp update`.

### ⛔ 1.2 GitHub Secrets para CI/CD

Los 10 secrets requeridos en `GitHub repo → Settings → Secrets and variables → Actions`:

| Secret                            | Valor                                                     |
| --------------------------------- | --------------------------------------------------------- |
| `AZURE_CLIENT_ID`                 | App registration Client ID (Workload Identity Federation) |
| `AZURE_TENANT_ID`                 | Azure AD Tenant ID                                        |
| `AZURE_SUBSCRIPTION_ID`           | Subscription ID                                           |
| `ACR_NAME`                        | `pawtrackacrdev`                                          |
| `CONTAINER_APP_NAME`              | `pawtrack-dev-api`                                        |
| `CONTAINER_APP_FQDN`              | FQDN del Container App (sin `https://`)                   |
| `AZURE_RESOURCE_GROUP`            | `PawnTrackBeta`                                           |
| `SQL_CONNECTION_STRING`           | Connection string completo de Azure SQL                   |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Token del Static Web App                                  |
| `VITE_API_URL`                    | `https://<container-app-fqdn>`                            |

### ✅ 1.3 Infraestructura beta en Azure

P-01 a P-07 completados. Container App corriendo con imagen `v1.0.0-beta`.

### ⛔ 1.4 Dominio personalizado `pawtrack.cr`

1. Comprar dominio (Namecheap/GoDaddy).
2. Crear CNAME → FQDN del Container App.
3. Custom domain en Azure Container App.
4. Actualizar `Cors__AllowedOrigins__0`, `App__BaseUrl` en Key Vault.
5. Actualizar `VITE_API_URL` en GitHub Secrets y re-deployar.

### ✅ 1.5 Push Notifications — VAPID sin proveedor externo

Commit `93fc3cf`. `PushNotificationService` usa RFC 8030 directo.  
**Pendiente Azure:** `Notifications__Push__VapidPublicKey` + `VapidPrivateKey` en Container App.

### ✅ 1.6 Migración `AddUserSoftDelete` + `AddUserLocationTimeZone` + `AddLostPetCurrencyCode`

Aplicadas en DB local. Pendiente en Azure SQL vía CI/CD.

---

## 3. Seguridad y Canales

### ⛔ 3.1 Bot de WhatsApp — configuración Meta Cloud API

Backend 100% implementado. Falta:

1. Crear app en [Meta for Developers](https://developers.facebook.com/) → WhatsApp Business.
2. Obtener `Phone Number ID` + Permanent Token.
3. Registrar webhook: `POST https://<fqdn>/api/whatsapp/webhook`.
4. Agregar en Container App:

```powershell
az containerapp update --name pawtrack-dev-api --resource-group PawnTrackBeta \
  --set-env-vars \
    "WhatsApp__BearerToken=secretref:whatsapp-bearer-token" \
    "WhatsApp__VerifyToken=secretref:whatsapp-verify-token" \
    "WhatsApp__PhoneNumberId=<PHONE_NUMBER_ID>"
```

### 🔄 3.2 Push VAPID en producción

Código listo. Solo falta configurar 2 vars en Azure Container App (ver §1.5).

---

## 4. Features Backend

### ✅ 4.1 Microchip RFID

Commit `aeff566`. `UpdatePetCommand`, validator ISO 11784, `PetDto.MicrochipId`, `PetForm`, `CreatePetPage`, `PetDetailPage`.

### ✅ 4.2 Reactivar mascota (Reunida → Activa)

Commit `d95c530`. `PATCH /api/pets/{id}/reactivate` + botón en `PetDetailPage`.

### ✅ 4.3 ChangePassword + DeleteAccount

Commit `aeff566`. `PATCH /api/auth/me/password` y `DELETE /api/auth/me` (soft-delete).

### ✅ 4.4 Timezone dinámico (§5.3)

Commit `5bf957b`. `UserLocation.TimeZoneId` (IANA), `IsInQuietHours` usa `TimeZoneInfo`, frontend envía `Intl.DateTimeFormat().resolvedOptions().timeZone`.

### ✅ 4.5 CurrencyCode ISO 4217 (§5.4)

Commit `a7d4445`. `LostPetEvent.CurrencyCode`, `RewardBadge` usa `Intl.NumberFormat` dinámico.

### ⛔ 4.6 Collar GPS — módulo completo (nueva feature)

**Arquitectura recomendada (MVP con Tractive):**

**Dominio:**

- `Collar` — `Id`, `PetId`, `DeviceId`, `CollarProvider` (Own/Tractive/Kippy), `ExternalToken` (OAuth cifrado), `BatteryPercent`, `LastSeenAt`, `IsActive`
- `CollarLocation` — write-heavy, insert via `ExecuteSqlRawAsync`, índice `(CollarId, RecordedAt DESC)`, purge automático >30 días

**Application Commands/Queries:**

- `RegisterCollar`, `RecordCollarLocation`, `DeactivateCollar`
- `GetCollarStatus`, `GetLocationHistory` (paginado, últimas N horas)

**Controller:** `api/collars`  
**Background job:** polling Tractive API cada 5 min  
**OAuth2:** conectar cuenta Tractive desde PetDetailPage

**Frontend:**

- Tab "GPS" en `PetDetailPage` con mapa de posición y batería
- Historial de trayectoria últimas 24h

**Esfuerzo estimado:** 3–4 semanas con Tractive. Hardware propio: 3 meses adicionales.

### ⛔ 4.7 Búsqueda de mascota por microchip (GET /api/pets/by-microchip/{chipId})

El repositorio ya tiene `GetByMicrochipIdAsync`. Faltan:

- `GetPetByMicrochipQuery` + Handler
- Endpoint `GET /api/pets/by-microchip/{chipId}` en `PetsController`
- Útil para clínicas con lectores RFID externos
- **Esfuerzo:** 4 horas

---

## 5. Features Frontend

### ✅ 5.1 ProfilePage enterprise

Commit `81cc944`. Rol en español, fortaleza contraseña, member-since, push toggle, foster map picker.

### ✅ 5.2 Dashboard — saludo personalizado + badge perdidas

Commit `6ebee07`. Nombre del usuario, contador de perdidas, quick actions.

### ✅ 5.3 LoginPage enterprise

Commits `2288c82`, `9f1efe4`. Ojo en password, errores específicos del backend, validación inline con debounce, stats reales, redirect preservation, remember email, attrs mobile keyboard, link "Explorar sin cuenta".

### ✅ 5.4 Sistema de diseño Fase 1 — Card component

Commits `9c34a0f`, `8e84b82`. `Card` con `bg-surface`, 19 patrones inline migrados.

### ✅ 5.5 Sistema de diseño Fase 2 — Layout

Commit `26f5de2`. Header dark mode, breadcrumb contextual, nav Admin, fix `/mis-mascotas`.

### ✅ 5.6 Sistema de diseño Fases 3+ — Dark mode

Commits `94c2de9`, `26fea19`. `bg-white`/`bg-sand-50` → `bg-surface`/`bg-surface-warm` en 15+ pantallas.

### ✅ 5.7 /estadísticas solo para Admin

`RoleGuard` + nav condicional + quickaction condicional.

### ⛔ 5.8 Sistema de diseño Fase 4 — Experiencias operativas (Case Room, Search Coordination, Visual Match)

Pendiente aplicar sistema de superficies en módulos avanzados.

### ⛔ 5.9 Sistema de diseño Fase 5 — Módulos por rol (Ally Panel completo, Clinic Dashboard completo, Admin con más data)

Ally Panel y Clinic Dashboard tienen `bg-surface` básico pero necesitan layout por rol más elaborado.

---

## 6. Monetización

### ⛔ 6.1 Freemium (dueños) — Tiers Plus/Familia

| Tier    | Precio     | Feature clave                                         |
| ------- | ---------- | ----------------------------------------------------- |
| Free    | Gratis     | 1 mascota, historial 5 escaneos                       |
| Plus    | ₡2,990/mes | 3 mascotas, predicción IA, alerts SMS, radio 10km     |
| Familia | ₡4,990/mes | Mascotas ilimitadas, multi-usuario, registros médicos |

Lo que ya existe y puede activarse: predicción de movimiento, historial completo, sala coordinación, radio de alertas.

### ⛔ 6.2 Clínicas tiers de pago

Banner de upgrade ya existe en `ClinicDashboardPage`. Falta: integración pasarela de pago (SINPE/Stripe).

| Tier    | Precio      | Feature clave                                          |
| ------- | ----------- | ------------------------------------------------------ |
| Básica  | Gratis      | Directorio, escanear QR/microchip                      |
| Plus    | ₡15,000/mes | Posición destacada, badge, estadísticas                |
| Partner | ₡35,000/mes | Widget embebible, PDF certificado, soporte prioritario |

### ⛔ 6.3 Municipalidades — licencias institucionales

| Paquete      | Precio       | Incluye                                 |
| ------------ | ------------ | --------------------------------------- |
| Básica       | ₡150,000/año | Portal control animal, mapa, capturados |
| Full         | ₡300,000/año | API de consulta, reportes, SLA          |
| Red Regional | ₡500,000/año | Múltiples cantones                      |

82 municipalidades en CR. 5 contratos básicos = ₡750,000/año.

### ⛔ 6.4 Sistema de recompensas con comisión (Bounty)

Flujo técnico: dueño deposita → escrow → aliado reporta avistamiento clave → HandoverCode confirma entrega → pago menos fee (10–15%) se libera vía SINPE/Stripe Connect.  
HandoverCode ya implementado en backend. Falta: escrow, SINPE/Stripe Connect, UI de recompensa activa en mapa.

### ⛔ 6.5 Productos físicos (collares, placas QR)

Botón "Pedir collar" en PetDetailPage → WhatsApp template. Sin cambios de backend. **Esfuerzo: 1 día.**

### ⛔ 6.6 Certificado veterinario PDF firmado digitalmente

Al registrar vacunas/consultas, la clínica genera un PDF/A-1b con firma digital y QR de verificación. Feature del tier Clínica Partner.

---

## 7. Checklist de pre-lanzamiento a producción

- [ ] GitHub Secrets configurados (§1.2)
- [ ] Dominio `pawtrack.cr` + CNAME configurado (§1.4)
- [ ] WhatsApp webhook registrado en Meta (§3.1)
- [ ] VAPID vars en Container App (§3.2)
- [ ] Migraciones aplicadas en Azure SQL vía CI/CD
- [ ] Smoke tests pasando en `https://pawtrack.cr/health`
- [ ] `VITE_API_URL` apuntando al dominio definitivo
- [ ] `Cors__AllowedOrigins__0` = `https://pawtrack.cr`
- [ ] `App__BaseUrl` = `https://pawtrack.cr`

---

## 8. Matriz impacto × esfuerzo — Recomendaciones de implementación

### 🟢 Alta prioridad — hacer esta semana (bajo esfuerzo, alto impacto)

| #   | Tarea                                      | Esfuerzo   | Impacto    | Notas                                                    |
| --- | ------------------------------------------ | ---------- | ---------- | -------------------------------------------------------- |
| 1   | GitHub Secrets (§1.2)                      | 30 min     | 🔴 Crítico | Desbloquea todo CI/CD. Sin código.                       |
| 2   | WhatsApp webhook en Meta (§3.1)            | 2 h config | 🔴 Crítico | Backend listo. Solo config externa.                      |
| 3   | VAPID vars en Container App (§3.2)         | 10 min     | 🟠 Alto    | Código listo. Comando az.                                |
| 4   | Búsqueda por microchip GET endpoint (§4.7) | 4 h        | 🟠 Alto    | Completa el flujo de clínicas con lector RFID.           |
| 5   | Botón "Pedir collar con QR" (§6.5)         | 1 día      | 🟡 Medio   | Link a WhatsApp. Sin backend. Primera conversión física. |

### 🟡 Media prioridad — próximas 2 semanas

| #   | Tarea                                                    | Esfuerzo | Impacto                      |
| --- | -------------------------------------------------------- | -------- | ---------------------------- |
| 6   | Dominio `pawtrack.cr` (§1.4)                             | 1 día    | 🔴 Para go-live público      |
| 7   | UI/UX Fase 4 — Case Room / Coordination dark mode (§5.8) | 2–3 días | 🟡 UX operativa              |
| 8   | UI/UX Fase 5 — Ally Panel / Clinic layout por rol (§5.9) | 3–4 días | 🟡 Percepción de calidad B2B |
| 9   | Clínicas tiers — integrar pasarela SINPE (§6.2)          | 2 sem    | 🔴 Primera línea de ingreso  |

### 🔵 Largo plazo — mes 2+

| #   | Tarea                               | Esfuerzo             | Impacto                             |
| --- | ----------------------------------- | -------------------- | ----------------------------------- |
| 10  | Collar GPS con Tractive (§4.6)      | 3–4 sem              | 🟠 Diferenciador de producto        |
| 11  | Sistema Bounty / recompensas (§6.4) | 4 sem                | 🟠 Viralidad + ingreso por comisión |
| 12  | Freemium dueños Plus/Familia (§6.1) | 3 sem                | 🟠 Ingreso recurrente escalable     |
| 13  | Municipalidades portal (§6.3)       | 4 sem + ciclo ventas | 🔴 Ticket más alto                  |
| 14  | Certificado PDF veterinario (§6.6)  | 2 sem                | 🟡 Feature tier Partner             |

---

## 9. Estado de implementación por sesión

| Commit    | Fecha      | Features                                                                 |
| --------- | ---------- | ------------------------------------------------------------------------ |
| `d95c530` | 2026-07-30 | Reactivar mascota, fixes UI                                              |
| `aeff566` | 2026-07-30 | Microchip, ChangePassword, DeleteAccount, /estadísticas admin, CI/CD fix |
| `93fc3cf` | 2026-07-30 | Push VAPID sin proveedor externo                                         |
| `6ebee07` | 2026-07-30 | Tier 1/2/3: migración, i18n, foster map, dominio, UX dashboard           |
| `81cc944` | 2026-07-30 | ProfilePage 6 mejoras enterprise                                         |
| `9c34a0f` | 2026-07-30 | Card component dark-mode aware (Fase 1 diseño)                           |
| `8e84b82` | 2026-07-30 | Card migration completa — 19 patrones                                    |
| `26f5de2` | 2026-07-30 | Layout Fase 2 — breadcrumb, dark mode nav                                |
| `5bf957b` | 2026-07-30 | Timezone IANA dinámico (§5.3)                                            |
| `a7d4445` | 2026-07-30 | CurrencyCode ISO 4217 (§5.4), fix nav                                    |
| `94c2de9` | 2026-07-30 | Dark mode 7 pantallas críticas                                           |
| `26fea19` | 2026-07-30 | Dark mode #9–12: Dashboard/PetDetail/ReportLost/Chat                     |
| `2288c82` | 2026-07-30 | Login enterprise (ojo, errores, stats reales, aria)                      |
| `9f1efe4` | 2026-07-30 | Login: redirect preservation, remember email, mobile attrs               |
