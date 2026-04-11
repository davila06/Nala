# PawTrack — Plan de Multipaís e Internacionalización

> Análisis realizado el 8 de abril de 2026.  
> Base de comparación: PawTrack CR v1 (Sprint 1 completado).

---

## 1. Resumen ejecutivo

El núcleo funcional de PawTrack (CRUD de mascotas, QR, reportes de pérdida, avistamientos, notificaciones, mapas) es agnóstico geográficamente. Con 1–2 días de trabajo es posible desplegar la app en cualquier país hispanohablante. El soporte multimoneda, multi-timezone y multi-idioma completo requiere fases adicionales.

---

## 2. Diagnóstico de bloqueos actuales

### 2.1 Bloqueos funcionales — rompen la lógica en otro país

| # | Archivo | Línea | Problema | Esfuerzo |
|---|---------|-------|----------|----------|
| 1 | `backend/src/PawTrack.Domain/Locations/UserLocation.cs` | 101 | `IsInQuietHours()` hardcodea `UTC-6` (Costa Rica). En México, España o Argentina las horas quietas serían incorrectas. Código: `utcNow.ToOffset(TimeSpan.FromHours(-6)).TimeOfDay` | Medio |
| 2 | `backend/src/PawTrack.Domain/LostPets/LostPetEvent.cs` | 44 | `RewardAmount` asume moneda CRC (colón costarricense). Sin campo `CurrencyCode` no se puede mostrar ni formatear correctamente en otra moneda. Comentario: `"Currency is always CRC (Costa Rican colón) in this MVP."` | Medio |

### 2.2 Degradaciones UX — funcionará pero se verá mal

| # | Archivo | Línea | Problema | Esfuerzo |
|---|---------|-------|----------|----------|
| 3 | `frontend/src/features/lost-pets/pages/LostReportConfirmationPage.tsx` | 151 | Texto de share hardcodeado: `"${pet.name} está perdido en Costa Rica. ¿Lo has visto?"` | Trivial |
| 4 | `frontend/src/features/auth/pages/LoginPage.tsx` | 32 | Subtítulo: `"una red comunitaria de rescate para Costa Rica."` | Trivial |
| 5 | `frontend/index.html` | 13 | Content-Security-Policy contiene `https://*.pawtrack.cr` — rompe el frontend en otro dominio | Trivial |
| 6 | `backend/src/PawTrack.API/appsettings.json` | — | `BaseUrl: "https://pawtrack.cr"` — solo cambio de config, no de código | Trivial |
| 7 | `backend/src/PawTrack.API/Controllers/PublicMapController.cs` | 18 | `MaxDegreeSpan = 5.0` — comentario explica el límite en términos de CR (≈460 km). El valor de 5° funciona para países medianos pero es insuficiente para EE.UU. (4500 km N-S) o Brasil | Bajo |

### 2.3 Lo que ya es agnóstico (no requiere cambio)

| Área | Estado |
|------|--------|
| GPS y coordenadas lat/lng | ✅ Universal |
| Fotos en Azure Blob Storage | ✅ Sin restricción geográfica |
| Autenticación JWT | ✅ Estándar |
| Razas de mascotas | ✅ Sin datos CR-específicos |
| Idioma UI | ✅ Español — funciona para cualquier país hispanohablante |
| Generación de QR | ✅ URL construida desde config `App__BaseUrl` |
| Rate limiting | ✅ Sin filtrado por país |
| Geo-matching de mascotas | ✅ Bounding box en grados, funciona globalmente |
| Validación de imágenes | ✅ MIME + magic bytes genéricos |
| Estructura de módulos | ✅ Clean Architecture, sin acoplamiento geográfico |

---

## 3. Plan de implementación por fases

### Fase 1 — Otro país hispanohablante (1–2 días)

Objetivo: desplegar la app en cualquier país de América Latina o España sin cambios estructurales.

**Cambios requeridos:**

1. **`LostReportConfirmationPage.tsx` L151**  
   Reemplazar `"Costa Rica"` por una variable de entorno o config:
   ```tsx
   // Antes
   text: `${pet.name} está perdido en Costa Rica. ¿Lo has visto?`,
   
   // Después
   text: `${pet.name} está perdido. ¿Lo has visto?`,
   // O con variable de config:
   text: `${pet.name} está perdido en ${APP_CONFIG.countryName}. ¿Lo has visto?`,
   ```

2. **`LoginPage.tsx` L32**  
   ```tsx
   // Antes
   una red comunitaria de rescate para Costa Rica.
   
   // Después
   una red comunitaria de rescate para mascotas.
   // O con variable:
   una red comunitaria de rescate para {APP_CONFIG.countryName}.
   ```

3. **`index.html` — Content-Security-Policy**  
   Mover el dominio a variable de build de Vite:
   ```html
   <!-- Antes (hardcoded) -->
   connect-src 'self' https://*.pawtrack.cr https://*.azure.com
   
   <!-- Después: usar env var o restringir solo a 'self' y azure.com -->
   connect-src 'self' https://*.azure.com
   ```
   O generar el header CSP desde el servidor donde sí existe la variable de entorno.

4. **`appsettings.json`**  
   Ya usa variables de App Service — solo cambiar `App__BaseUrl` y `Cors__AllowedOrigins__0` en el portal de Azure.

---

### Fase 2 — Multi-timezone (1–3 días)

Objetivo: que las horas quietas de notificaciones funcionen correctamente para usuarios en cualquier zona horaria.

**Archivos afectados:**
- `backend/src/PawTrack.Domain/Locations/UserLocation.cs` (L99–101)
- `backend/src/PawTrack.Infrastructure/Persistence/Configurations/UserLocationConfiguration.cs`

**Cambios requeridos:**

1. Agregar `TimeZoneId` al perfil de usuario (entidad `User` o `UserLocation`):
   ```csharp
   // En User o UserLocation
   public string TimeZoneId { get; private set; } = "America/Costa_Rica"; // default
   ```

2. Modificar `IsInQuietHours()` en `UserLocation.cs`:
   ```csharp
   // Antes — hardcoded UTC-6
   var crTime = TimeOnly.FromTimeSpan(
       utcNow.ToOffset(TimeSpan.FromHours(-6)).TimeOfDay);
   
   // Después — usa la timezone del usuario
   public bool IsInQuietHours(DateTimeOffset utcNow, string timeZoneId = "America/Costa_Rica")
   {
       if (QuietHoursStart is null || QuietHoursEnd is null) return false;
       var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
       var localTime = TimeOnly.FromTimeSpan(
           TimeZoneInfo.ConvertTimeFromUtc(utcNow.UtcDateTime, tz).TimeOfDay);
       // ... resto de la lógica igual
   }
   ```

3. El frontend puede enviar el timezone del navegador:
   ```typescript
   const timeZoneId = Intl.DateTimeFormat().resolvedOptions().timeZone;
   // "America/New_York", "Europe/Madrid", etc.
   ```

**Migración requerida:**  
Agregar columna `TimeZoneId NVARCHAR(100) NOT NULL DEFAULT 'America/Costa_Rica'` a la tabla correspondiente.

---

### Fase 3 — Multi-moneda (3–5 días)

Objetivo: que el campo `RewardAmount` funcione con cualquier moneda local.

**Archivos afectados:**
- `backend/src/PawTrack.Domain/LostPets/LostPetEvent.cs` (L43–46)
- Validadores de `ReportLost` command
- Frontend: `ReportLostPage.tsx`, `PublicPetProfilePage.tsx`, `CaseRoomPage.tsx`

**Cambios requeridos:**

1. Agregar `CurrencyCode` a `LostPetEvent`:
   ```csharp
   // Antes
   /// Currency is always CRC (Costa Rican colón) in this MVP.
   public decimal? RewardAmount { get; private set; }
   
   // Después
   public decimal? RewardAmount { get; private set; }
   public string? CurrencyCode { get; private set; } // "CRC", "USD", "EUR", "MXN", etc.
   ```

2. Frontend — formateo dinámico:
   ```typescript
   const formatted = rewardAmount
     ? new Intl.NumberFormat(locale, {
         style: 'currency',
         currency: currencyCode ?? 'USD',
       }).format(rewardAmount)
     : null;
   ```

3. Variable de configuración global para moneda por defecto:
   ```typescript
   // src/lib/config.ts
   export const DEFAULT_CURRENCY = import.meta.env.VITE_DEFAULT_CURRENCY ?? 'USD';
   ```

**Migración requerida:**  
Agregar columna `CurrencyCode NVARCHAR(3) NULL` a `LostPetEvents`.

---

### Fase 4 — Multi-idioma completo con i18n (2–4 semanas)

Objetivo: soporte de múltiples idiomas (español, inglés, portugués, etc.).

**Decisión de librería:**  
Usar **`react-i18next`** + `i18next` — es el estándar en el ecosistema React.

**Estructura recomendada:**
```
frontend/src/
  i18n/
    locales/
      es/
        common.json
        auth.json
        pets.json
        lost-pets.json
        sightings.json
        notifications.json
      en/
        common.json
        auth.json
        ... (mismos archivos)
      pt/
        ...
    index.ts       ← configuración de i18next
```

**Instalación:**
```bash
npm install i18next react-i18next i18next-browser-languagedetector
```

**Configuración base (`src/i18n/index.ts`):**
```typescript
import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    fallbackLng: 'es',
    supportedLngs: ['es', 'en', 'pt'],
    interpolation: { escapeValue: false },
    resources: { /* carga dinámica */ },
  });
```

**Uso en componentes:**
```tsx
// Antes
<p>una red comunitaria de rescate para Costa Rica.</p>

// Después
import { useTranslation } from 'react-i18next';
const { t } = useTranslation('auth');
<p>{t('login.subtitle')}</p>
```

**Prioridad de strings a externalizar (orden sugerido):**
1. Textos con "Costa Rica" hardcodeado (3 instancias — Fase 1)
2. Mensajes de error y validación
3. Labels de formularios
4. Textos de estado (perdido, encontrado, reunido)
5. Emails transaccionales (backend — requiere templates)
6. Notificaciones push

---

## 4. Archivos con mayor densidad de hardcoding

Estos archivos concentran la mayor cantidad de texto CR-específico y serán los primeros en tocarse al internacionalizar:

| Archivo | Tipo de contenido | Prioridad |
|---------|-------------------|-----------|
| `frontend/src/features/lost-pets/pages/LostReportConfirmationPage.tsx` | Texto de share social | Alta |
| `frontend/src/features/auth/pages/LoginPage.tsx` | Subtítulo de marca | Alta |
| `backend/src/PawTrack.Domain/Locations/UserLocation.cs` | Lógica UTC-6 | Alta |
| `backend/src/PawTrack.Domain/LostPets/LostPetEvent.cs` | Asunción de moneda CRC | Media |
| `frontend/index.html` | CSP con dominio .cr | Alta |
| `backend/src/PawTrack.API/Controllers/PublicMapController.cs` | Límite de bbox para CR | Baja |

---

## 5. Variables de entorno para multipaís

Agregar estas variables al proceso de build y a App Service para soporte multi-país sin cambios de código:

### Frontend (`.env.production`)
```dotenv
VITE_COUNTRY_NAME=Costa Rica
VITE_DEFAULT_CURRENCY=CRC
VITE_DEFAULT_LOCALE=es-CR
VITE_DEFAULT_TIMEZONE=America/Costa_Rica
VITE_APP_TITLE=PawTrack CR
```

### Backend (App Service settings)
```
App__CountryName=Costa Rica
App__DefaultCurrency=CRC
App__DefaultTimezone=America/Costa_Rica
App__BaseUrl=https://pawtrack.cr
```

---

## 6. Consideraciones adicionales para expansión internacional

### Privacidad y regulación
- **Costa Rica**: regulado por PRODHAB (Ley 8968)
- **Unión Europea/España**: GDPR — requiere consentimiento explícito, derecho al olvido, DPA
- **México**: LFPDPPP
- **Brasil**: LGPD

La app actualmente no tiene una pantalla de consentimiento de privacidad explícita. Para UE sería bloqueante.

### Números de teléfono
- El placeholder `+506 8888-0000` en `ReportLostPage.tsx` es informativo, no un validador.
- El backend scrubber en `PiiScrubber.cs` ya usa un regex genérico (no restringido a CR).
- Para un campo de teléfono con selector de país, usar `react-phone-number-input`.

### Dirección y geocodificación
- `IReverseGeocodingService` está documentada para "Costa Rica canton names".
- Para otros países necesita ser generalizada a "administrative area" (estado/departamento/municipio).
- Azure Maps soporta geocodificación global — el cambio es en el contrato de la interfaz, no en la infraestructura.

### Dominio y marca
- "PawTrack CR" como nombre puede variar por país: PawTrack MX, PawTrack ES, etc.
- Alternativamente, usar solo "PawTrack" como marca global desde el inicio.

---

## 7. Estimado de esfuerzo total

| Fase | Alcance | Estimado |
|------|---------|----------|
| Fase 1 | País hispanohablante sin cambios estructurales | 1–2 días |
| Fase 2 | Multi-timezone | 2–3 días + migración |
| Fase 3 | Multi-moneda | 3–5 días + migración |
| Fase 4 | i18n completo (español + inglés) | 2–4 semanas |
| Fase 5 | Regulación UE / GDPR | 1–2 semanas adicionales |

---

## 8. Recomendación para el MVP actual

Para el lanzamiento en Costa Rica **no se requiere ningún cambio**. La app está correctamente localizada para CR.

Para una **expansión a otra región hispanohablante** en el corto plazo, solo se necesita la Fase 1 (1–2 días): 3 cambios de strings y la variable de dominio en la CSP.

Las Fases 2–4 pueden implementarse **cuando exista demanda real** de otro mercado, sin bloquear el lanzamiento actual.
