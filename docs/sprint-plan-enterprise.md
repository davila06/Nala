# PawTrack CR — Enterprise Sprint Plan

> **Última actualización: 2026-08-19**
> **Estado: SPRINT COMPLETADO** — Todos los features F1-F9 del Sprint Next + Sprint +2 están implementados.

---

## Estado de los features

| Feature | Estado | Tests |
|---------|--------|-------|
| F1: Smart Predictive Health Reminders | ✅ Completado | ✅ |
| F2: Weight Trend Chart + HealthScore | ✅ Completado | ✅ |
| F3: Activity Logging + BreedBenchmark | ✅ Completado | ✅ |
| F4: Annual Report PDF | ✅ Completado | — |
| F5: Neighbor Network (Guardia Vecinal) | ✅ Completado | — |
| F6: Bounty con SINPE + HandoverCode | ✅ Completado | ✅ 16 tests |
| F7: Clinic Medical Access Grants | ✅ Completado | — |
| F8: Clinic Scan Notifications | ✅ Completado | — |
| F9: Admin Promotions Tab | ✅ Completado | — |
| **Tiendas de mascotas (B2B)** | ✅ Completado | ✅ 16 tests |
| **Vallas publicitarias (Billboard)** | ✅ Completado | — |
| **SignalR Chat real-time** | ✅ Completado | — |
| **SQL JTI Blocklist** | ✅ Completado | — |
| **Security hardening (rounds 1-51+)** | ✅ Completado | ✅ 916 tests |

---

## Features adicionales post-sprint (agosto 2026)

### Tiendas de mascotas (Store B2B)
- Domain: Store, StoreProduct, StoreOrder, StoreOrderItem con state machine completo
- Plan gate: solo StorePlus/StorePartner reciben pedidos in-app
- Frontend: mapa con markers, catálogo, carrito (Zustand persist), checkout SINPE, dashboard para dueños
- Admin: aprobación de tiendas pendientes
- Tests: 16 tests (state machine + handlers + validación)

### Vallas publicitarias (Billboard)
- Domain: Billboard con Placement, Status, prioridad, CTA seguro
- 4 placements: Map, Dashboard, Directory, Feed
- Frontend: `BillboardBanner` dismissible por sesión, rotación por prioridad
- Admin: tab "Vallas" con CRUD + upload imagen + activar/pausar
- Integrado en: Mapa público (placement Map)

### SignalR ChatHub
- Hub en `/hubs/chat` — push inmediato en mensajes nuevos
- `useChatSignalR` hook con `withAutomaticReconnect`
- Poll 10s como fallback si la conexión falla

### JTI Blocklist distribuido
- `RevokedTokens` tabla en SQL
- `RevokedTokenCleanupJob` (BackgroundService, purga nocturna)
- Reemplaza `InMemoryJtiBlocklist` — funciona con App Service scale-out

---

## Convenciones enterprise aplicadas

> Todas las reglas del plan enterprise fueron aplicadas en todos los ítems:
- Clean Architecture + CQRS via MediatR en todos los handlers
- FluentValidation en pipeline — jamás validación manual en handlers
- Result\<T\> o domain exceptions — sin excepciones cruzando módulos
- Guid v7 PKs en todos los nuevos entities
- Problem Details (RFC 7807) en todos los errores HTTP
- Fire-and-forget siempre con `CancellationToken.None` + `.ContinueWith(LogWarning, OnlyOnFaulted)`
- Rate limiting en todos los nuevos endpoints
- BOLA checks en todas las queries de recursos sensibles (Collars, Bounties, Family)
>
> - Backend: Clean Architecture · CQRS via MediatR · FluentValidation en pipeline · Result<T> · Guid v7 PKs · Problem Details (RFC 7807)
> - Nunca lanzar excepciones de dominio entre módulos — usar notificaciones MediatR o Result
> - Frontend: TypeScript strict · React Query para server state · Zustand solo para UI state · co-locate component + hook + test
> - Tests: cada Handler tiene unit test con NSubstitute · cada endpoint crítico tiene integration test · cada hook tiene test con MSW
> - No comentarios que explican lo que el código ya dice — solo comentarios que explican el "por qué"
> - Migraciones EF Core: una por feature, nunca editar migración aplicada a entorno compartido

---

## FEATURE 1 — Smart Predictive Health Reminders

> **Objetivo:** Engine de reglas que analiza el expediente y crea VetReminders proactivos antes de que venzan, sin intervención del dueño.

### 1.1 · Domain

- [ ] Crear `backend/src/PawTrack.Domain/Medical/HealthProtocol.cs`
  - Props: `Id (Guid)`, `Species (string)`, `RecordType (MedicalRecordType)`, `IntervalDays (int)`, `DisplayName (string)`, `IsSystemDefined (bool)`
  - Factory: `HealthProtocol.Define(species, recordType, intervalDays, displayName)`
  - Regla: `IsOverdue(DateOnly lastDate)` → `lastDate.AddDays(IntervalDays) < DateOnly.Today`
  - Regla: `DaysUntilDue(DateOnly lastDate)` → `(lastDate.AddDays(IntervalDays) - DateOnly.Today).TotalDays`

- [ ] Agregar `HealthScore` value object en `MedicalRecord.cs`
  - Método estático `HealthScore.Calculate(IEnumerable<MedicalRecord> records, IEnumerable<HealthProtocol> protocols)` → score 0-100
  - Regla: cada protocolo cumplido → puntos proporcionales al total de protocolos activos para la especie

### 1.2 · Infrastructure / Persistence

- [ ] Agregar `DbSet<HealthProtocol>` en `PawTrackDbContext`
  - Configuración en `OnModelCreating`: `HasKey`, `HasIndex(p => new { p.Species, p.RecordType }).IsUnique()`
  - Seed data en método `SeedHealthProtocols()`:
    ```
    Dog:   Vaccine/Rabies=365, Vaccine/DHPP=365, Deworming=180, Checkup=365, Medication/Flea=30
    Cat:   Vaccine/Rabies=365, Vaccine/FVRCP=365, Deworming=180, Checkup=365
    Rabbit: Deworming=90, Checkup=180
    Bird:  Checkup=365
    ```
  - Seed ejecutado en migración como `HasData`

- [ ] Crear migración: `dotnet ef migrations add AddHealthProtocols`

- [ ] Crear `IHealthProtocolRepository` en `PawTrack.Domain/Medical/`
  - `Task<IReadOnlyList<HealthProtocol>> GetBySpeciesAsync(string species, CancellationToken ct)`

- [ ] Implementar `HealthProtocolRepository` en `PawTrack.Infrastructure/Medical/`

### 1.3 · Application

- [ ] Crear `backend/src/PawTrack.Application/Medical/HealthAlertCommands.cs`

  **Query:** `GetHealthAlertsQuery(Guid PetId)` → `IReadOnlyList<HealthAlertDto>`
  - `HealthAlertDto`: `{ Protocol: string, LastDate: DateOnly?, DueDate: DateOnly, DaysUntilDue: int, IsOverdue: bool, Severity: "critical"|"warning"|"info" }`
  - Handler: carga últimos registros agrupados por `MedicalRecordType`, aplica `HealthProtocol` de la especie del pet
  - Severidad: overdue → `critical`, ≤14 días → `warning`, 15-30 días → `info`
  - Solo devuelve alertas; omite protocolos donde no hay historial y el pet es nuevo (<90 días)

  **Query:** `GetHealthScoreQuery(Guid PetId)` → `HealthScoreDto { Score: int, Breakdown: [...] }`
  - Validator: `PetId != Guid.Empty`

  **Command:** `DismissHealthAlertCommand(Guid PetId, MedicalRecordType RecordType)` → `Result`
  - Crea un `VetReminder` con dueDate = DueDate del protocolo + title autogenerado
  - Validator: PetId y RecordType requeridos

- [ ] Crear `HealthAlertHostedService` en `PawTrack.Infrastructure/Medical/`
  - Ejecuta diariamente a las 09:00 CR (similar a `VetReminderHostedService`)
  - Para cada mascota activa: llama `GetHealthAlertsQuery` → para alertas `critical` sin VetReminder existente → crea VetReminder automático via MediatR
  - Envía push notification al dueño: "⚠️ {petName} necesita {protocol} — programar cita"
  - Deduplication: no crear VetReminder si ya existe uno del mismo tipo dentro de ±7 días

### 1.4 · API

- [ ] Agregar endpoints en `MedicalController.cs`:
  - `GET /api/pets/{petId}/health-alerts` → `GetHealthAlertsQuery`
  - `GET /api/pets/{petId}/health-score` → `GetHealthScoreQuery`
  - `POST /api/pets/{petId}/health-alerts/dismiss` → `DismissHealthAlertCommand`
  - Todos requieren `[Authorize]` + validar `petId` pertenece al usuario

### 1.5 · Frontend

- [ ] Crear `frontend/src/features/medical/api/healthAlertsApi.ts`
  - Tipos: `HealthAlertDto`, `HealthScoreDto`
  - Funciones: `getHealthAlerts(petId)`, `getHealthScore(petId)`, `dismissHealthAlert(petId, recordType)`

- [ ] Crear `frontend/src/features/medical/hooks/useHealthAlerts.ts`
  - `useHealthAlerts(petId)` → React Query con staleTime 1h
  - `useHealthScore(petId)` → React Query con staleTime 1h
  - `useDismissHealthAlert(petId)` → mutation

- [ ] Crear `frontend/src/features/medical/components/HealthAlertBanner.tsx`
  - Props: `petId: string`
  - Muestra alertas `critical` como banner rojo colapsable con CTA "Programar cita"
  - Muestra alertas `warning` como banner amber
  - Dismiss: oculta la alerta y crea VetReminder via `useDismissHealthAlert`
  - Animación: `framer-motion` para enter/exit de cada alerta
  - Accesibilidad: `role="alert"` en alertas críticas

- [ ] Crear `frontend/src/features/medical/components/HealthScoreCard.tsx`
  - Props: `petId: string`
  - Círculo de progreso SVG con score 0-100 (color: danger/warn/rescue según rango)
  - Breakdown: lista de protocolos con ✓/⚠/✗
  - Tooltip en cada ítem: "Último registro: {fecha}"
  - Solo visible en plan Familia (PlanGate)

- [ ] Integrar `HealthAlertBanner` en `PetDetailPage.tsx` (sobre los tabs, siempre visible)
- [ ] Integrar `HealthScoreCard` en tab "Expediente" de `MedicalHistoryTab.tsx` (arriba de todo)

### 1.6 · Tests

- [ ] `PawTrack.UnitTests/Medical/GetHealthAlertsQueryHandlerTests.cs`
  - Caso: pet con vacuna rábica hace 400 días → alerta critical
  - Caso: pet con vacuna hace 350 días → alerta warning
  - Caso: pet nuevo sin registros → sin alertas
  - Caso: pet con VetReminder existente para mismo tipo → sin alerta duplicada

- [ ] `PawTrack.UnitTests/Medical/HealthScoreCalculatorTests.cs`
  - Caso: todos los protocolos al día → score > 90
  - Caso: todos los protocolos vencidos → score < 30

- [ ] `frontend/tests/features/medical/HealthAlertBanner.test.tsx`
  - Render: muestra badge critical con color danger
  - Interaction: dismiss oculta el banner
  - MSW mock: GET /api/pets/{id}/health-alerts

---

## FEATURE 2 — Weight Trend Chart

> **Objetivo:** Visualización de tendencia de peso por visita veterinaria con rangos de referencia por raza/especie.

### 2.1 · Backend

- [ ] Agregar query `GetWeightHistoryQuery(Guid PetId)` en `MedicalCommands.cs`
  - Returns `WeightHistoryDto { Entries: List<WeightEntryDto>, BreedReference: WeightReferenceDto? }`
  - `WeightEntryDto`: `{ Date: DateOnly, WeightKg: decimal, Source: "Owner"|"Clinic", ClinicName: string? }`
  - `WeightReferenceDto`: `{ MinKg: decimal, MaxKg: decimal, Label: string }` — null si no hay referencia para la raza
  - Handler: query sobre `MedicalRecord WHERE WeightKg IS NOT NULL ORDER BY Date ASC`
  - Alerta embebida en DTO: `WeightChangeAlert: string?` — si la diferencia entre el primer y último peso en 90 días es >15%, incluir mensaje

- [ ] Agregar endpoint `GET /api/pets/{petId}/weight-history` en `MedicalController.cs`
  - Requiere `[Authorize]` + petId ownership
  - Plan gate: solo Familia (retornar 403 con ProblemDetails si plan inferior)

- [ ] Agregar seed data estático de rangos de peso en `BreedWeightReference.cs` (clase estática en Domain)
  - `Dictionary<string, (decimal MinKg, decimal MaxKg)>` con 30+ razas comunes en CR
  - Fallback por especie si la raza no está mapeada

### 2.2 · Frontend

- [ ] Instalar `recharts` → `npm install recharts @types/recharts` en `frontend/`

- [ ] Crear `frontend/src/features/medical/api/weightApi.ts`
  - Tipos: `WeightHistoryDto`, `WeightEntryDto`, `WeightReferenceDto`
  - Función: `getWeightHistory(petId)`

- [ ] Crear `frontend/src/features/medical/hooks/useWeightHistory.ts`
  - `useWeightHistory(petId)` → React Query, staleTime 30min

- [ ] Crear `frontend/src/features/medical/components/WeightTrendChart.tsx`
  - Recharts `LineChart` con área sombreada entre Min/Max de referencia
  - Eje X: fechas formateadas `DD MMM` en es-CR
  - Eje Y: kg con rango dinámico ± 20% del min/max registrado
  - Puntos: diferenciados por Source (círculo = Owner, cuadrado = Clinic)
  - Tooltip custom: fecha, peso, fuente, clinic name si aplica
  - Alert badge si `WeightChangeAlert` está presente (borde amber, mensaje)
  - Empty state: "No hay registros de peso. Agrega el peso en tu próxima visita al veterinario."
  - `aria-label` y `role="img"` en el chart container con descripción textual alternativa
  - Responsive: `<ResponsiveContainer width="100%" height={220}`

- [ ] Integrar `WeightTrendChart` en `MedicalHistoryTab.tsx`
  - Posición: debajo de `HealthScoreCard`, antes de la lista de registros
  - Solo visible si hay al menos 2 entradas con peso (ocultar si no hay datos suficientes)
  - Solo plan Familia (PlanGate wrapper)

### 2.3 · Tests

- [ ] `PawTrack.UnitTests/Medical/GetWeightHistoryQueryHandlerTests.cs`
  - Caso: pet con 3 registros con peso → devuelve lista ordenada por fecha
  - Caso: peso baja >15% en 90 días → WeightChangeAlert presente
  - Caso: sin registros con peso → Entries vacío, sin alerta

---

## FEATURE 3 — NFC Tag en Bundle Orders

> **Objetivo:** Agregar la opción "NFC + QR combo" al flujo de pedido de accesorios y trackear si una visita al perfil provino de NFC o QR.

### 3.1 · Domain & Application

- [ ] Agregar enum value `NfcQrCombo = 3` a `BundleOrderStatus` o crear `BundleProductType` enum:
  - `CollarGpsPlus = 0`, `QrPlate = 1`, `SiliconeTag = 2`, `NfcQrCombo = 3`, `EmergencyPack = 4`

- [ ] Agregar campo `ProductType BundleProductType` a `BundleOrder.cs`
  - Factory method: `BundleOrder.PlaceOrder(userId, petId, productType, addressLine, notes)`
  - Migración: `AddBundleProductType`

- [ ] Agregar `ScanInputType.Nfc = 2` al enum `ScanInputType` en `PawTrack.Domain/Clinics/ScanInputType.cs`
  - (ya existe `ScanInputType`, agregar valor NFC)

- [ ] Agregar query param `?source=nfc|qr` en `PublicController.cs` endpoint de perfil público
  - Handler registra `PetScanEvent` con source correcto

### 3.2 · API

- [ ] Actualizar `BundleOrdersController.cs` para aceptar `productType` en el body de creación

### 3.3 · Frontend

- [ ] Actualizar `frontend/src/features/bundles/components/BundleOrderModal.tsx`
  - Agregar selector de producto: tarjeta visual para cada `BundleProductType`
  - Card "NFC + QR Combo": descripción, precio ₡12,000, beneficios (toca con Android, escanea con iOS)
  - Card seleccionada: borde brand-500, checkmark

- [ ] Crear `frontend/src/features/pets/components/NfcSetupGuide.tsx`
  - Modal/drawer con tutorial paso a paso:
    1. Instalar "NFC Tools" (link App Store / Play Store)
    2. Escribir URL: `https://pawtrack.cr/p/{petId}`
    3. Tocar el tag con el teléfono
    4. Verificar: escanear y confirmar que abre el perfil
  - Accesible desde PetDetailPage → "⚡ Configurar NFC" (solo si tiene tag NFC en pedido)

- [ ] Actualizar `frontend/src/features/pets/api/petsApi.ts`
  - Agregar `scanSource?: 'qr' | 'nfc'` al `PetScanEvent`

### 3.4 · Tests

- [ ] `PawTrack.UnitTests/Bundles/BundleOrderCommandTests.cs`
  - Caso: crear pedido con ProductType NfcQrCombo → se persiste correctamente
- [ ] `frontend/tests/features/bundles/BundleOrderModal.test.tsx`
  - Render: muestra opción NFC en el selector de producto

---

## FEATURE 4 — Emergency Vet Finder

> **Objetivo:** Marcar clínicas como 24h emergencias y surfacearlas en momentos críticos (mascota perdida, alerta de salud).

### 4.1 · Domain

- [ ] Agregar campos a `Clinic.cs`:
  - `public bool IsEmergency24h { get; private set; }`
  - `public string? EmergencyPhone { get; private set; }` — puede diferir del PhoneNumber principal
  - Método: `SetEmergencyStatus(bool is24h, string? emergencyPhone)`

- [ ] Migración: `AddClinicEmergencyFields`

### 4.2 · Application / API

- [ ] Actualizar `ClinicDto` / `ClinicProfileView` para incluir `IsEmergency24h` y `EmergencyPhone`

- [ ] Agregar campo `isEmergency24h: bool` al `GET /api/public/map` query filter
  - Handler: si `isEmergency24h = true`, filtrar solo clínicas con ese flag

- [ ] Actualizar `ClinicsController.cs` endpoint de actualización de perfil para aceptar `IsEmergency24h`
  - Solo Admin puede marcar una clínica como emergency (o la clínica misma en su panel)

- [ ] Crear endpoint `GET /api/public/emergency-vets?lat={}&lng={}&radiusKm={}`
  - Returns: lista de clínicas 24h ordenadas por distancia, máximo 5 resultados
  - No requiere auth — pública para uso desde la app y desde WhatsApp bot
  - Rate limit: 30 req/min por IP

### 4.3 · Frontend

- [ ] Actualizar `frontend/src/features/map/pages/PublicMapPage.tsx`
  - Agregar toggle "🚨 Solo emergencias 24h" en los controles del mapa
  - Pines de emergency: color rojo con cruz blanca, z-index mayor

- [ ] Crear `frontend/src/features/lost-pets/components/EmergencyVetPanel.tsx`
  - Panel colapsable que aparece en `CaseRoomPage` y en `PetDetailPage` cuando pet está Lost
  - Muestra: hasta 3 clínicas de emergencia más cercanas (si hay ubicación del usuario)
  - CTA "Llamar ahora" → `tel:` link con el número de emergencia
  - Hardcoded: "Control de Intoxicaciones CICT: +506 2223-1028" como primer ítem siempre
  - Accesibilidad: `aria-label="Veterinarias de emergencia cercanas"`

- [ ] Crear hook `useEmergencyVets(lat, lng)` en `frontend/src/features/map/hooks/`
  - React Query, enabled solo cuando hay coordenadas disponibles

- [ ] Agregar campo `isEmergency24h` al formulario de perfil en `ClinicDashboardPage`

### 4.4 · Tests

- [ ] `PawTrack.UnitTests/Clinics/EmergencyVetQueryTests.cs`
  - Caso: 3 clínicas en radio → devuelve las 3 ordenadas por distancia
  - Caso: ninguna clínica de emergency en radio → lista vacía
- [ ] `frontend/tests/features/lost-pets/EmergencyVetPanel.test.tsx`
  - Render: siempre muestra CICT aunque no haya clínicas
  - Render: muestra clínicas de emergency con CTA de llamada

---

## FEATURE 5 — UX sin-login "Encontré una mascota" (Enhanced)

> **Objetivo:** Landing ultra-simplificada pública para el flujo de encontrar una mascota, sin fricción de registro, con AI matching en <3 segundos.

### 5.1 · Backend

- [ ] Crear endpoint público `POST /api/public/found-pet/quick-match`
  - No requiere autenticación
  - Rate limit: 10 req/min por IP (más estricto que el auth)
  - Acepta: `multipart/form-data` con `photo (IFormFile)` + `lat? (double)` + `lng? (double)`
  - Lógica: igual que `VisualMatchQuery` existente pero sin userId
  - Response: `QuickMatchResultDto { Matches: [...], SessionToken: string }` — SessionToken firmado (JWT 1h) para continuar el flujo sin crear cuenta
  - Limit: máximo 5 matches en la respuesta pública (vs 10 en auth)
  - GDPR/PII: la foto NO se guarda en este endpoint — solo el vector embedding en caché TTL 1h

- [ ] Agregar `SessionToken` validation a `POST /api/sightings` (alt path: permite crear avistamiento con SessionToken en lugar de JWT de usuario)
  - Si SessionToken válido → crear avistamiento con `IsAnonymous = true`, `ReporterSessionRef = hash(sessionToken)`

- [ ] Agregar rate limiting named policy `quick-match-public` en `Program.cs`

### 5.2 · Frontend — Nueva ruta pública

- [ ] Crear página `frontend/src/features/sightings/pages/QuickFoundPetPage.tsx`
  - Ruta pública: `/encontre` (agregar a `routes.tsx` en `PublicLayout`)
  - SEO: `<title>Encontré una mascota — PawTrack CR</title>` via `react-helmet-async`
  - `og:image`, `og:description` para compartibilidad en WhatsApp

- [ ] Diseño del wizard de 3 pasos:
  - **Paso 1 — Foto:**
    - Área de drop grande con ícono de cámara (mobile: activa cámara directamente)
    - Instrucción: "Toma o sube una foto clara de la mascota que encontraste"
    - Submit → spinner → resultados en <3 segundos
    - Timeout handling: si >8s → "La búsqueda está tardando más de lo normal…"

  - **Paso 2 — Resultados:**
    - Grid de matches con foto, nombre, especie, raza, % similaridad
    - Badge "Alta coincidencia" / "Posible coincidencia" según score
    - CTA primario en cada card: "¿Es esta mascota?" → abre diálogo de confirmación
    - CTA secundario: "No encontré ninguna — reportar igualmente"
    - Si 0 matches: "No encontramos una mascota perdida que coincida. Puedes reportar el avistamiento igualmente."

  - **Paso 3 — Contacto seguro:**
    - Si seleccionó match: formulario simplificado (ubicación + foto + nota libre)
    - Envía como avistamiento anónimo usando SessionToken
    - Confirmación: "¡Gracias! El dueño de {nombre} fue notificado. Se comunicarán contigo pronto."
    - CTA: "Crear cuenta para hacer seguimiento" (upsell suave)

- [ ] Crear hook `useQuickMatch` en `frontend/src/features/sightings/hooks/`
  - Mutación con SessionToken en respuesta guardado en `sessionStorage`
  - Reusa SessionToken para el paso 3

- [ ] Actualizar `PublicLayout.tsx` y navegación pública:
  - Agregar "Encontré una mascota" en el nav público y en el menú mobile
  - Agregar enlace desde `PublicPetProfilePage` cuando pet está Lost: "¿Eres tú quien encontró a esta mascota?"

- [ ] Agregar ruta `/encontre` en `staticwebapp.config.json` para Azure Static Web Apps routing

### 5.3 · SEO & Performance

- [ ] Agregar `<link rel="preload">` para el modelo de AI visual en el bundle del chunk de quick-match
- [ ] Lazy-load `QuickFoundPetPage` (ya es patrón en routes.tsx)

### 5.4 · Tests

- [ ] `PawTrack.IntegrationTests/Public/QuickMatchEndpointTests.cs`
  - Caso: foto válida → response con matches y SessionToken
  - Caso: sin foto → 400 Bad Request
  - Caso: rate limit excedido → 429 Too Many Requests
- [ ] `frontend/tests/features/sightings/QuickFoundPetPage.test.tsx`
  - Step 1: file upload activa mutación
  - Step 2: muestra matches con % de similaridad
  - Step 3: formulario de avistamiento con SessionToken

---

## FEATURE 6 — Neighbor Network Opt-In (Guardia Vecinal)

> **Objetivo:** Red voluntaria de vecinos verificados por número de teléfono CR que reciben alertas ultra-locales (500m) cuando una mascota se pierde en su cuadra.

### 6.1 · Domain

- [ ] Crear `backend/src/PawTrack.Domain/Locations/NeighborAlert.cs`
  - Props: `Id (Guid)`, `UserId (Guid)`, `RadiusMeters (int default 500)`, `IsActive (bool)`, `VerifiedPhone (string)`, `VerifiedAt (DateTimeOffset?)`, `EnrolledAt (DateTimeOffset)`
  - Invariant: `RadiusMeters` entre 100 y 2000
  - Factory: `NeighborAlert.Enroll(userId, verifiedPhone, radiusMeters)`
  - Método: `Verify()` → setea `VerifiedAt = now`
  - Método: `SetRadius(int meters)` → valida rango
  - Método: `Deactivate()` / `Activate()`

- [ ] Crear `backend/src/PawTrack.Domain/Locations/NeighborOtp.cs`
  - Props: `Id (Guid)`, `UserId (Guid)`, `Phone (string)`, `CodeHash (string)`, `ExpiresAt (DateTimeOffset)`, `IsUsed (bool)`
  - Factory: `NeighborOtp.Issue(userId, phone, code)` — hash el code internamente (SHA-256)
  - Método: `Verify(string code)` → compara hash, verifica no expirado ni usado → retorna `Result`
  - Expiración: 10 minutos

### 6.2 · Infrastructure

- [ ] Agregar `DbSet<NeighborAlert>` y `DbSet<NeighborOtp>` en `PawTrackDbContext`
  - Index espacial (si SQL Server lo soporta) o al menos `HasIndex(n => n.UserId)`
  - `NeighborOtp`: index `UserId + IsUsed`, TTL no nativo → limpiar con job

- [ ] Migración: `AddNeighborNetwork`

- [ ] Crear `INeighborAlertRepository` en `PawTrack.Domain/Locations/`
  - `GetActiveInRadiusAsync(double lat, double lng, int radiusMeters)` → lista de `NeighborAlert` con user emails para enviar notificación

- [ ] Implementar con query espacial Haversine en `PawTrack.Infrastructure/Locations/`

- [ ] `NeighborOtpCleanupService` (hosted): elimina OTPs expirados una vez al día

### 6.3 · Application

- [ ] Crear `backend/src/PawTrack.Application/Locations/NeighborNetworkCommands.cs`

  **Command:** `SendNeighborOtpCommand(Guid UserId, string Phone)`
  - Genera OTP 6 dígitos (CSPRNG)
  - Envía via WhatsApp API (ya existe `IWhatsAppSender`) o SMS como fallback
  - Mensaje: "Tu código PawTrack Guardia Vecinal es: {code}. Expira en 10 minutos."
  - Rate limit por usuario: max 3 OTPs por hora (guardar en Redis/Memory cache)
  - Validator: Phone formato CR (+506 o 8 dígitos), UserId no vacío

  **Command:** `VerifyNeighborPhoneCommand(Guid UserId, string Phone, string Code)`
  - Verifica OTP → si válido crea o actualiza `NeighborAlert` con IsActive=true
  - Validator: todos los campos requeridos, Code de 6 dígitos

  **Command:** `UpdateNeighborSettingsCommand(Guid UserId, int RadiusMeters, bool IsActive)`

  **Query:** `GetNeighborStatusQuery(Guid UserId)` → `NeighborStatusDto { IsEnrolled, IsActive, VerifiedPhone, RadiusMeters, NeighborsInRange: int }`

  **Query:** `GetNeighborCountInAreaQuery(double Lat, double Lng, int RadiusMeters)` → `int`
  - Usado en el flujo de reportar pérdida para mostrar "Hay X vecinos activos en tu área"

- [ ] Actualizar `LostPetCommands.cs` handler de `ReportLostCommand`:
  - Tras crear el reporte, publicar `LostPetReportedNotification` via MediatR
  - Notification handler: llama `GetActiveInRadiusAsync` con las coords del reporte → envía push/email a cada vecino activo

### 6.4 · API

- [ ] Crear `backend/src/PawTrack.API/Controllers/NeighborNetworkController.cs`
  - `POST /api/neighbor/otp` → `SendNeighborOtpCommand` (requiere auth)
  - `POST /api/neighbor/verify` → `VerifyNeighborPhoneCommand`
  - `PUT /api/neighbor/settings` → `UpdateNeighborSettingsCommand`
  - `GET /api/neighbor/status` → `GetNeighborStatusQuery`
  - `GET /api/public/neighbor-count?lat={}&lng={}&radius={}` → `GetNeighborCountInAreaQuery` (público, rate-limited)

### 6.5 · Frontend

- [ ] Crear `frontend/src/features/locations/components/NeighborNetworkSetup.tsx`
  - Wizard de 3 pasos en un `Drawer` (side=bottom):
    1. Explicación del beneficio + mapa de radio
    2. Input de teléfono CR → "Enviar código"
    3. Input OTP 6 dígitos con countdown de 10min
  - Usa `InputOtp` pattern: 6 inputs separados para mejor UX mobile
  - Tras verificación: badge "Vecino Activo ✅" + slider de radio

- [ ] Crear `frontend/src/features/locations/components/NeighborStatusCard.tsx`
  - Muestra: estado (activo/inactivo), radio configurado, conteo de vecinos en radio
  - CTA para ajustar radio (slider 100m–2000m)
  - Toggle de activación/desactivación

- [ ] Integrar en `frontend/src/features/auth/pages/ProfilePage.tsx`
  - Nueva sección "Guardia Vecinal" en el perfil
  - Badge "Vecino Activo" junto al nombre si está activo

- [ ] Integrar en `ReportLostPage.tsx` (Paso 1 del wizard):
  - Card informativa: "🏘️ Hay {count} vecinos activos en tu área. Serán notificados automáticamente."
  - Si 0 vecinos: "Sé el primero en activar la Guardia Vecinal en tu zona" con link al perfil

- [ ] Crear hooks `useNeighborStatus`, `useSendOtp`, `useVerifyPhone`, `useUpdateNeighborSettings`

### 6.6 · Tests

- [ ] `PawTrack.UnitTests/Locations/NeighborOtpTests.cs`
  - Caso: OTP válido verifica correctamente
  - Caso: OTP expirado → Result.Failure
  - Caso: OTP ya usado → Result.Failure

- [ ] `PawTrack.UnitTests/Locations/LostPetReportedNotificationHandlerTests.cs`
  - Caso: reporte creado → handler llama GetActiveInRadius y envía notificaciones

- [ ] `frontend/tests/features/locations/NeighborNetworkSetup.test.tsx`
  - Render: muestra los 3 pasos del wizard
  - Interaction: envío de OTP, input de código, confirmación

---

## FEATURE 7 — Activity Log Manual + Tractive Sync

> **Objetivo:** Registro de actividad física diaria con entrada manual y sincronización automática desde collar Tractive (si conectado), con benchmarks por raza.

### 7.1 · Domain

- [ ] Crear `backend/src/PawTrack.Domain/Medical/ActivityLog.cs`
  - Props: `Id (Guid)`, `PetId (Guid)`, `OwnerId (Guid)`, `Date (DateOnly)`, `Type (ActivityType)`, `DurationMinutes (int)`, `DistanceMeters (int?)`, `Notes (string?)`, `Source (ActivitySource)`, `CreatedAt (DateTimeOffset)`
  - `ActivityType` enum: `Walk = 0, Run = 1, Play = 2, Swim = 3, Training = 4, Other = 5`
  - `ActivitySource` enum: `Manual = 0, Tractive = 1, Collar = 2`
  - Factory: `ActivityLog.Record(petId, ownerId, date, type, durationMinutes, distanceMeters?, notes?, source)`
  - Invariant: `DurationMinutes > 0`, `DistanceMeters >= 0`, `Date <= DateOnly.Today`

- [ ] Crear `backend/src/PawTrack.Domain/Medical/BreedActivityBenchmark.cs` (clase estática)
  - `static Dictionary<string, ActivityBenchmark> Benchmarks` con 30+ razas
  - `ActivityBenchmark`: `{ DailyMinutesMin, DailyMinutesMax, DailyKmMin, DailyKmMax, EnergyLevel: "low"|"medium"|"high" }`
  - Fallback por especie si raza no mapeada
  - Data: basada en estándares AKC/veterinarios reconocidos

### 7.2 · Infrastructure

- [ ] Agregar `DbSet<ActivityLog>` en `PawTrackDbContext`
  - Index: `(PetId, Date)` para queries por rango de fechas
  - Index: `(PetId, Source, Date)` para deduplicación de Tractive sync

- [ ] Migración: `AddActivityLog`

- [ ] Crear `IActivityLogRepository` interface + implementación:
  - `GetByPetAndDateRangeAsync(petId, from, to)` → `IReadOnlyList<ActivityLog>`
  - `GetWeeklySummaryAsync(petId, weekStart)` → `ActivityWeeklySummary`
  - `ExistsBySourceAndDateAsync(petId, source, date)` → `bool` (para dedup Tractive)

- [ ] Actualizar `CollarSyncJob` / `TractivePollingService` en `PawTrack.Infrastructure/Collars/`:
  - Al sincronizar posición del collar → si hay MovementData disponible → crear `ActivityLog` con `Source = Tractive`
  - Dedup: verificar `ExistsBySourceAndDateAsync` antes de insertar
  - Parsear distancia diaria desde Tractive API response (campo `distance_meters` si existe)

### 7.3 · Application

- [ ] Crear `backend/src/PawTrack.Application/Medical/ActivityCommands.cs`

  **Command:** `LogActivityCommand(Guid PetId, Guid OwnerId, DateOnly Date, ActivityType Type, int DurationMinutes, int? DistanceMeters, string? Notes)`
  - Validator: PetId, OwnerId requeridos; DurationMinutes 1–1440; Date no futura; Notes max 500 chars

  **Command:** `DeleteActivityLogCommand(Guid ActivityId, Guid OwnerId)`
  - Verifica ownership antes de eliminar

  **Query:** `GetActivityLogsQuery(Guid PetId, DateOnly From, DateOnly To)`
  - Returns: `ActivitySummaryDto { Logs: [...], WeeklyTotals: [...], Benchmark: BreedBenchmarkDto?, StreakDays: int }`
  - `StreakDays`: número de días consecutivos con al menos 1 log

  **Query:** `GetActivityStreakQuery(Guid PetId)` → `ActivityStreakDto { Current: int, Best: int, LastLogDate: DateOnly? }`

### 7.4 · API

- [ ] Crear `backend/src/PawTrack.API/Controllers/ActivityController.cs`
  - `POST /api/pets/{petId}/activity` → `LogActivityCommand` (requiere auth, plan Plus)
  - `DELETE /api/pets/{petId}/activity/{activityId}` → `DeleteActivityLogCommand`
  - `GET /api/pets/{petId}/activity?from={}&to={}` → `GetActivityLogsQuery`
  - `GET /api/pets/{petId}/activity/streak` → `GetActivityStreakQuery`
  - Plan gate: retornar 403 si plan Free

### 7.5 · Frontend

- [ ] Crear `frontend/src/features/medical/api/activityApi.ts`
  - Tipos: `ActivityLogDto`, `ActivityType`, `ActivitySummaryDto`, `ActivityStreakDto`

- [ ] Crear `frontend/src/features/medical/hooks/useActivity.ts`
  - `useActivityLogs(petId, from, to)` → React Query
  - `useActivityStreak(petId)` → React Query
  - `useLogActivity(petId)` → mutation
  - `useDeleteActivity(petId)` → mutation

- [ ] Crear `frontend/src/features/medical/components/ActivityTab.tsx`
  - **Header:** streak counter con fuego 🔥 + "X días consecutivos registrando actividad"
  - **Benchmark card:** "Tu {raza} necesita {min}–{max} min/día · Esta semana: {total} min"
    - Barra de progreso color-coded (rojo/amber/verde según % del objetivo)
  - **Weekly chart:** Recharts `BarChart` con 7 barras (lun–dom), colores por ActivityType
  - **Quick log form:** inline, minimalista:
    - Tipo (iconos seleccionables): 🦮 Paseo · 🏃 Carrera · 🎾 Juego · 🏊 Natación
    - Duración: slider con pasos de 5 min (5–120 min) + input manual
    - Distancia: opcional, input km con helper "¿Cuántos km caminaste?"
    - Fecha: default hoy, puede cambiar
    - CTA: "+ Registrar"
  - **Lista del mes:** agrupada por semana, ícono de actividad + duración + distancia
  - **Tractive sync badge:** si hay collar Tractive conectado → "📡 Sincronizando desde Tractive" en los logs auto-importados
  - PlanGate: muestra upsell si plan Free

- [ ] Integrar `ActivityTab` como nueva tab en `PetDetailPage.tsx`
  - Label: "Actividad" con ícono de rayo/zapatilla
  - Orden: Info · GPS · Expediente · Actividad · Escaneos

### 7.6 · Gamification

- [ ] Agregar evento al `IncentiveSystem` cuando streak alcanza 7, 30, 100 días
  - Badge: "🏃 Atleta Activo" (7 días), "🥇 Maestro del Movimiento" (30 días)
  - Publicar `IncentiveEarnedNotification` via MediatR

### 7.7 · Tests

- [ ] `PawTrack.UnitTests/Medical/LogActivityCommandHandlerTests.cs`
  - Caso: log válido → se persiste con campos correctos
  - Caso: duración 0 → ValidationException
  - Caso: fecha futura → ValidationException

- [ ] `PawTrack.UnitTests/Medical/GetActivityLogsQueryHandlerTests.cs`
  - Caso: 7 logs en una semana → WeeklyTotals correcto
  - Caso: 5 días consecutivos → StreakDays = 5

- [ ] `frontend/tests/features/medical/ActivityTab.test.tsx`
  - Render: muestra streak y benchmark
  - Interaction: quick log form → llama mutación

---

## FEATURE 8 — Annual Pet Health Report PDF

> **Objetivo:** Reporte anual autogenerado tipo "Year in Review" que agrega todas las métricas del pet en un año calendario — descargable y compartible.

### 8.1 · Application

- [ ] Crear `backend/src/PawTrack.Application/Medical/AnnualReportCommands.cs`

  **Query:** `GenerateAnnualReportQuery(Guid PetId, Guid RequestingUserId, int Year)`
  - Validator: Year entre 2024 y año actual; PetId y UserId requeridos
  - Verifica ownership del pet (o membership familiar)
  - Plan gate: solo Familia (retornar `Result.Failure("plan_required")` si no)
  - Agrega:
    - `PetInfo`: nombre, especie, raza, edad calculada al fin del año
    - `VetVisits`: count total, lista de fechas y clínicas
    - `VaccinesApplied`: lista de vacunas aplicadas en el año
    - `WeightTimeline`: primer y último peso del año + delta en %
    - `ScanHistory`: total escaneos del año, mapa de calor por mes
    - `ActivitySummary`: total minutos activos, total km (si GPS o logs manuales), día más activo
    - `LostEvents`: si hubo eventos de pérdida → fecha de pérdida, días perdido, fecha de reunificación
    - `RemindersCompleted`: conteo de recordatorios veterinarios marcados como completos
    - `HealthScore`: score promedio del año vs score actual

### 8.2 · Infrastructure / PDF

- [ ] Crear `AnnualReportPdfService.cs` en `PawTrack.Infrastructure/Medical/`
  - Usa QuestPDF (ya en el proyecto desde `CertificatesController`)
  - Template diseño visual:
    - **Portada:** Foto del pet (circular), nombre, año, logo PawTrack
    - **Sección 1 — Identidad:** especie, raza, microchip, QR del perfil
    - **Sección 2 — Salud preventiva:** tabla de vacunas/tratamientos con fechas
    - **Sección 3 — Peso:** mini chart con valores mensiles (QuestPDF charts)
    - **Sección 4 — Actividad:** total km, días activos, logros (si aplica)
    - **Sección 5 — Comunidad:** total escaneos de QR, avistamientos recibidos
    - **Sección 6 — Eventos de pérdida:** (omitir si no hubo ninguno)
    - **Contraportada:** "Generado por PawTrack CR · {fecha}" + QR de verificación
  - Watermark ligero en páginas internas si plan no es Familia
  - Idioma: español (es-CR)

- [ ] Agregar endpoint `GET /api/pets/{petId}/annual-report?year={year}` en `MedicalController.cs`
  - Retorna `application/pdf` con `Content-Disposition: attachment; filename="pawtrack-{petName}-{year}.pdf"`
  - Timeout: 30s máximo (los PDF pueden tardar)
  - Cache: por (petId, year) con TTL 1 hora (Content-Addressable Storage en Blob)

### 8.3 · Frontend

- [ ] Crear `frontend/src/features/medical/components/AnnualReportButton.tsx`
  - Dropdown selector de año (años disponibles desde creación del pet hasta año actual)
  - Muestra preview modal antes de generar:
    - Card con íconos representando las secciones disponibles
    - "Este reporte incluye: X visitas veterinarias, X vacunas, X km de actividad…"
    - CTA "Descargar PDF" → llama al endpoint y descarga automáticamente
    - CTA "Compartir imagen" → genera imagen de preview via `html2canvas` para compartir en WhatsApp/Instagram (solo la portada)
  - Loading state: spinner con "Generando tu informe anual… (puede tomar unos segundos)"
  - PlanGate: si no es Familia → muestra upsell en lugar del botón

- [ ] Integrar en `PetDetailPage.tsx` — botón en el header del pet (junto a "Exportar PDF" del expediente)
- [ ] Crear hook `useGenerateAnnualReport(petId)` → descarga directa via `axios responseType: 'blob'`

### 8.4 · Tests

- [ ] `PawTrack.UnitTests/Medical/GenerateAnnualReportQueryHandlerTests.cs`
  - Caso: pet con datos completos del año → DTO correcto
  - Caso: pet sin actividad ese año → ActivitySummary con zeros
  - Caso: plan no Familia → Result.Failure

- [ ] `frontend/tests/features/medical/AnnualReportButton.test.tsx`
  - Render: muestra selector de año con años correctos
  - Interaction: click en "Descargar" → llama endpoint y descarga

---

## FEATURE 9 — Vaccine Passport OIRSA/SENASA

> **Objetivo:** Certificado de salud estandarizado en formato compatible con requisitos de viaje internacional y nacional de Costa Rica, emitido solo por clínicas Partner.

### 9.1 · Domain

- [ ] Crear `backend/src/PawTrack.Domain/Certificates/VaccinePassport.cs`
  - Props: `Id (Guid)`, `PetId (Guid)`, `IssuingClinicId (Guid)`, `IssuingVetName (string)`, `IssuingVetLicense (string)`, `IssuedAt (DateTimeOffset)`, `ValidUntil (DateOnly)`, `VerificationCode (string)`, `RabiesVaccineDate (DateOnly?)`, `RabiesVaccineBrand (string?)`, `RabiesVaccineLotNumber (string?)`, `OtherVaccines: List<PassportVaccineEntry>`, `ParasiteControl: PassportParasiteEntry?`, `MicrochipNumber (string?)`, `IsoFormat (string default "OIRSA-CR-2025")`
  - `PassportVaccineEntry`: `{ Name, Date, Brand, LotNumber, ValidUntil }`
  - `PassportParasiteEntry`: `{ ProductName, ApplicationDate, NextDueDate }`
  - Factory: `VaccinePassport.Issue(petId, clinicId, vetName, vetLicense, ...)`
  - `VerificationCode`: 8 caracteres alfanuméricos CSPRNG + timestamp hash (similar a cómo se hace en `PromotionCode`)

- [ ] Crear `IVaccinePassportRepository` interface + implementación

### 9.2 · Infrastructure / PDF

- [ ] Crear migración: `AddVaccinePassport`
  - `DbSet<VaccinePassport>` + `DbSet<PassportVaccineEntry>` (owned collection o tabla separada)
  - Index: `(PetId, IssuedAt)`, `(VerificationCode)` — unique

- [ ] Crear `VaccinePassportPdfService.cs` en `PawTrack.Infrastructure/Certificates/`
  - Template QuestPDF siguiendo el formato del CERTIFICADO DE SALUD OIRSA:
    - Header: "CERTIFICADO DE SALUD / HEALTH CERTIFICATE" bilingüe
    - Sección I: Identificación del animal (nombre, especie, raza, color, microchip, edad)
    - Sección II: Propietario (nombre, dirección — de los datos del user)
    - Sección III: Vacunaciones (tabla con fecha, producto, lote, próxima dosis)
    - Sección IV: Tratamientos antiparasitarios
    - Sección V: Clínica emisora + firma digital + SENASA license
    - Footer: QR de verificación pública `pawtrack.cr/verificar/{verificationCode}`
    - Watermark: "Emitido por PawTrack CR — Verificable en línea"

### 9.3 · Application

- [ ] Crear `backend/src/PawTrack.Application/Certificates/VaccinePassportCommands.cs`

  **Command:** `IssueVaccinePassportCommand(Guid PetId, Guid IssuingClinicId, string VetName, string VetLicense, ...datos de vacunas y parásitos...)`
  - Solo accesible para Clinic con plan Partner
  - Verifica que el pet tenga `ClinicMedicalAccessGrant` activo para la clínica
  - Pre-llena datos de vacunas desde los `MedicalRecord` del pet (tipo Vaccine del año anterior)
  - El vet puede editar/agregar antes de emitir
  - Validator: rabies required para perros, VetLicense formato SENASA

  **Query:** `GetVaccinePassportsQuery(Guid PetId, Guid RequestingUserId)` → lista de pasaportes emitidos

  **Query:** `VerifyVaccinePassportQuery(string VerificationCode)` → datos públicos del pasaporte (nombre del pet, clínica, fecha, vigencia)
  - Esta query es pública — usada en la landing de verificación

- [ ] Agregar endpoint de verificación pública en `CertificatesController.cs`:
  - `GET /api/public/passport/{verificationCode}` → `VerifyVaccinePassportQuery`
  - `GET /api/pets/{petId}/passport/{passportId}/download` → PDF download (requiere ownership o familia)
  - `POST /api/clinics/passport` → `IssueVaccinePassportCommand` (requiere rol Clinic + plan Partner)

### 9.4 · Frontend

- [ ] Crear `frontend/src/features/clinics/components/VaccinePassportIssuer.tsx`
  - Formulario para emitir desde el portal de clínica Partner
  - Pre-llena vacunas desde el expediente del paciente
  - Campos editables: VetName, VetLicense, fechas, productos, lotes
  - Preview del PDF antes de emitir (iframe con `/download` endpoint)
  - CTA "Emitir y descargar"

- [ ] Crear página pública `frontend/src/features/clinics/pages/PassportVerificationPage.tsx`
  - Ruta: `/verificar/pasaporte/:code` (pública)
  - Muestra: nombre del pet, clínica, fecha de emisión, vacunas, QR de la clínica
  - Badge "Verificado ✅" o "Expirado" o "No encontrado"
  - CTA: "Ver perfil público de {pet}" (link al perfil)

- [ ] Integrar en `MedicalHistoryTab.tsx` (plan Familia):
  - Lista de pasaportes emitidos para la mascota
  - Botón "Descargar" por cada pasaporte
  - "Solicitar a tu veterinaria" si no hay pasaportes (con info de qué clínicas son Partner)

- [ ] Agregar ruta `/verificar/pasaporte/:code` en `routes.tsx` bajo `PublicLayout`

### 9.5 · Tests

- [ ] `PawTrack.UnitTests/Certificates/IssueVaccinePassportCommandHandlerTests.cs`
  - Caso: clínica Partner con grant activo → pasaporte emitido con VerificationCode único
  - Caso: clínica no Partner → 403 Forbidden (Result.Failure)
  - Caso: sin access grant → Result.Failure

- [ ] `PawTrack.UnitTests/Certificates/VerifyVaccinePassportQueryHandlerTests.cs`
  - Caso: código válido → datos del pasaporte
  - Caso: código inexistente → Result.NotFound

- [ ] `frontend/tests/features/clinics/PassportVerificationPage.test.tsx`
  - Render: muestra badge "Verificado" para código válido
  - Render: muestra "No encontrado" para código inválido

---

## TAREAS TRANSVERSALES (aplican a todos los features)

### Migrations & DB

- [ ] Ejecutar todas las migraciones en orden en LocalDB de prueba antes de commit:
  ```
  AddHealthProtocols
  AddActivityLog
  AddNeighborNetwork
  AddBundleProductType
  AddClinicEmergencyFields
  AddVaccinePassport
  ```
- [ ] Verificar que seed data de `HealthProtocol` y `BreedActivityBenchmark` se aplica correctamente

### Plan Gating

- [ ] Auditar todos los nuevos endpoints: cada feature tiene el plan correcto en el gate
  - Health Alerts: todos los planes (Free también recibe alertas, sin score)
  - Weight Chart: Familia only
  - Activity Log: Plus y Familia
  - Annual Report: Familia only
  - Vaccine Passport: emitir → Clinic Partner; ver → Familia; verificar → público
  - Emergency Vet: todos los planes (público)
  - Quick Match: público (sin login)
  - Neighbor Network: todos los planes

### Subscription Gating (Frontend)

- [ ] Agregar valores al enum de plan checks en `useMyTier.ts` si es necesario
- [ ] Verificar que `PlanGate` component muestra el upsell correcto para cada feature

### Notification Types

- [ ] Agregar al enum `NotificationType` los nuevos tipos:
  - `HealthAlert`, `NeighborLostPetAlert`, `ActivityStreak`, `AnnualReportReady`
- [ ] Agregar templates de push notification para cada nuevo tipo en `NotificationService`

### Tests de Integración

- [ ] `PawTrack.IntegrationTests/Medical/HealthAlertsEndpointTests.cs`
- [ ] `PawTrack.IntegrationTests/Locations/NeighborNetworkEndpointTests.cs`
- [ ] `PawTrack.IntegrationTests/Medical/ActivityEndpointTests.cs`
- [ ] `PawTrack.IntegrationTests/Certificates/VaccinePassportEndpointTests.cs`

### Frontend — Package Updates

- [ ] `npm install recharts` (para Weight Chart y Activity Chart)
- [ ] Verificar que Recharts no rompe el bundle size (tree-shake con imports específicos)
- [ ] Actualizar `vite.config.ts` si es necesario para optimizar chunks de recharts

### Documentación

- [ ] Actualizar `docs/MANUAL_USUARIO.md` con secciones de Activity Log, Health Alerts, Annual Report
- [ ] Actualizar `docs/MANUAL_CLINICAS.md` con sección de Vaccine Passport (emisión)
- [ ] Actualizar `docs/MANUAL_TECNICO.md` con nuevos módulos y endpoints
- [ ] Actualizar `README.md` tabla de Feature Overview con los nuevos módulos
- [ ] Actualizar `docs/planes.md` con las nuevas features por tier

### Deployment

- [ ] Actualizar `infra/main.bicep` si hay nuevas variables de entorno necesarias
- [ ] Agregar cualquier nuevo secret a Azure Key Vault
- [ ] Verificar que el nuevo `recharts` no exceda los límites de bundle size (< 500KB gzip por chunk)

---

## ORDEN DE IMPLEMENTACIÓN SUGERIDO

```
Día 1:     Feature 2 (Weight Chart — sin backend nuevo, recharts)
Día 2:     Feature 3 (NFC Bundle — mínimo cambio) + Feature 4 (Emergency 24h)
Días 3-5:  Feature 1 (Health Reminders — Domain + Infra + App + API + UI)
Días 6-8:  Feature 5 (Quick Found Pet — backend + frontend público)
Día 9:     Tareas transversales: migrations, plan gates, notification types, tests
Días 10-15: Buffer + polish + integration tests Sprint Next

Días 16-19: Feature 7 Activity Log — Domain + Infra
Días 20-22: Feature 7 Activity Log — Application + API + Frontend
Días 23-26: Feature 6 Neighbor Network — Domain + Infra + Application
Días 27-30: Feature 6 Neighbor Network — API + Frontend
Días 31-34: Feature 8 Annual Report PDF
Días 35-38: Feature 9 Vaccine Passport
Días 39-45: Buffer + integration tests Sprint +2 + documentación + deployment
```

---

_Documento generado: 2026-08-11 | PawTrack CR Enterprise Sprint Plan_
