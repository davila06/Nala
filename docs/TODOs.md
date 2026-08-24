# PawTrack CR — TODOs y Mejoras Enterprise

> **Fecha:** 2026-08-24 | Análisis exhaustivo del codebase completo  
> **Metodología:** grep de TODOs/STUBs/`.Result`/`.Wait()`/anti-patrones + revisión manual de arquitectura  
> **Prioridad:** 🔴 Crítico · 🟡 Alto · 🟢 Medio · ⚪ Bajo/Futuro

---

## 1. STUBs — Código que no hace nada en producción

### 1.1 Facebook Broadcast Channel
- [ ] **🔴** `Infrastructure/Broadcast/Channels/FacebookChannelBroadcaster.cs` siempre retorna `null` y solo loguea
  - **Implementación:** `POST https://graph.facebook.com/v19.0/{pageId}/feed` con `{ message: "...", link: trackingUrl }`
  - Requiere: `Broadcast:Facebook:PageAccessToken` y `Broadcast:Facebook:PageId` en Key Vault
  - Añadir a `IHttpClientFactory` con nombre `"Facebook"` y retry policy (Polly)
  - El `BroadcastAttempt` ya se persiste — solo falta la llamada HTTP real
  - Archivos: `FacebookChannelBroadcaster.cs`, `InfrastructureServiceCollectionExtensions.cs`

### 1.2 Telegram Broadcast Channel
- [ ] **🔴** `Infrastructure/Broadcast/Channels/TelegramChannelBroadcaster.cs` mismo problema
  - **Implementación:** `POST https://api.telegram.org/bot{token}/sendMessage` con `{ chat_id, text, parse_mode: "HTML" }`
  - Requiere: `Broadcast:Telegram:BotToken` y `Broadcast:Telegram:ChatId` en Key Vault
  - El canal ya tiene interface y registro DI — solo falta la llamada HTTP
  - Añadir retry con Polly (transient HTTP errors)

### 1.3 Kippy GPS Integration
- [ ] **🟡** `Domain/Collars/CollarProvider.cs` tiene `Kippy = 2` pero no hay `KippyService`
  - **Implementación:**
    1. Crear `KippyService.cs` implementando `ITrackerService` (misma interface que Tractive)
    2. Crear `KippyPollingJob.cs` similar a `TractivePollingJob.cs` con `PeriodicTimer(5 min)`
    3. OAuth2 con la API de Kippy (docs: https://www.kippy.eu/developers)
    4. Registrar en DI con HttpClient named `"Kippy"`
  - Sin Kippy, el enum value es dead code que confunde

---

## 2. Anti-patrones de async/concurrencia

### 2.1 `.Result` después de `Task.WhenAll`
- [ ] **🟡** `Application/Fosters/Commands/CloseCustody/CloseCustodyCommand.cs` líneas 57-59:
  ```csharp
  // ACTUAL — anti-patrón (safe solo porque ya fue awaited, pero confuso)
  var fosterUser = fosterUserTask.Result;
  var ownerUser  = ownerUserTask.Result;
  var pet        = petTask.Result;

  // ENTERPRISE — desempaquetar con tuple deconstruction
  var (fosterUser, ownerUser, pet) = (await fosterUserTask, await ownerUserTask, await petTask);
  // O mejor aún, después de WhenAll:
  var fosterUser = await fosterUserTask;
  ```
  - Archivos: `CloseCustodyCommand.cs`, `GetCaseRoomQuery.cs` (línea 166)

### 2.2 Background Services con `Task.Delay` loop (drift)
- [ ] **🟢** Los siguientes servicios usan `while(!ct) { await Task.Delay(...) }` en lugar de `PeriodicTimer`:
  - `HealthAlertHostedService.cs` → usar `PeriodicTimer` con cron-like scheduling
  - `VetReminderHostedService.cs` → ídem
  - `QrScanRetentionHostedService.cs` → ídem
  - `StaleReportCheckerHostedService.cs` → ídem
  - `EmbeddingRefreshHostedService.cs` → ídem
  - **Implementación correcta (patrón ya usado en `RevokedTokenCleanupJob`):**
    ```csharp
    using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
    while (await timer.WaitForNextTickAsync(stoppingToken))
    {
        await DoWorkAsync(stoppingToken);
    }
    ```
  - `PeriodicTimer` no acumula drift, no puede overlappear ticks

### 2.3 Background Services en múltiples instancias (duplicated jobs)
- [ ] **🟡** Todos los `BackgroundService` se ejecutan en **cada instancia** del Container App en scale-out
  - Los jobs diarios (VetReminder, HealthAlert, QrRetention, StaleReport) se ejecutarán N veces cuando hay N instancias
  - **Solución enterprise:** SQL-based distributed lock antes de ejecutar:
    ```csharp
    // En cada BackgroundService antes de ejecutar el trabajo:
    await using var lease = await distributedLock.AcquireAsync($"job:{nameof(VetReminderHostedService)}", TimeSpan.FromHours(1), ct);
    if (!lease.Acquired) return; // otra instancia ya lo está corriendo
    ```
  - Alternativamente: usar Azure Container Apps Jobs (run-to-completion) en lugar de hosted services
  - Afecta: todos los `BackgroundService` del proyecto

---

## 3. Escalabilidad horizontal (scale-out)

### 3.1 TypingStateService — in-memory, no funciona con múltiples instancias
- [ ] **🟡** `API/Services/TypingStateService.cs` — documentado internamente como limitación
  - **Solución A (recomendada):** Azure SignalR Service como backplane
    ```csharp
    builder.Services.AddSignalR().AddAzureSignalR(configuration["AzureSignalR:ConnectionString"]);
    ```
    Con esto, los grupos de SignalR se sincronizan automáticamente entre instancias
  - **Solución B:** Redis Pub/Sub para broadcast de "is typing" entre instancias
  - **Solución C (mínimo esfuerzo):** mover el estado de typing a un canal SignalR broadcast — el cliente ya tiene el canal

### 3.2 SearchCoordinationHub — `_lastLocationUpdate` estático entre instancias
- [ ] **🟡** `API/Hubs/SearchCoordinationHub.cs` línea 25: `private static readonly ConcurrentDictionary`
  - El throttle de ubicación es por conexión, pero si la conexión migra de instancia, el throttle se pierde
  - **Solución:** mover el throttle al cliente (client-side debounce de 2s antes de enviar) o Redis

### 3.3 MemoryCacheNotificationRateLimitService — no distribuido
- [ ] **🟡** `Infrastructure/Notifications/MemoryCacheNotificationRateLimitService.cs` usa `IMemoryCache`
  - En scale-out, cada instancia tiene su propio cache → usuario puede recibir N notificaciones (una por instancia)
  - **Solución:** `IDistributedCache` (Redis o Azure Cache for Redis):
    ```csharp
    // Reemplazar IMemoryCache con IDistributedCache
    // Key: $"notif-rl:{userId}:{alertType}"
    // TTL: ventana de rate limit
    ```

---

## 4. Paginación ineficiente

### 4.1 GetMyStoreOrders — paginación en memoria
- [ ] **🔴** `Application/Stores/StoreOrderCommands.cs` línea 250:
  ```csharp
  // ACTUAL — carga TODOS los pedidos del cliente en memoria, luego pagina
  var orders = await repo.GetByCustomerAsync(request.CustomerId, ct); // ← potencialmente N
  var paged = orders.Skip((page - 1) * pageSize).Take(pageSize).ToList();
  
  // ENTERPRISE — paginación en base de datos
  ```
  - Crear `GetByCustomerPagedAsync(Guid customerId, int skip, int take, ct)` en `IStoreOrderRepository`
  - Añadir `CountByCustomerAsync(Guid customerId, ct)` para `totalCount`
  - Retornar `PagedResult<StoreOrderDto>` en lugar de `IReadOnlyList`
  - Archivos: `IStoreOrderRepository.cs`, `StoreOrderRepository.cs`, `StoreOrderCommands.cs`, `storeOrdersApi.ts`

### 4.2 Skip/Take degrada en tablas grandes
- [ ] **🟢** Para `Notifications`, `QrScanEvents`, y tablas con alto volumen, `OFFSET/FETCH` (Skip/Take) tiene O(offset) performance en SQL Server
  - **Solución enterprise:** cursor-based pagination usando `WHERE Id > @lastId ORDER BY Id`
  - Candidatos: `NotificationRepository.GetPagedWithCountsAsync`, `QrScanHistoryQuery`
  - Implementar `CursorPagedResult<T, TCursor>` genérico

---

## 5. Domain Events sin dispatch

### 5.1 Domain events definidos pero ignorados
- [ ] **🟢** Los siguientes agregados tienen `_domainEvents` pero todos están con `.Ignore()` en EF Core:
  - `Pet.cs` → `PetCreatedDomainEvent`, `PetReactivatedDomainEvent`
  - `LostPetEvent.cs` → `LostPetReportedDomainEvent`, `PetReunitedDomainEvent`
  - `Sighting.cs` → `SightingReportedDomainEvent`
  - `FoundPetReport.cs` → domain events
  - **Los eventos existen pero no se publican a ningún handler**
  - **Solución enterprise:** publicar eventos en `PawTrackDbContext.SaveChangesAsync`:
    ```csharp
    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var events = ChangeTracker.Entries<IHasDomainEvents>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();
        var result = await base.SaveChangesAsync(ct);
        foreach (var evt in events)
            await mediator.Publish(evt, ct);
        return result;
    }
    ```
  - Esto permite handlers desacoplados sin modificar los comandos existentes

---

## 6. Confiabilidad de notificaciones (outbox pattern)

### 6.1 Fire-and-forget con riesgo de pérdida
- [ ] **🟡** Todos los handlers usan el patrón:
  ```csharp
  await unitOfWork.SaveChangesAsync(ct); // ← commit
  _ = notificationDispatcher.DispatchX(...) // ← si el proceso muere aquí, la notif se pierde
      .ContinueWith(t => logger.LogWarning(...));
  ```
  - Si el Container App se reinicia entre el commit y el dispatch, la notificación se pierde permanentemente
  - **Solución enterprise (Transactional Outbox):**
    1. Crear tabla `OutboxMessages(Id, Type, Payload, CreatedAt, ProcessedAt?)`
    2. En `SaveChangesAsync`, serializar eventos a outbox en la misma transacción
    3. `OutboxProcessor` BackgroundService procesa y borra mensajes procesados
  - Esto garantiza at-least-once delivery sin transacciones distribuidas
  - Librerías: MassTransit Outbox, o implementación propia simple

---

## 7. Retry policies para HTTP clients

### 7.1 Sin Polly en clientes externos
- [ ] **🟡** Los siguientes `HttpClient` no tienen retry policy:
  - `"MetaWhatsApp"` — mensajes de WhatsApp se pierden en errores transitorios
  - `"AzureVision"` — embeddings fallan silenciosamente
  - `"AzureMaps"` — geocoding falla sin retry
  - `"PushProvider"` — push notifications se pierden
  - `"Facebook"` (cuando se implemente)
  - `"Telegram"` (cuando se implemente)
  - **Implementación con Microsoft.Extensions.Http.Resilience (.NET 8):**
    ```csharp
    services.AddHttpClient("MetaWhatsApp")
        .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(15))
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
        });
    ```
  - No usar Polly directamente — `Microsoft.Extensions.Http.Resilience` es el estándar en .NET 8+

### 7.2 SendGrid sin retry
- [ ] **🟡** `Infrastructure/Notifications/EmailSender.cs` — la llamada a SendGrid no tiene retry
  - Un 429 (rate limit) o 503 transitorio de SendGrid silenciosamente descarta el email
  - **Solución:** envolver la llamada `SendAsync` con retry exponencial (3 intentos, backoff 1s, 2s, 4s)
  - Alternativa: usar SendGrid's built-in retry (v3 SDK tiene `ISendGridClient` con retry configurado via `HttpClient`)

---

## 8. Seguridad — gaps residuales

### 8.1 Race condition en idempotency del bot de WhatsApp
- [ ] **🟡** `HandleWhatsAppWebhookCommandHandler.cs` línea 61:
  ```csharp
  if (session.IsMessageProcessed(request.MessageId)) return; // check
  session.MarkMessageProcessed(request.MessageId);           // set
  await unitOfWork.SaveChangesAsync(ct);                     // commit
  ```
  - Si dos instancias reciben el mismo webhook simultáneamente, ambas pasan el check antes de que alguna haga commit
  - **Solución:** añadir índice único en `BotSession.ProcessedMessageIds` o usar `UPDLOCK` en SQL:
    ```sql
    -- O más simple: tabla separada WhatsAppIdempotencyKey(MessageId PRIMARY KEY) 
    -- con INSERT que falla en duplicado
    ```

### 8.2 GeofencedAlertLog usa `Guid.NewGuid()` en lugar de `Guid.CreateVersion7()`
- [ ] **⚪** `Domain/Locations/GeofencedAlertLog.cs` línea 26 — inconsistencia menor con el resto del codebase
  - **Fix:** cambiar `Guid.NewGuid()` → `Guid.CreateVersion7()`
  - Guid v7 es sortable por tiempo — mejor para índices clustered en SQL Server

### 8.3 Adoption photo blobs no se borran al remover animal
- [ ] **🟡** `AdminModerateAnimalCommand` y `AdoptablePet.Remove()` cambian el status pero no borran los blobs
  - Los blobs `adoption-photos/{animalId}/*` quedan en Azure Storage indefinidamente
  - **Solución:** en `AdminModerateAnimalCommandHandler`, si `action == "remove"`:
    ```csharp
    foreach (var url in animal.PhotoUrls)
        await blobStorage.DeleteAsync(url, ct);
    ```
  - También aplicar al `DeleteAdoptionPhotoCommand` cuando se borra el animal completo

---

## 9. Shelter Publish — UX incompleta

### 9.1 Fotos no se pueden subir durante la publicación
- [ ] **🟡** `ShelterPublishPage.tsx` — después de publicar, muestra toast "Ahora puedes subir fotos" y redirige al dashboard
  - El usuario tiene que navegar manualmente al dashboard para encontrar el animal y subir fotos
  - **Enterprise UX:** redirigir directamente al animal recién creado con el formulario de fotos abierto, o añadir paso 3 de fotos dentro del mismo flujo de publicación:
    ```tsx
    // Después del publish exitoso:
    const animalId = result.id;
    // Mostrar DropZone inline con uploadPhoto mutation
    // Redirect a /adopciones/{animalId} solo cuando el usuario termina
    ```

---

## 10. Audit log para acciones admin

### 10.1 Sin trazabilidad de acciones administrativas
- [ ] **🟡** No existe registro de quién aprobó un aliado, quién activó una suscripción, quién removió un animal
  - **Implementación:** tabla `AuditLog(Id, AdminUserId, Action, EntityType, EntityId, OldValue, NewValue, PerformedAt)`
  - Poblarla en los handlers de admin (`ReviewAllyApplicationCommandHandler`, `AdminActivateSubscriptionCommandHandler`, `AdminModerateAnimalCommandHandler`, etc.)
  - Exponer endpoint `GET /api/admin/audit?entityType=X&entityId=Y` para investigación de incidentes

---

## 11. Frontend — mejoras de calidad

### 11.1 `any` cast en WeightTrendChart
- [ ] **🟢** `features/medical/components/WeightTrendChart.tsx` línea 23:
  ```typescript
  // ACTUAL
  const { active, payload } = props as any;
  
  // ENTERPRISE — tipo correcto de recharts
  import type { TooltipProps } from "recharts";
  import type { NameType, ValueType } from "recharts/types/component/DefaultTooltipContent";
  const { active, payload } = props as TooltipProps<ValueType, NameType>;
  ```

### 11.2 Polling agresivo en notificaciones y chat
- [ ] **🟢** Los siguientes hooks pollan cada 10–30s aunque SignalR ya está implementado:
  - `useNotifications.ts` → `refetchInterval: 30_000` — redundante con push notifications
  - `useChatThread.ts` → `refetchInterval: 10_000` y `15_000` — redundante con SignalR
  - `useStoreOrders.ts` → `refetchInterval: 30_000` — podría ser event-driven
  - **Solución:** usar `refetchOnWindowFocus: true` + `staleTime` largo + invalidar via SignalR events en lugar de polling constante

### 11.3 Absence of error boundaries per feature
- [ ] **🟢** Solo hay un `AppErrorBoundary` global en `routes.tsx`
  - Si el `ShelterDashboardPage` falla, toda la app muestra un error
  - **Enterprise:** wrapping por feature/page con boundary específico:
    ```tsx
    <ErrorBoundary fallback={<FeatureErrorFallback />}>
      <ShelterDashboardPage />
    </ErrorBoundary>
    ```

### 11.4 Missing suspense boundaries per route section
- [ ] **⚪** Las páginas tienen `<Suspense fallback={<PageSkeleton />}>` pero el skeleton es genérico para todas
  - Implementar skeletons específicos por página (similar a lo que hacen Google y Meta)

---

## 12. API — gaps

### 12.1 Sin versioning de API
- [ ] **🟡** No existe `AddApiVersioning()` en `Program.cs`
  - Cuando haya breaking changes (cambiar un DTO, renombrar un campo), todos los clientes se rompen
  - **Implementación:**
    ```csharp
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),     // /api/v1/...
            new HeaderApiVersionReader("Api-Version"));
    });
    ```

### 12.2 Sin paginación en endpoints que pueden devolver muchos datos
- [ ] **🟢** Los siguientes endpoints carecen de paginación y podrían devolver N filas sin límite:
  - `GET /api/adoptions/animals/{id}/applications` — puede haber muchas aplicaciones para un animal popular
  - `GET /api/allies/alerts` — limitado a `MaxResults = 50` hardcoded
  - `GET /api/admin/adoptions/animals` — ya tiene paginación ✅
  - **Fix:** añadir `page`/`pageSize` query params con límite máximo de 50

### 12.3 Inconsistencia en códigos de error
- [ ] **🟢** Algunos handlers usan constantes tipadas (`internal const string NotVerifiedShelterError = "not_verified_shelter"`) y otros usan strings literales inlineados (`"Access denied."`, `"Custody record not found."`)
  - **Enterprise:** crear `ErrorCodes` static class con todas las constantes o usar un `enum ErrorCode` convertido a string
  - Permite que el frontend muestre mensajes específicos en español sin depender de strings del servidor

---

## 13. Testing gaps

### 13.1 Sin integration tests para el flujo de adopciones
- [ ] **🟡** El módulo de adopciones tiene unit tests pero ningún integration test end-to-end
  - **Implementar `AdoptionFlowIntegrationTests.cs`:**
    - Test 1: `Publish → Apply → Approve → MarkAdopted` (happy path)
    - Test 2: Gating de 5 animales (ShelterBasic)
    - Test 3: Fair creation bloqueada sin ShelterPlus
    - Test 4: Withdraw después de ApplyToAdopt
    - Usar `WebApplicationFactory<Program>` + SQL Server In-Memory (o SQLite con EF Core)

### 13.2 Sin tests para broadcast channels
- [ ] **🟢** `WhatsAppChannelBroadcaster`, `EmailChannelBroadcaster` no tienen tests
  - `FacebookChannelBroadcaster` y `TelegramChannelBroadcaster` tampoco (aunque son stubs)
  - **Añadir:** tests con `NSubstitute` para `IWhatsAppSender`, `IEmailSender` mockeados

### 13.3 Sin mutation tests
- [ ] **⚪** 1,021 tests pero sin mutation testing (Stryker.NET)
  - Mutation testing revela tests que pasan con código roto
  - **Setup:**
    ```bash
    dotnet tool install -g dotnet-stryker
    dotnet stryker --project PawTrack.Application --mutation-level Standard
    ```

---

## 14. Infrastructure y despliegue

### 14.1 Jobs de background en Container Apps (scale-out problem)
- [ ] **🟡** Alternativa enterprise a hosted services para jobs periódicos:
  - Crear **Azure Container Apps Jobs** separados para: `VetReminderJob`, `HealthAlertJob`, `QrRetentionJob`
  - Los Jobs son instancias efímeras que corren a demanda (cron o trigger), sin duplicación
  - El Bicep ya incluye Container Apps — añadir `containerapp job create` al template

### 14.2 Migración de EF Core en startup — riesgo en scale-out
- [ ] **🟢** `MigrationHelper.cs` corre migraciones al arrancar la app
  - Con múltiples instancias arrancando simultáneamente, hay riesgo de migraciones concurrentes
  - El helper ya tiene un distributed lock comentado (`UPDLOCK`)
  - **Verificar:** que el lock de migración funciona correctamente con múltiples Container App replicas

### 14.3 SendGrid API Key sin rotación automática
- [ ] **🟢** El Key Vault reference de SendGrid no tiene rotación automática configurada
  - **Añadir al Bicep:** `rotationPolicy` para el secreto de SendGrid con notificación 30 días antes
  - Configurar `EventGrid` subscription para recibir notificación de expiración

---

## 15. Monitoring y observabilidad

### 15.1 Sin alertas de Application Insights para errores críticos
- [ ] **🟡** Application Insights está configurado pero sin alertas automáticas para:
  - Tasa de errores 5xx > 1%
  - Latencia P99 > 2s en endpoints de mapa
  - Fallos de SignalR connection
  - **Añadir al Bicep:** `Microsoft.Insights/metricAlerts` para las métricas críticas

### 15.2 Sin distributed tracing entre services
- [ ] **⚪** Si en el futuro se extraen microservicios, no hay correlation de trazas entre ellos
  - **Preparación:** añadir `Activity.Current?.TraceId` al logging estructurado de Serilog
  - Usar `W3C Trace Context` headers en los `HttpClient` para propagación automática

### 15.3 Dashboard de Kusto/KQL para adopciones
- [ ] **⚪** Application Insights no tiene queries de adopciones predefinidas
  - **Crear Workbook en Azure con queries como:**
    ```kusto
    customEvents
    | where name == "AdoptionPublished"
    | summarize count() by bin(timestamp, 1d)
    ```
  - Añadir `telemetry.TrackEvent("AdoptionPublished", ...)` en los handlers

---

## 16. Otros gaps técnicos menores

### 16.1 `GeofencedAlertLog` usa `Guid.NewGuid()`
- [ ] **⚪** `Domain/Locations/GeofencedAlertLog.cs` línea 26
  - **Fix trivial:** `Id = Guid.CreateVersion7()` — una línea, consistente con el resto

### 16.2 ShelterPublish page sin photo upload inline
- [ ] **🟡** Ver sección 9.1 — separado aquí para tracking
  - **UX fix:** añadir `<DropZone>` multi-file en el paso 3 del formulario de publicación usando `useUploadAdoptionPhoto`

### 16.3 UC-06 Seguimiento post-adopción sin implementar
- [ ] **⚪** `adopciones.md` sección UC-06 — check-in 30/90/365 días post-adopción
  - **Sin roadmap activo** — crear spec técnica cuando el módulo de adopciones tenga suficiente tracción

### 16.4 `FosterVolunteer.AcceptedSpeciesCsv` — antipatrón de datos
- [ ] **⚪** `Domain/Fosters/FosterVolunteer.cs` almacena species como CSV string
  - **Enterprise:** tabla `FosterVolunteerSpecies(FosterVolunteerId, Species)` many-to-many
  - O al menos `nvarchar` con JSON array (como `AdoptablePet._photoUrls`)

### 16.5 `BreedActivityBenchmark` y `BreedWeightReference` — datos hardcodeados
- [ ] **⚪** `Domain/Medical/*.cs` tienen diccionarios de razas hardcodeados en C#
  - **Enterprise:** moverlos a tabla `BreedReference` en la DB con seed migration
  - Permite actualizar sin deploy; admins pueden agregar razas nuevas vía panel

### 16.6 Missing cursor pagination en collar GPS
- [ ] **🟢** `GET /api/collars/{id}/location-history` — sin límite documentado ni cursor
  - Un collar Tractive con meses de historial podría devolver miles de puntos
  - **Fix:** añadir `?from=ISO8601&to=ISO8601&maxPoints=500` con downsampling

---

## Resumen por categoría de impacto

| # | Categoría | Críticos 🔴 | Altos 🟡 | Medios 🟢 | Bajos ⚪ |
|---|---|---|---|---|---|
| 1 | STUBs sin implementar | 2 | 1 | — | — |
| 2 | Async anti-patrones | — | 1 | 1 | — |
| 3 | Scale-out / multi-instancia | — | 3 | — | — |
| 4 | Paginación ineficiente | 1 | — | 1 | — |
| 5 | Domain events sin dispatch | — | — | 1 | — |
| 6 | Outbox / confiabilidad | — | 1 | — | — |
| 7 | HTTP retry / resiliencia | — | 2 | — | — |
| 8 | Seguridad residual | — | 2 | — | 1 |
| 9 | UX de shelter | — | 2 | — | — |
| 10 | Audit log | — | 1 | — | — |
| 11 | Frontend calidad | — | — | 3 | 1 |
| 12 | API gaps | — | 1 | 2 | — |
| 13 | Testing gaps | — | 1 | 1 | 1 |
| 14 | Infrastructure | — | 1 | 2 | — |
| 15 | Monitoring | — | 1 | — | 2 |
| 16 | Técnicos menores | — | 2 | 1 | 3 |
| **Total** | | **3** | **19** | **12** | **8** |

---

## Quick wins (< 1h cada uno, alto impacto)

1. **`Guid.NewGuid()` → `Guid.CreateVersion7()`** en `GeofencedAlertLog.cs` — 1 línea
2. **`.Result` → `await`** en `CloseCustodyCommand.cs` y `GetCaseRoomQuery.cs` — 5 líneas
3. **`any` cast en `WeightTrendChart`** → tipo correcto de recharts — 3 líneas  
4. **Polly retry en WhatsApp y SendGrid** — 20 líneas por client
5. **Fotos de adopción borradas al remover animal** — 5 líneas en `AdminModerateAnimalCommandHandler`
6. **Gating de paginación en `GetMyStoreOrders`** — mover Skip/Take a la query de repo — 30 líneas

---

_Última actualización: 2026-08-24 — análisis del commit `2e4af15`_
