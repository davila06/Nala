# PawTrack CR — Pendientes Totales Consolidados

> Versión: 2026-07-31 (fuente única — reemplaza `erroresConsolidado.md`, `PENDIENTES_BETA.md`, `collares.md`, `mejorasUI.md`, `multi-idioma.md`)  
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
| **Subscription Gating**  | **10**   | **0**    | **0**      | **10**       |
| **Features Familia**     | **5**    | **0**    | **0**      | **5**        |
| **UI Gates**             | **6**    | **0**    | **0**      | **6**        |
| **TOTAL**                | **61+**  | **24**   | **2**      | **32+**      |

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

### ✅ 4.7 Búsqueda de mascota por microchip (GET /api/pets/by-microchip/{chipId})

Commit `c948a96`. `GetPetByMicrochipQuery` + Handler, `GET /api/pets/by-microchip/{chipId}`, validación ISO 11784 hex (10-15 chars).

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

### 🔄 5.8 Sistema de diseño Fase 4 — Experiencias operativas (Case Room, Search Coordination, Visual Match)

`bg-sand-50` → `bg-surface-warm` completado en VisualMatchPage, FoundPetMatchResultPage, ReportSightingPage. SearchCoordinationPage mantiene zinc oscuro intencional (War Room). CaseRoomPage: superficies correctas.

### 🔄 5.9 Sistema de diseño Fase 5 — Módulos por rol (Ally Panel completo, Clinic Dashboard completo, Admin con más data)

ClinicDashboard: banner → modal interactivo (`ClinicTiersModal`). AdminPage: enterprise rewrite con stats header. AllyPanel: KPIs y alerts mejorados. — **en progreso activo**

---

## 6. Monetización

### 🔄 6.1 Freemium (dueños) — Tiers Plus/Familia

`FreemiumModal` implementado (3 tiers: Explorador/Plus/Familia). Banner en Dashboard para usuarios con mascotas. Falta: integración pasarela de pago (SINPE/Stripe).

### 🔄 6.2 Clínicas tiers de pago

`ClinicTiersModal` implementado (commit `5fe8806`): tabla comparativa 3 tiers, modal interactivo desde banner. Falta: integración pasarela de pago (SINPE/Stripe).

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

### ✅ 6.5 Productos físicos (collares, placas QR)

Commit `c948a96`. Botón condicional con `VITE_COLLAR_WHATSAPP_NUMBER` (feature flag). Oculto si no está configurado.

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

---

## 10. Implementación de Planes de Suscripción — Feature Gating Enterprise

> Análisis: ningún handler ni controller verifica `SubscriptionTier` para usuarios. El único check de tier
> existente es `ClinicPartner` en `IssueCertificate`. Todo lo demás está libre para todos.
> Las tareas de esta sección son necesarias para poder **cobrar correctamente** por los planes.
>
> **Convención de estado:** ⛔ Pendiente · 🔄 Parcial · ✅ Hecho

---

### 10.1 Capa de enforcement — Backend

#### ⛔ SUBS-01 · `SubscriptionService` — helper de tier en Application

Crear `ISubscriptionService` con métodos que encapsulan la lógica de tier:

```csharp
// PawTrack.Application/Subscriptions/Services/ISubscriptionService.cs
public interface ISubscriptionService
{
    Task<SubscriptionTier?> GetActiveUserTierAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsAtLeastPlusAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsFamiliaAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetPetLimitAsync(Guid userId, CancellationToken ct = default);      // 1 / 3 / ∞ (-1)
    Task<int> GetScanHistoryLimitAsync(Guid userId, CancellationToken ct = default); // 5 / 50 / 50
    Task<int?> GetMonthlyAiSearchLimitAsync(Guid userId, CancellationToken ct = default); // 3 / null / null
    Task<int> GetAlertRadiusBoostMetresAsync(Guid userId, CancellationToken ct = default); // 0 / +7000 / +∞
}
```

Implementación: `SubscriptionService` inyecta `ISubscriptionRepository`, mapea `SubscriptionStatus.Active` + tier.  
Free = `null` subscription ⇒ tier `Explorador` por default.

**Archivos a crear:**  
`Application/Subscriptions/Services/ISubscriptionService.cs`  
`Infrastructure/Subscriptions/SubscriptionService.cs`  
Registrar en `InfrastructureServiceCollectionExtensions.cs`.

---

#### ⛔ SUBS-02 · Límite de mascotas — `CreatePetCommandHandler`

Inject `ISubscriptionService`. Antes de crear:

```csharp
var petCount = await petRepository.CountByOwnerAsync(request.OwnerId, ct);
var limit    = await subscriptionService.GetPetLimitAsync(request.OwnerId, ct);
if (limit != -1 && petCount >= limit)
    return Result.Failure<Guid>($"Tu plan permite hasta {limit} mascota(s). Actualiza a Plus para registrar más.");
```

Agregar `CountByOwnerAsync(Guid ownerId)` en `IPetRepository` + implementación.

---

#### ⛔ SUBS-03 · Historial de escaneos — `GetPetScanHistoryQuery`

```csharp
var limit  = await subscriptionService.GetScanHistoryLimitAsync(request.RequestingUserId, ct);
var events = await qrScanEventRepository.GetByPetIdAsync(request.PetId, take: limit, ct);
```

`DefaultPageSize = 50` se mantiene para Plus+; free recibe `take: 5`.

---

#### ⛔ SUBS-04 · Contador mensual de búsquedas IA — `MatchSightingPhotoQuery` y `MatchSightingByIdQuery`

1. Agregar tabla `AiSearchUsage` (`UserId`, `YearMonth` int, `Count` int, `UpdatedAt`) con índice único `(UserId, YearMonth)`.
2. Migración EF Core.
3. En los dos handlers de Visual Match, antes de ejecutar la búsqueda:

```csharp
var monthlyLimit = await subscriptionService.GetMonthlyAiSearchLimitAsync(userId, ct);
if (monthlyLimit.HasValue)
{
    var used = await aiSearchUsageRepository.GetCountAsync(userId, currentYearMonth, ct);
    if (used >= monthlyLimit.Value)
        return Result.Failure<...>("Has alcanzado tu límite mensual de 3 búsquedas IA. Activa Plus para búsquedas ilimitadas.");
    await aiSearchUsageRepository.IncrementAsync(userId, currentYearMonth, ct);
}
```

**Archivos a crear/modificar:**  
`Domain/Sightings/AiSearchUsage.cs`  
`Application/Common/Interfaces/IAiSearchUsageRepository.cs`  
`Infrastructure/Sightings/AiSearchUsageRepository.cs`  
Migración `AddAiSearchUsage`.

---

#### ⛔ SUBS-05 · Radio de alerta por tier — `LostPetSearchRadiusCalculator`

Modificar `ILostPetSearchRadiusCalculator.Calculate(...)` para aceptar un multiplicador de tier:

```csharp
// Tier multipliers: Free = ×1.0 | Plus = ×3.3 | Familia = ×99 (sin límite práctico)
int baseRadius = RadiusMatrix[key][bracket];
int effective  = tierMultiplier == -1 ? baseRadius * 33 : (int)(baseRadius * tierMultiplier);
return Math.Min(effective, tierMultiplier == -1 ? int.MaxValue : 100_000);
```

En `ReportLostPetCommandHandler`:

```csharp
var tierMultiplier = await subscriptionService.GetAlertRadiusBoostMetresAsync(ownerId, ct);
var alertRadiusMetres = searchRadiusCalculator.Calculate(species, breed, lastSeenAt, tierMultiplier);
```

El pricing documenta 3 km (free) / 10 km (Plus) como valores aproximados —  
el calculador puede retornar más si la especie/raza es activa; el tier solo escala el resultado base.

---

#### ⛔ SUBS-06 · Gating WhatsApp broadcast — `BroadcastLostPetCommandHandler`

```csharp
var isPlus = await subscriptionService.IsAtLeastPlusAsync(lostEvent.OwnerId, ct);
if (!isPlus)
{
    // Free users get email-only broadcast; skip WhatsApp/Telegram
    channelsToUse = channelsToUse.Where(c => c.Channel == BroadcastChannel.Email).ToList();
}
```

---

#### ⛔ SUBS-07 · Gating Case Room — `GetCaseRoomQuery`

```csharp
var isPlus = await subscriptionService.IsAtLeastPlusAsync(request.RequestingUserId, ct);
if (!isPlus)
    return Result.Failure<CaseRoomDto>("La sala de coordinación requiere el plan Plus.");
```

---

#### ⛔ SUBS-08 · Gating Bounty — `CreateBountyCommand`

```csharp
var isPlus = await subscriptionService.IsAtLeastPlusAsync(request.RequestingUserId, ct);
if (!isPlus)
    return Result.Failure<BountyDto>("El sistema de recompensas requiere el plan Plus.");
```

---

#### ⛔ SUBS-09 · Gating GPS Collar — `RegisterCollarCommand` y `GetCollarStatusQuery`

```csharp
var isPlus = await subscriptionService.IsAtLeastPlusAsync(request.OwnerId, ct);
if (!isPlus)
    return Result.Failure<CollarDto>("El collar GPS requiere el plan Plus.");
```

---

#### ⛔ SUBS-10 · Fix `GetMovementPrediction` — mover de público a Plus

Actualmente en `PublicMapController` accesible sin login.  
Opciones: mover a `LostPetsController` con `[Authorize]` + check Plus, o mantener en público pero sin las coordenadas exactas.

Decisión arquitectónica: **mantener el trail en mapa público** (valor comunitario) pero el **panel de predicción detallado** en `CaseRoomPage` (Plus-only). El endpoint público devuelve solo la dirección general; el de Case Room devuelve el cálculo completo con confianza.

---

### 10.2 Features Familia — Completamente nuevas

#### ⛔ FAM-01 · Módulo Multi-Usuario (cuenta familiar)

**Dominio — entidades nuevas:**

```
FamilyAccount     Id, OwnerId, Name, CreatedAt
FamilyMembership  Id, FamilyAccountId, UserId, Role (Owner|Member), JoinedAt, IsActive
FamilyInvitation  Id, FamilyAccountId, InvitedEmail, Token (GUID), ExpiresAt, AcceptedAt?
```

**Application — Commands/Queries:**

- `CreateFamilyAccountCommand` — solo Familia tier
- `InviteFamilyMemberCommand` — genera token, envía email
- `AcceptFamilyInvitationCommand` — valida token, crea `FamilyMembership`
- `RemoveFamilyMemberCommand`
- `GetFamilyMembersQuery`
- `GetFamilyPetsQuery` — agrega mascotas de todos los miembros

**Controller:** `api/family`

**Frontend — nuevas páginas:**

- `/familia/panel` — lista de miembros con foto + estado
- `/familia/invitar` — enviar invitación por email
- `/familia/aceptar?token=...` — landing de aceptación

**Reglas de negocio:**

- Solo el dueño puede invitar/eliminar miembros
- Máximo 5 miembros (incluyendo el dueño)
- Todos los miembros ven todas las mascotas del grupo familiar
- Solo el dueño puede reportar pérdida / cambiar estado

**Esfuerzo estimado:** 2–3 semanas

---

#### ⛔ FAM-02 · Historial médico — modelo de datos

**Dominio — entidades nuevas:**

```
MedicalRecord  Id, PetId, Type (Vaccination|Deworming|VetVisit|Surgery|Other),
               Date, Description, VetName?, ClinicName?, NextDueDate?,
               DocumentUrl? (Blob), CreatedAt, CreatedBy (UserId)
```

**Application:**

- `AddMedicalRecordCommand` (requiere Familia tier)
- `UpdateMedicalRecordCommand`
- `DeleteMedicalRecordCommand`
- `GetMedicalHistoryQuery` (paginado, filtrado por tipo)

**Controller:** `api/pets/{petId}/medical`

**Frontend:**

- Tab "Salud" en `PetDetailPage` (solo si Familia)
- `MedicalRecordForm` — tipo, fecha, descripción, adjunto PDF/foto
- Lista cronológica con iconos por tipo
- Gateable con `<FamiliaGate>` component

**Esfuerzo estimado:** 1–2 semanas

---

#### ⛔ FAM-03 · Recordatorios veterinarios

**Dominio — entidad nueva:**

```
VetReminder  Id, PetId, OwnerId, Type, DueDate, Title, Notes?,
             IsCompleted, CompletedAt?, ReminderSentAt?, CreatedAt
```

**Background job:** `VetReminderNotificationJob` — corre diario a las 08:00 local,
envía push/email cuando `DueDate` es hoy o en 7 días y `IsCompleted = false`.

**Application:**

- `CreateVetReminderCommand`
- `MarkReminderCompletedCommand`
- `GetUpcomingRemindersQuery`
- Auto-crear reminders desde `MedicalRecord.NextDueDate` al guardar un registro

**Frontend:**

- Widget en Dashboard: "Próximos recordatorios" para usuarios Familia
- Tabla de recordatorios en `PetDetailPage` tab "Salud"
- Notificación push al vencer

**Esfuerzo estimado:** 1 semana (sobre FAM-02)

---

#### ⛔ FAM-04 · Export historial médico PDF (QuestPDF)

QuestPDF ya está instalado y usado en `CertificateGenerator`.

**Application — `ExportMedicalHistoryCommand`:**

- Requiere Familia tier
- Genera PDF con: portada con foto de mascota, tabla de historial médico, tabla de recordatorios pasados, firma con metadata (fecha, generado por PawTrack CR)

**Controlador:** `GET api/pets/{petId}/medical/export`  
**Respuesta:** `application/pdf` con `Content-Disposition: attachment`

**Frontend:**  
Botón "Exportar PDF" en tab "Salud", descarga directamente.

**Esfuerzo estimado:** 3–4 días (sobre FAM-02)

---

#### ⛔ FAM-05 · Push familiar — notificar a todos los miembros

Modificar `NotificationDispatcher.DispatchPetReunitedAsync` (y Lost, Sighting) para,
cuando el dueño pertenece a una cuenta familiar Familia-tier, enviar push a **todos los miembros activos**:

```csharp
if (await subscriptionService.IsFamiliaAsync(ownerId, ct))
{
    var memberIds = await familyRepository.GetActiveMemberIdsAsync(ownerId, ct);
    foreach (var memberId in memberIds)
        await pushNotificationService.SendAsync(memberId, title, body, ct);
}
```

**Esfuerzo estimado:** 2 días (sobre FAM-01)

---

### 10.3 Frontend — Componentes de gating UI

#### ⛔ UI-GATE-01 · Hook `useMyTier()`

```ts
// frontend/src/features/pets/hooks/useSubscription.ts
export function useMyTier() {
  const { data: sub } = useMySubscription();
  const tier = sub?.status === "Active" ? sub.tier : "Explorador";
  return {
    tier,
    isPlus: tier === "UserPlus" || tier === "UserFamilia",
    isFamilia: tier === "UserFamilia",
  };
}
```

---

#### ⛔ UI-GATE-02 · Componente `<PlanGate>`

```tsx
// frontend/src/features/pets/components/PlanGate.tsx
interface PlanGateProps {
  requires: "Plus" | "Familia";
  children: ReactNode;
  fallback?: ReactNode; // default: upgrade banner
}
```

Muestra `children` si el tier es suficiente, `fallback` (o un `<UpgradeBanner>`) si no.

---

#### ⛔ UI-GATE-03 · `<UpgradeBanner>` contextual

Componente que muestra el tier requerido con CTA → `FreemiumModal`:

```
🔒 Esta función requiere Plan Plus (₡2,990/mes)
   [Conocer Plus →]
```

---

#### ⛔ UI-GATE-04 · Gating en `PetDetailPage`

Envolver con `<PlanGate>`:

- Tab "GPS" → `requires="Plus"`
- Botón "Historial completo" en escaneos → `requires="Plus"`
- Tab "Salud" → `requires="Familia"`
- Botón "Sala de coordinación" en reporte activo → `requires="Plus"`

---

#### ⛔ UI-GATE-05 · Mostrar límite de mascotas en `Dashboard`

Cuando el usuario tiene 1 mascota (free), mostrar:

```
🐾 1 / 1 mascotas — Agrega hasta 3 con Plus
```

CTA abre `FreemiumModal`.

---

#### ⛔ UI-GATE-06 · Contador AI/mes en `VisualMatchPage`

Para usuarios free, mostrar debajo del botón:

```
Búsquedas IA: 2 / 3 este mes · Plus = ilimitado
```

---

### 10.4 Orden de implementación recomendado

| Prioridad | Tarea                                            | Bloquea a                         | Esfuerzo |
| --------- | ------------------------------------------------ | --------------------------------- | -------- |
| 🔴 1      | SUBS-01 `SubscriptionService`                    | Todo lo demás                     | 1 día    |
| 🔴 2      | SUBS-02 Límite de mascotas                       | Poder demostrar gating a usuarios | 4 h      |
| 🔴 3      | SUBS-03 Historial escaneos                       | Coherencia con pricing            | 2 h      |
| 🔴 4      | SUBS-04 Contador IA mensual + migración          | Previene abuso del plan free      | 1 día    |
| 🟠 5      | UI-GATE-01 `useMyTier` + UI-GATE-02 `<PlanGate>` | Todos los gates de UI             | 1 día    |
| 🟠 6      | UI-GATE-03/04/05/06 — Gates en pantallas         | UX de upgrade                     | 1 día    |
| 🟠 7      | SUBS-05 Radio de alerta por tier                 | Diferenciador Plus                | 4 h      |
| 🟠 8      | SUBS-06 Broadcast WhatsApp gating                | Gating canal premium              | 2 h      |
| 🟠 9      | SUBS-07/08/09 Case Room + Bounty + GPS gating    | Gating features Plus              | 4 h      |
| 🔵 10     | FAM-02 Historial médico (modelo + API)           | FAM-03/04/05                      | 1.5 sem  |
| 🔵 11     | FAM-03 Recordatorios veterinarios                | —                                 | 1 sem    |
| 🔵 12     | FAM-04 Export PDF médico                         | FAM-02                            | 3 días   |
| 🔵 13     | FAM-01 Multi-usuario familiar                    | FAM-05                            | 3 sem    |
| 🔵 14     | FAM-05 Push familiar                             | FAM-01                            | 2 días   |
| 🔵 15     | SUBS-10 Fix movement prediction (Plus vs public) | —                                 | 4 h      |

**Total estimado mínimo para poder "cobrar correctamente":**  
SUBS-01→09 + UI-GATE-01→06 = ~1 semana de trabajo continuo.  
Familia completo (FAM-01→05) = ~5–6 semanas adicionales.

---
