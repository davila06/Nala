# PawTrack CR — TODOs y Mejoras Enterprise

> **Fecha:** 2026-08-24 | Análisis exhaustivo del codebase completo  
> **Metodología:** grep de TODOs/STUBs/`.Result`/`.Wait()`/anti-patrones + revisión manual de arquitectura  
> **Prioridad:** 🔴 Crítico · 🟡 Alto · 🟢 Medio · ⚪ Bajo/Futuro  
> **⚠️ Re-auditado 2026-09-02:** una ola grande de hardening (fechada el mismo 2026-08-24, horas después de este análisis) resolvió ~15 de los ítems originales sin que este documento se actualizara. Cada ítem marcado `[x]` abajo fue re-verificado contra el código actual en esa fecha; los que siguen `[ ]` siguen abiertos de verdad.

---

## 1. STUBs — Código que no hace nada en producción

### 1.1 Facebook Broadcast Channel — ✅ resuelto

- [x] ~~`FacebookChannelBroadcaster.cs` siempre retorna `null` y solo loguea~~
  - Ya hace `POST https://graph.facebook.com/v19.0/{pageId}/feed` real con `HttpClient("Facebook")`, maneja éxito/error y solo retorna `null` cuando faltan credenciales o la API falla (no incondicionalmente). Verificado 2026-09-02.

### 1.2 Telegram Broadcast Channel — ✅ resuelto

- [x] ~~`TelegramChannelBroadcaster.cs` mismo problema~~
  - Ya hace `POST https://api.telegram.org/bot{token}/sendMessage` real con `HttpClient("Telegram")`, incluye envío de foto (`sendPhoto`). Verificado 2026-09-02.

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

### 2.1 `.Result` después de `Task.WhenAll` — ✅ resuelto

- [x] ~~`CloseCustodyCommand.cs` líneas 57-59 usaba `.Result` en vez de `await`~~
  - Ya usa `var fosterUser = await fosterUserTask; var ownerUser = await ownerUserTask;`. Verificado 2026-09-02. `GetCaseRoomQuery.cs` línea 166 no re-verificada.

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

### 2.3 Background Services en múltiples instancias (duplicated jobs) — 🟡 parcialmente resuelto

- [x] ~~`VetReminderHostedService`, `StaleReportCheckerHostedService`, `QrScanRetentionHostedService`~~ ya usan `IDistributedJobLock.TryAcquireAsync(...)` antes de ejecutar. Verificado 2026-09-02.
- [ ] **🟡** `HealthAlertHostedService.cs` y `EmbeddingRefreshHostedService.cs` **todavía NO** adquieren el distributed lock — siguen duplicando trabajo en scale-out. Aplicar el mismo patrón que los tres anteriores.
  - Alternativamente: usar Azure Container Apps Jobs (run-to-completion) en lugar de hosted services

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

### 4.1 GetMyStoreOrders — ✅ resuelto (paginación en base de datos)

- [x] ~~`Application/Stores/StoreOrderCommands.cs` cargaba todos los pedidos del cliente en memoria~~
  - Ya usa `IStoreOrderRepository.GetByCustomerPagedAsync` (SQL `Skip/Take` + `AsNoTracking`) y `CountByCustomerAsync` para el total; retorna `PagedResult<StoreOrderDto>`. Verificado 2026-09-02; el método sin paginar quedó eliminado por no tener llamadores.

### 4.2 Skip/Take degrada en tablas grandes — ✅ resuelto para Notifications

- [x] ~~Implementar `CursorPagedResult<T, TCursor>` genérico~~ — existe `CursorPagedResult<T>` (`Application/Common/CursorPagedResult.cs`) y ya se usa en `GetMyNotificationsCursorQuery`. Verificado 2026-09-02.
  - **Sigue pendiente:** confirmar si `QrScanHistoryQuery` y el historial de ubicación de collares (ver §16.6) ya migraron a cursor o siguen con `Skip/Take`.

---

## 5. Domain Events sin dispatch

### 5.1 Domain events definidos pero ignorados — ✅ resuelto

- [x] ~~Los eventos existen pero no se publican a ningún handler~~
  - `PawTrackDbContext.SaveChangesAsync` ya colecciona `ChangeTracker.Entries<IHasDomainEvents>()`, los limpia, y los despacha (via outbox — ver §6.1) exactamente con el patrón enterprise recomendado aquí. Verificado 2026-09-02.

---

## 6. Confiabilidad de notificaciones (outbox pattern)

### 6.1 Fire-and-forget con riesgo de pérdida — ✅ resuelto

- [x] ~~Implementar Transactional Outbox~~ — ya existe: tabla `OutboxMessages`, `PawTrackDbContext.SaveChangesAsync` serializa los domain events al outbox en la misma transacción, y `OutboxProcessorHostedService` (con `IDistributedJobLock`) los procesa cada 10s y los despacha via MediatR. Verificado 2026-09-02.

---

## 7. Retry policies para HTTP clients

### 7.1 Sin Polly en clientes externos — ✅ resuelto

- [x] ~~`MetaWhatsApp`, `AzureVision`, `AzureMaps`, `PushProvider`, `Facebook`, `Telegram`, `Tractive` sin retry policy~~ — los 7 `AddHttpClient(...)` en `InfrastructureServiceCollectionExtensions.cs` ya encadenan `.AddStandardResilienceHandler()` (el estándar `Microsoft.Extensions.Http.Resilience` recomendado aquí, no Polly directo). Verificado 2026-09-02.

### 7.2 SendGrid sin retry — ✅ resuelto

- [x] ~~La llamada a SendGrid no tiene retry~~ — `EmailSender.cs` ya reintenta hasta 3 veces en errores transitorios (comentario explícito "Retry up to 3 times on transient errors"). Verificado 2026-09-02.

---

## 8. Seguridad — gaps residuales

### 8.1 Race condition en idempotency del bot de WhatsApp — ✅ resuelto

- [x] ~~`HandleWhatsAppWebhookCommandHandler.cs` — check-then-set sobre `BotSession.ProcessedMessageIds` sin lock~~
  - Ya existe una guarda a nivel de base de datos: `IWhatsAppIdempotencyRepository.TryMarkAsync` hace un `INSERT` atómico en la tabla `WhatsAppProcessedMessages`, que tiene un índice único sobre `Wamid` (migración `AddWhatsAppIdempotencyTable`, `WhatsAppProcessedMessageConfiguration`). El handler llama a este guard **antes** de tocar `BotSession` y corta temprano si el `INSERT` falla por duplicado (`DbUpdateException` con mensaje de violación de constraint único). El check-then-set sobre `BotSession.IsMessageProcessed`/`MarkMessageProcessed` sigue existiendo como segunda guarda (deduplicación dentro de la misma sesión), pero ya no es la única línea de defensa. Verificado 2026-09-02.
  - **Deuda pendiente:** no hay test automatizado que ejercite el constraint único a nivel de base de datos — la suite de integración usa el proveedor EF Core InMemory, que no aplica índices únicos. Cubrir esto correctamente requeriría un fixture con SQL Server real o SQLite, y el modelo completo de `PawTrackDbContext` tiene columnas espaciales (`Sighting.Location` como `geography`) que complican un DbContext de prueba aislado sin invertir en un fixture dedicado — no se justificó el esfuerzo en esta sesión.

### 8.2 GeofencedAlertLog usa `Guid.NewGuid()` en lugar de `Guid.CreateVersion7()` — ✅ resuelto

- [x] ~~inconsistencia menor con el resto del codebase~~ — ya usa `Id = Guid.CreateVersion7()`. Verificado 2026-09-02 (duplicado de §16.1).

### 8.3 Adoption photo blobs no se borran al remover animal — ✅ resuelto

- [x] ~~`AdminModerateAnimalCommand` no borraba los blobs~~ — `AdminModerateAnimalCommandHandler` ya llama a `blobStorage.DeleteAsync(photoUrl, ct)`. Verificado 2026-09-02.

---

## 9. Shelter Publish — UX incompleta

### 9.1 Fotos no se pueden subir durante la publicación — ✅ resuelto

- [x] ~~El usuario tiene que navegar manualmente al dashboard para encontrar el animal y subir fotos~~
  - `ShelterPublishPage.tsx` ya tiene `useUploadAdoptionPhoto` + `handleUploadPhotos` inline en el mismo flujo de publicación. Verificado 2026-09-02 (duplicado de §16.2).

---

## 10. Audit log para acciones admin

### 10.1 Sin trazabilidad de acciones administrativas — ✅ resuelto

- [x] ~~No existe registro de quién aprobó un aliado, quién activó una suscripción, quién removó un animal~~
  - Ya existe `AuditLogEntry` + `IAuditLogRepository` + `GetAuditLogQuery`, poblado desde `ReviewAllyApplicationCommandHandler`, `ReviewClinicCommandHandler`, `AdminActivateSubscriptionCommand`, `StoreCommands`, `AdoptionAdminQueries`, etc. Verificado 2026-09-02.

---

## 11. Frontend — mejoras de calidad

### 11.1 `any` cast en WeightTrendChart — ✅ resuelto

- [x] ~~`const { active, payload } = props as any;`~~ — ya usa `TooltipProps<number, string>` de recharts. Verificado 2026-09-02.

### 11.2 Polling agresivo en notificaciones y chat — 🟢 parcialmente re-verificado

- [x] ~~`useNotifications.ts` con `refetchInterval: 30_000`~~ — ya no tiene `refetchInterval`. Verificado 2026-09-02.
- [ ] **🟢** `useChatThread.ts` y `useStoreOrders.ts` no re-verificados en esta pasada — asumir abiertos hasta confirmar.

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

### 12.1 Sin versioning de API — ✅ resuelto

- [x] ~~No existe `AddApiVersioning()` en `Program.cs`~~ — ya está configurado. Verificado 2026-09-02.

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

### 13.1 Sin integration tests para el flujo de adopciones — ✅ resuelto

- [x] ~~El módulo de adopciones tiene unit tests pero ningún integration test end-to-end~~ — `AdoptionFlowIntegrationTests.cs` ya existe. Verificado 2026-09-02 (contenido exacto de los casos no re-verificado).

### 13.2 Sin tests para broadcast channels — 🟢 parcialmente resuelto

- [x] ~~`WhatsAppChannelBroadcaster`, `EmailChannelBroadcaster` no tenían tests~~ — `WhatsAppChannelBroadcasterTests.cs`, `EmailChannelBroadcasterTests.cs` y `TelegramChannelBroadcasterTests.cs` ya existen. Verificado 2026-09-02.
- [ ] **🟢** `FacebookChannelBroadcasterTests` sigue sin existir — único canal sin cobertura.

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

### 16.1 `GeofencedAlertLog` usa `Guid.NewGuid()` — ✅ resuelto

- [x] ~~cambiar a `Guid.CreateVersion7()`~~ — ver §8.2, ya aplicado.

### 16.2 ShelterPublish page sin photo upload inline — ✅ resuelto

- [x] ~~ver §9.1~~ — ya implementado.

### 16.3 UC-06 Seguimiento post-adopción sin implementar

- [ ] **⚪** `adopciones.md` sección UC-06 — check-in 30/90/365 días post-adopción
  - **Sin roadmap activo** — crear spec técnica cuando el módulo de adopciones tenga suficiente tracción

### 16.4 `FosterVolunteer.AcceptedSpeciesCsv` — antipatrón de datos — ✅ resuelto

- [x] ~~almacena species como CSV string~~ — ya usa `AcceptedSpeciesJson` (JSON array), manteniendo el nombre de columna `AcceptedSpeciesCsv` solo para no requerir migración, con parseo legacy CSV como fallback de compatibilidad. Verificado 2026-09-02.

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

| #         | Categoría                   | Críticos 🔴 | Altos 🟡 | Medios 🟢 | Bajos ⚪ |
| --------- | --------------------------- | ----------- | -------- | --------- | -------- |
| 1         | STUBs sin implementar       | 2           | 1        | —         | —        |
| 2         | Async anti-patrones         | —           | 1        | 1         | —        |
| 3         | Scale-out / multi-instancia | —           | 3        | —         | —        |
| 4         | Paginación ineficiente      | 1           | —        | 1         | —        |
| 5         | Domain events sin dispatch  | —           | —        | 1         | —        |
| 6         | Outbox / confiabilidad      | —           | 1        | —         | —        |
| 7         | HTTP retry / resiliencia    | —           | 2        | —         | —        |
| 8         | Seguridad residual          | —           | 2        | —         | 1        |
| 9         | UX de shelter               | —           | 2        | —         | —        |
| 10        | Audit log                   | —           | 1        | —         | —        |
| 11        | Frontend calidad            | —           | —        | 3         | 1        |
| 12        | API gaps                    | —           | 1        | 2         | —        |
| 13        | Testing gaps                | —           | 1        | 1         | 1        |
| 14        | Infrastructure              | —           | 1        | 2         | —        |
| 15        | Monitoring                  | —           | 1        | —         | 2        |
| 16        | Técnicos menores            | —           | 2        | 1         | 3        |
| **Total** |                             | **3**       | **19**   | **12**    | **8**    |

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
