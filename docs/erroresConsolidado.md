# PawTrack CR — Pendientes Consolidados

> Última actualización: 2026-07-30 (sesión de trabajo completa)
> Fuentes: `PENDIENTES_BETA.md`, `collares.md`, `mejorasUI.md`, `multi-idioma.md`, `pasos para ir live.md`, `pricing.md`, `PawTrack_Documento_Maestro_v3.1.md`, `GUIA_ONBOARDING_DEV.md`

---

## Resumen ejecutivo de estado

| Categoría                 | Total   | ⛔ Pendiente | 🔄 Parcial | ✅ Hecho |
| ------------------------- | ------- | ------------ | ---------- | -------- |
| Infraestructura / DevOps  | 6       | 2            | 0          | 4        |
| Seguridad / Configuración | 2       | 1            | 0          | 1        |
| Features backend          | 5       | 2            | 0          | 3        |
| Features frontend         | 7       | 2            | 0          | 5        |
| Bugs / fixes producción   | 8       | 0            | 0          | 8        |
| Internacionalización      | 4       | 2            | 0          | 2        |
| Módulos nuevos (GPS)      | 1       | 1            | 0          | 0        |
| UI/UX (rediseño)          | 6 fases | 5            | 1          | 0        |
| Monetización              | 4       | 4            | 0          | 0        |

---

## 1. Infraestructura y DevOps

### 1.1 ⛔ Dominio personalizado `pawtrack.cr`

**Fuente:** `PENDIENTES_BETA.md → P-09`  
**Impacto:** Requerido antes del lanzamiento público.  
**Pasos:**

1. Comprar `pawtrack.cr` en Namecheap/GoDaddy.
2. Crear CNAME apuntando al FQDN del Container App.
3. Agregar custom domain en Azure Container App.
4. Actualizar `Cors__AllowedOrigins__0` y `App__BaseUrl` en Key Vault.
5. Actualizar `VITE_API_URL` en GitHub Secrets y re-deployar frontend.

---

### 1.2 ⛔ GitHub Secrets para CI/CD

**Fuente:** `PENDIENTES_BETA.md → P-11`  
**Impacto:** Los workflows `backend.yml` y `frontend.yml` existen pero no se ejecutan porque los secrets no están configurados.  
**Acción:** Ir a `GitHub repo → Settings → Secrets and variables → Actions` y agregar:

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

---

### 1.3 ✅ backend.yml corregido (Docker + ACR + Container App)

**Fuente:** `PENDIENTES_BETA.md` + análisis de código  
**Estado:** Corregido en commit `aeff566` (2026-07-30). El deploy job ahora hace `docker build` → `push ACR` → `az containerapp update`.

---

### 1.4 ✅ Infraestructura beta en Azure

**Fuente:** `PENDIENTES_BETA.md → P-01 a P-07`  
**Estado:** Todo completado. Container App corriendo con imagen `v1.0.0-beta`, migraciones aplicadas, CORS configurado.

---

## 2. Seguridad y Configuración de Canales

### 2.1 ⛔ Bot de WhatsApp — configuración Meta Cloud API

**Fuente:** `PENDIENTES_BETA.md → P-10`, `pasos para ir live.md → §6`  
**Impacto:** El handler `HandleWhatsAppWebhookCommandHandler` está completamente implementado, pero sin configurar no procesa mensajes.  
**Pasos:**

1. Crear app en [Meta for Developers](https://developers.facebook.com/) → agregar producto WhatsApp Business.
2. Obtener `Phone Number ID` y generar Permanent Token.
3. Registrar webhook en Meta: `POST https://<fqdn>/api/whatsapp/webhook`.
4. Agregar en Container App:
   ```powershell
   az containerapp update --name pawtrack-dev-api --resource-group PawnTrackBeta \
     --set-env-vars \
       "WhatsApp__BearerToken=secretref:whatsapp-bearer-token" \
       "WhatsApp__VerifyToken=secretref:whatsapp-verify-token" \
       "WhatsApp__PhoneNumberId=<PHONE_NUMBER_ID>"
   ```
5. Verificar que el webhook responde con `hub.challenge` en Meta Developer Console.

---

### 2.2 ✅ Push Notifications — VAPID directo sin proveedor externo

**Estado:** ✅ Implementado en commit `93fc3cf` (2026-07-30).  
`PushNotificationService` reescrito para enviar directamente via Web Push Protocol (RFC 8030) con VAPID. Claves en `appsettings.Local.json` y `frontend/.env.local`.  
**Para Azure:** agregar `Notifications__Push__VapidPublicKey` + `Notifications__Push__VapidPrivateKey` (en Key Vault) al Container App.

---

## 3. Features de Backend Pendientes

### 3.1 ✅ Microchip RFID — registrar y actualizar desde UI owner

**Fuente:** `collares.md → Parte 1`  
**Estado:** ✅ Implementado en commit `aeff566` (2026-07-30).

- `UpdatePetCommand` + validator ISO 11784, `PetDto` expone `MicrochipId`, `PetForm` con campo hexadecimal, `CreatePetPage` + `PetDetailPage`.

---

### 3.2 ⛔ Collar GPS — módulo completo sin implementar

**Fuente:** `collares.md → Parte 2`  
**Impacto:** No existe ningún archivo de este módulo en el repo.  
**Arquitectura documentada en `collares.md`** — incluye:

**Backend necesario:**

- Entidades: `Collar`, `CollarLocation` (write-heavy, sin EF tracking)
- Enum: `CollarProvider` (Own, Tractive, Kippy)
- Commands: `RegisterCollar`, `RecordCollarLocation`, `DeactivateCollar`
- Queries: `GetCollarStatus`, `GetLocationHistory`
- Controller: `api/collars`
- Background job: polling a Tractive API cada 5 min
- OAuth2 flow para conectar cuenta Tractive
- EF Config: índice en `(CollarId, RecordedAt DESC)`
- SQL purge job: eliminar localizaciones > 30 días

**Frontend necesario:**

- Sección en `PetDetailPage` para vincular/desvincular collar
- Mapa de última posición GPS en tab de mascota
- Historial de trayectoria (últimas N horas)

**Opción recomendada para MVP:** Integrar API Tractive (sin hardware propio).

---

### 3.3 ✅ EF Core migration `AddUserSoftDelete`

**Estado:** ✅ Generada y aplicada en DB local (`6ebee07`, 2026-07-30). Pendiente aplicar en Azure SQL de producción via CI/CD.

---

### 3.5 ✅ Estadísticas — `RecoveredCount` correcto sin depender de GPS

**Estado:** ✅ Corregido (sesión 2026-07-30). `RecoveryStatsRawData` ahora tiene campo `RecoveredCount` separado que cuenta todos los `LostPetEvent.Status == Reunited` sin requerir `RecoveryDistanceMeters != null`.

---

### 3.6 ✅ Mascota reactivable desde estado Reunida

**Estado:** ✅ Implementado en commit `d95c530` (2026-07-30). `PATCH /api/pets/{id}/reactivate` + botón "Marcar como activa" en `PetDetailPage`.

---

## 4. Features de Frontend Pendientes

### 4.1 ✅ Cambiar contraseña en ProfilePage

**Estado:** ✅ Implementado en commit `aeff566` (2026-07-30).

---

### 4.2 ✅ Eliminar cuenta en ProfilePage

**Estado:** ✅ Implementado en commit `aeff566` (2026-07-30). Soft-delete con confirmación por contraseña.

---

### 4.3 ✅ Foster — map picker en ProfilePage

**Estado:** ✅ Implementado en commit `6ebee07` (2026-07-30). `LastSeenMap` reemplaza inputs numéricos.

---

### 4.4 ⛔ Certificado veterinario con PDF firmado digitalmente

**Fuente:** `pricing.md → §7`  
**Impacto:** Feature de valor para clínicas Partner. Aún no existe en backend ni frontend.  
**Descripción:** Al registrar vacunas/consultas, la clínica genera un PDF/A-1b con firma digital y QR de verificación.

---

### 4.5 ⛔ Integración con perreras / municipalidades

**Fuente:** `PawTrack_Documento_Maestro_v3.1.md → §11`, `pricing.md → §4`  
**Impacto:** Canal de mayor ticket. 82 municipalidades en CR.  
**Tiers documentados:**

- Básica: ₡150,000/año — portal de control animal, capturados, mapa
- Full: ₡300,000/año — API de consulta, reportes mensuales, SLA
- Red Regional: ₡500,000/año — múltiples cantones

---

### 4.6 ✅ ProfilePage — mejoras UX enterprise

**Estado:** ✅ Implementado en commit `81cc944` (2026-07-30).

- Rol localizado en español, especies con emoji, "Miembro desde" con fecha
- Indicador fortaleza contraseña (4 niveles), validación inline de coincidencia
- Botón custodio condicional, resumen colapsado, toggle push nativo

---

## 5. Internacionalización (multi-país)

**Fuente:** `multi-idioma.md`  
Todos los ítems siguientes son ⛔ sin implementar.

### 5.1 ✅ Textos hardcodeados "Costa Rica" eliminados

**Estado:** ✅ Eliminados en commit `6ebee07` (2026-07-30). Tres ubicaciones corregidas: share text, WhatsApp message, CSP en `index.html`.

---

### 5.2 ✅ Content-Security-Policy actualizada

**Estado:** ✅ Corregida (sesión 2026-07-30). Eliminado `*.pawtrack.cr` hardcodeado; añadidos `raw.githubusercontent.com`, `cdn.jsdelivr.net`, `www.geoboundaries.org`, `github.com` para activos legítimos.

**Archivo:** `frontend/index.html`  
**Problema:** `connect-src` contiene `https://*.pawtrack.cr` — rompe el frontend en otro dominio.  
**Fix:** Generar CSP desde el servidor o usar solo `'self'` + `*.azure.com`.

---

### 5.3 ⛔ Horas quietas hardcodeadas en UTC-6 (Costa Rica)

**Archivo:** `backend/src/PawTrack.Domain/Locations/UserLocation.cs` — método `IsInQuietHours()`  
**Problema:** `utcNow.ToOffset(TimeSpan.FromHours(-6))` — hardcodeado para GMT-6.  
**Fix:**

```csharp
public bool IsInQuietHours(DateTimeOffset utcNow, string timeZoneId = "America/Costa_Rica")
{
    var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    var localTime = TimeOnly.FromTimeSpan(
        TimeZoneInfo.ConvertTimeFromUtc(utcNow.UtcDateTime, tz).TimeOfDay);
    // ... resto igual
}
```

**Migración necesaria:** agregar columna `TimeZoneId NVARCHAR(100) NOT NULL DEFAULT 'America/Costa_Rica'`.

---

### 5.4 ⛔ Moneda CRC hardcodeada en `LostPetEvent`

**Archivo:** `backend/src/PawTrack.Domain/LostPets/LostPetEvent.cs`  
**Comentario en código:** `"Currency is always CRC (Costa Rican colón) in this MVP."`  
**Fix:**

```csharp
public decimal? RewardAmount { get; private set; }
public string? CurrencyCode { get; private set; } // "CRC", "USD", "MXN"
```

**Migración necesaria:** `CurrencyCode NVARCHAR(3) NULL` en `LostPetEvents`.

---

## 6. Módulo Collar GPS (nuevo — sin ningún archivo)

**Fuente:** `collares.md → Parte 2`  
Ver sección 3.2 de este documento para el detalle completo de arquitectura.

**Resumen de esfuerzo:**

- Backend: ~8 archivos de dominio/application + migration + controller
- Frontend: sección en PetDetailPage + mapa GPS
- Integración: OAuth2 con Tractive para MVP, firmware propio para v2

---

## 7. Rediseño UI/UX (6 fases)

**Fuente:** `mejorasUI.md`  
**Estado:** Plan completo documentado. Ninguna fase ejecutada.

| Fase  | Descripción                                                                 | Duración est. |
| ----- | --------------------------------------------------------------------------- | ------------- |
| **0** | Descubrimiento — moodboard, dirección creativa, criterios                   | 3–5 días      |
| **1** | Sistema de diseño — tokens, componentes base, guía de estilos               | 1–2 semanas   |
| **2** | Layouts globales — app shell, navegación por rol, responsive                | 4–7 días      |
| **3** | Flujos críticos — Auth, Dashboard, Pet Detail, Report Lost, Report Sighting | 2–3 semanas   |
| **4** | Experiencias operativas — Chat, Notifications, Case Room, Map, Visual Match | 1.5–2 semanas |
| **5** | Módulos por rol — Ally panel, Clinic dashboard, Admin                       | 1–1.5 semanas |
| **6** | Pulido final — accesibilidad, responsive QA, motion, checklist release      | 4–7 días      |

**Hallazgos clave del diagnóstico:**

1. Copy mezclado español/inglés en varios módulos.
2. Diferencia de calidad visual importante entre módulos.
3. Layout autenticado básico para una app con múltiples roles.
4. Sin sistema de diseño formal — tokens, espaciado y componentes inconsistentes.
5. Accesibilidad parcialmente atendida, no sistematizada.

**Pantallas de prioridad crítica para Fase 3:**  
Login → Register → Dashboard → Create/Edit Pet → Pet Detail → Public Pet Profile → Report Lost → Lost Report Confirmation → Report Sighting → Chat.

---

## 8. Monetización Pendiente

**Fuente:** `pricing.md`

| #   | Feature                                          | Estado | Ingreso potencial                   |
| --- | ------------------------------------------------ | ------ | ----------------------------------- |
| 8.1 | Tiers de pago para clínicas (Plus/Partner)       | ⛔     | ₡450,000+/mes con 30 clínicas       |
| 8.2 | Licencias para municipalidades/perreras          | ⛔     | ₡750,000+/año con 5 municipalidades |
| 8.3 | Certificado veterinario PDF firmado              | ⛔     | Feature clave del tier Partner      |
| 8.4 | Marketplace de productos/servicios para mascotas | ⛔     | Comisión por transacción            |

---

## 9. Checklist de pre-lanzamiento

Lista operativa para el go-live en producción:

- [ ] **1.1** Dominio `pawtrack.cr` registrado y CNAME configurado
- [ ] **1.2** GitHub Secrets configurados (10 secrets listados en §1.2)
- [ ] **2.1** WhatsApp webhook registrado en Meta y variables en Container App
- [x] **2.2** Push VAPID configurado en local — pendiente Container App vars en Azure
- [x] **3.3** Migración `AddUserSoftDelete` aplicada en DB local — aplicar en Azure SQL via CI/CD
- [ ] Smoke tests pasando en `https://pawtrack.cr/health`
- [ ] `VITE_API_URL` apuntando al dominio definitivo en el build de frontend
- [ ] `Cors__AllowedOrigins__0` = `https://pawtrack.cr` en Container App
- [ ] `App__BaseUrl` = `https://pawtrack.cr` en Container App

---

## 10. Fixes resueltos en sesión 2026-07-30

| Fix | Descripción                                                                                                            |
| --- | ---------------------------------------------------------------------------------------------------------------------- |
| ✅  | `CookieConsentBanner` usaba `<Link>` fuera de `<RouterProvider>` → reemplazado con `<a>`                               |
| ✅  | `LeaderboardWidget` key warning — clave compuesta para IDs null/duplicados                                             |
| ✅  | `Tabs.tsx` bg-white en dark mode → `bg-[var(--color-surface)]`                                                         |
| ✅  | Scrollbar visible en tab bar de `PetDetailPage` → `.no-scrollbar`                                                      |
| ✅  | GeoJSON cantones CR cargaba desde Git LFS (404) → archivo local `public/geojson/cantons-cr.geojson` (GADM 4.1, 333 KB) |
| ✅  | `THREE.Clock` deprecation warning en devtools → filtrado en `main.tsx` solo en DEV                                     |
| ✅  | CSP bloqueaba `raw.githubusercontent.com`, `cdn.jsdelivr.net` → agregados a `connect-src`                              |
| ✅  | `/encontre-mascota` GPS denegado mostraba inputs lat/lng manuales inutilizables → mapa interactivo `LastSeenMap`       |
| ✅  | WebGL context lost en componentes 3D → `e.preventDefault()` en `contextlost`, `dpr` reducido                           |
| ✅  | `petsApi.ts` sintaxis rota por formatter (campo sin `;`) → corregido                                                   |
| ✅  | `ReportFoundPetPage` duplicado de `gpsError` por formatter → eliminado                                                 |

---

## Apéndice — Archivos fuente de este documento

| Archivo                              | Tipo de pendientes                                  |
| ------------------------------------ | --------------------------------------------------- |
| `docs/PENDIENTES_BETA.md`            | Infra Azure, secrets, CI/CD                         |
| `collares.md`                        | Microchip (✅ hecho), Collar GPS (⛔ pendiente)     |
| `mejorasUI.md`                       | Rediseño UI/UX completo — 6 fases                   |
| `multi-idioma.md`                    | Internacionalización — 4 bloqueantes funcionales    |
| `pasos para ir live.md`              | Checklist de producción, configuraciones opcionales |
| `pricing.md`                         | Monetización — 4 líneas de ingreso sin implementar  |
| `PawTrack_Documento_Maestro_v3.1.md` | Backlog estratégico de alto nivel                   |
