# TODOs PENDIENTES — PawTrack CR

> **Fecha:** 2026-09-11  
> **Alcance:** Auditoría integral de Backend (.NET 9) y Frontend (React 19 / TypeScript / PWA).  
> **Propósito:** Identificar con precisión todo código comentado, stubs, características deshabilitadas, mocks, integraciones pendientes y deuda técnica, con su contexto, impacto, solución técnica detallada y checklist de ejecución.

---

## 📋 Resumen Ejecutivo y Checklist de Estado

| ID          | Área            | Categoría                          | Componente / Archivo                                                                                           |             Estado             |        Prioridad         |
| ----------- | --------------- | ---------------------------------- | -------------------------------------------------------------------------------------------------------------- | :----------------------------: | :----------------------: |
| **TODO-01** | Backend & Front | Código comentado / Deshabilitado   | Limpieza código comentado Tractive (`CollarsController.cs`, `InfrastructureExtensions.cs`, `CollarGpsTab.tsx`) |    Completado (2026-09-11)     |         Cerrado          |
| **TODO-02** | Backend & Front | Hardware / Proveedor IoT           | Proveedor `JimiTrackSolid` / AL600 (`CollarProvider.cs`, `TrackSolidService.cs`, `TrackSolidPollingJob.cs`)    |    Completado (2026-09-11)     |         Cerrado          |
| **TODO-03** | Backend         | Dead Code / Proveedor no soportado | Retiro y deprecación `CollarProvider.Kippy = 2`                                                                |    Completado (2026-09-11)     |         Cerrado          |
| **TODO-04** | Backend         | Notificaciones / Transaccional     | Correo de bienvenida tras aprobación de clínica (`ReviewClinicCommandHandler.cs`, `EmailSender.cs`)            |    Completado (2026-09-11)     |         Cerrado          |
| **TODO-05** | Backend         | Concurrencia / Background Services | Migración de bucles `Task.Delay` a `PeriodicTimer` en Hosted Services                                          |    Completado (2026-09-11)     |         Cerrado          |
| **TODO-06** | Frontend        | Copy / Catálogo de Bundles         | Desacoplar mención de "Tractive" en Bundles (`bundleApi.ts`, `BundleOrderModal.tsx`, `FreemiumModal.tsx`)      |    Completado (2026-09-11)     |         Cerrado          |
| **TODO-07** | Backend         | Integración de Pagos / Webhooks    | Pasarela de Pago Automática SINPE / Tarjetas (`WebhooksController.cs`, `SinpePaymentService.cs`)               | MVP Funcional (Manual+Webhook) | Alta (Fase Escalamiento) |
| **TODO-08** | Backend & Front | Configuración Externa / Key Vault  | Credenciales activas para WhatsApp Meta Graph API y Telegram en producción                                     |     Requiere Ops / Secret      |  Alta (Pre-lanzamiento)  |
| **TODO-09** | Backend         | Tarea en Background                | Índices dedicados y bloqueo distribuido en `CollarLocations` y `QrScans`                                       |    Completado (2026-09-11)     |         Cerrado          |
| **TODO-10** | Backend         | Infraestructura / Seguridad        | Creación de `Directory.Build.props` y `.editorconfig` globales                                                 |    Completado (2026-09-11)     |         Cerrado          |

---

## 🔍 Detalle Exhaustivo de Cada TODO

---

### TODO-01: Limpieza de Código Comentado de Integración Externa Tractive GPS — ✅ COMPLETADO

- **Ubicación en el Código:**
  - `backend/src/PawTrack.API/Controllers/CollarsController.cs` (Eliminado bloque comentado de endpoints OAuth2).
  - `backend/src/PawTrack.Infrastructure/InfrastructureServiceCollectionExtensions.cs` (Eliminado HttpClient y servicios huérfanos).
  - `frontend/src/features/pets/components/CollarGpsTab.tsx` (Eliminado bloque comentado JSX de setup manual).
  - Clases eliminadas: `ITractiveService.cs`, `TractiveService.cs`, `TractivePollingJob.cs`.
- **Contexto y Causa Raíz:**
  - Inicialmente se prototipó la integración OAuth2 con la API de Tractive para collares GPS de terceros. Posteriormente, por decisión de negocio y estrategia de hardware propio (iniciativa NALA / Jimi IoT), se deshabilitó el flujo de Tractive para canalizar a los usuarios hacia el collar oficial PawTrack / OEM vía serial y credencial `X-Collar-Key`.
- **Impacto:**
  - El código de producción queda completamente limpio de bloques comentados y clases huérfanas, mejorando la mantenibilidad y documentación OpenAPI.
- **Checklist:**
  - [x] Decisión de producto: Descartar integración directa Tractive y priorizar hardware oficial PawTrack / Jimi IoT AL600.
  - [x] Eliminar código comentado en `CollarsController.cs` y `CollarGpsTab.tsx`.
  - [x] Eliminar clases huérfanas `ITractiveService.cs`, `TractiveService.cs` y `TractivePollingJob.cs`.
  - [x] Limpiar registro en `InfrastructureServiceCollectionExtensions.cs`.

---

### TODO-02: Implementación de Integración Jimi IoT / TrackSolid Pro (AL600) — ✅ COMPLETADO

- **Ubicación en el Código:**
  - `backend/src/PawTrack.Domain/Collars/CollarProvider.cs` (`JimiTrackSolid = 3`).
  - `backend/src/PawTrack.API/Controllers/CollarsController.cs` (Habilitado registro para `CollarProvider.JimiTrackSolid`).
  - `frontend/src/features/pets/api/collarApi.ts` (`export type CollarProvider = "Own" | "JimiTrackSolid" | "Generic" | "Tractive";`).
  - `backend/src/PawTrack.Application/Collars/Interfaces/ITrackSolidService.cs` (Contrato y DTO `TrackSolidPosition`).
  - `backend/src/PawTrack.Infrastructure/Collars/TrackSolidService.cs` (Cliente Open API con autenticación por firma MD5 y chunking de hasta 100 IMEIs).
  - `backend/src/PawTrack.Infrastructure/Collars/TrackSolidPollingJob.cs` (Hosted Service con cadencia dual de 30s/5m, `PeriodicTimer` y `IDistributedJobLock`).
  - `backend/tests/PawTrack.UnitTests/Collars/Services/TrackSolidServiceTests.cs` (Pruebas unitarias completas con NSubstitute).
- **Contexto y Causa Raíz:**
  - Las negociaciones con Jimi IoT confirmaron que el modelo AL600 versión Latinoamérica no soporta HTTP/JSON directo al backend de PawTrack; la telemetría se integra vía la Open API B2B de TrackSolid Pro.
- **Impacto:**
  - La plataforma queda lista para recibir el lote de 2 muestras de AL600, sincronizar ubicaciones en tiempo real y reaccionar ante eventos de mascotas perdidas y geocercas.
- **Checklist:**
  - [x] Agregar `CollarProvider.JimiTrackSolid` en Domain y Frontend.
  - [x] Diseñar e implementar `ITrackSolidService` en Application.
  - [x] Implementar `TrackSolidService` con firma MD5 y manejo de degradación por falta de credenciales.
  - [x] Implementar `TrackSolidPollingJob` con `PeriodicTimer`, lock distribuido y actualización de `LostPetEvents` y `CollarSafeZones`.
  - [x] Registrar HttpClient con resiliencia en `InfrastructureServiceCollectionExtensions.cs`.
  - [x] Crear pruebas unitarias con mock de la respuesta HTTP de TrackSolid.

---

### TODO-03: Limpieza de Dead Code — `CollarProvider.Kippy` — ✅ COMPLETADO

- **Ubicación en el Código:**
  - `backend/src/PawTrack.Domain/Collars/CollarProvider.cs` (`[Obsolete("Kippy is not supported and has been deprecated.", false)] Kippy = 2`).
  - `frontend/src/features/pets/api/collarApi.ts` (Removido `"Kippy"` del tipo unión).
  - `frontend/src/features/pets/components/CollarGpsTab.tsx` (Removido de etiquetas y selectores).
- **Contexto y Causa Raíz:**
  - Se documentó en `docs/TODOs.md §1.3` (2026-09-02) que no se desarrollará soporte para collares Kippy. El enum quedó huérfano en el modelo de dominio.
- **Impacto:**
  - Se eliminó la exposición pública en frontend y se marcó como obsoleto en el backend para preservar la compatibilidad con BD.
- **Checklist:**
  - [x] Verificar que no existan registros funcionales en base de datos.
  - [x] Marcar `[Obsolete]` en `CollarProvider.cs`.
  - [x] Remover `"Kippy"` de `collarApi.ts` y componentes de frontend.

---

### TODO-04: Envío de Correo de Bienvenida a Clínicas tras Aprobación — ✅ COMPLETADO

- **Ubicación en el Código:**
  - `backend/src/PawTrack.Application/Common/Interfaces/IEmailSender.cs` (Método `SendClinicApprovedWelcomeAsync`).
  - `backend/src/PawTrack.Infrastructure/Notifications/EmailSender.cs` (Implementación de plantilla HTML y despacho por SendGrid con fallback estructurado).
  - `backend/src/PawTrack.Application/Clinics/Commands/ReviewClinic/ReviewClinicCommandHandler.cs` (Disparo de correo de bienvenida tras aprobación).
  - `backend/src/PawTrack.Application/Clinics/Commands/RegisterClinic/RegisterClinicCommand.cs` (Documentación del ciclo de vida).
  - `backend/tests/PawTrack.UnitTests/Clinics/Commands/ReviewClinicCommandHandlerTests.cs` (Pruebas unitarias de aprobación y suspensión con NSubstitute).
- **Contexto y Causa Raíz:**
  - Las clínicas se registran con estado inicial `Pending` y requieren aprobación manual de un administrador. Previamente no se disparaba un correo formal de bienvenida con las instrucciones de acceso.
- **Impacto:**
  - Automatización total del onboarding de clínicas veterinarias asociadas una vez aprobada su licencia SENASA.
- **Checklist:**
  - [x] Definir plantilla HTML de bienvenida para clínicas en `EmailSender.cs`.
  - [x] Inyectar `IEmailSender` en `ReviewClinicCommandHandler`.
  - [x] Disparar el correo cuando la clínica pase a estado activo/aprobado.
  - [x] Añadir prueba unitaria para verificar el envío ante aprobación.

---

### TODO-05: Modernización de Background Services a `PeriodicTimer` — ✅ COMPLETADO

- **Ubicación en el Código:**
  - `backend/src/PawTrack.Infrastructure/Medical/HealthAlertHostedService.cs`
  - `backend/src/PawTrack.Infrastructure/Medical/VetReminderHostedService.cs`
  - `backend/src/PawTrack.Infrastructure/Notifications/Jobs/QrScanRetentionHostedService.cs`
  - `backend/src/PawTrack.Infrastructure/Notifications/Jobs/StaleReportCheckerHostedService.cs`
  - `backend/src/PawTrack.Infrastructure/AI/EmbeddingRefreshHostedService.cs`
- **Contexto y Causa Raíz:**
  - Varios servicios en segundo plano calculaban la diferencia hasta la próxima ejecución y usaban `Task.Delay`.
- **Impacto:**
  - Todos los bucles manuales fueron migrados al patrón moderno `PeriodicTimer`, eliminando el riesgo de drift temporal y manteniendo `IDistributedJobLock` para ejecución única y segura en entornos escalados horizontalmente.
- **Checklist:**
  - [x] Refactorizar `HealthAlertHostedService.cs` a `PeriodicTimer`.
  - [x] Refactorizar `VetReminderHostedService.cs` a `PeriodicTimer`.
  - [x] Refactorizar `QrScanRetentionHostedService.cs` a `PeriodicTimer`.
  - [x] Refactorizar `StaleReportCheckerHostedService.cs` a `PeriodicTimer`.
  - [x] Refactorizar `EmbeddingRefreshHostedService.cs` a `PeriodicTimer`.

---

### TODO-06: Desacoplamiento de Marca "Tractive" en Frontend (Bundles y Modales) — ✅ COMPLETADO

- **Ubicación en el Código:**
  - `frontend/src/features/bundles/api/bundleApi.ts`
  - `frontend/src/features/bundles/components/BundleOrderModal.tsx`
  - `frontend/src/features/pets/components/FreemiumModal.tsx`
  - `backend/src/PawTrack.Domain/Bundles/CollarModel.cs`
  - `backend/src/PawTrack.Application/Bundles/BundleOrderCommands.cs`
- **Contexto y Causa Raíz:**
  - Los bundles comerciales fueron originalmente redactados asumiendo la reventa de hardware Tractive. Con la estrategia actual orientada a **AL600 / Hardware PawTrack propio**, mantener menciones explícitas a Tractive en la UI y catálogo de bundles confundía al cliente final.
- **Impacto:**
  - La tienda y los modales promocionales ahora reflejan consistentemente la marca propia "Collar GPS PawTrack", integrando `CollarModel.PawTrackAL600` tanto en el backend como en el frontend.
- **Checklist:**
  - [x] Modificar copy en `bundleApi.ts` reemplazando "Tractive GPS" por "Collar GPS PawTrack".
  - [x] Actualizar texto descriptivo en `BundleOrderModal.tsx`.
  - [x] Actualizar texto de beneficios en `FreemiumModal.tsx`.
  - [x] Agregar `CollarModel.PawTrackAL600` y validar compatibilidad hacia atrás en `BundleOrderCommands.cs`.

---

### TODO-07: Conciliación y Pasarela Automatizada de Pagos SINPE

- **Ubicación en el Código:**
  - `backend/src/PawTrack.Infrastructure/Subscriptions/SinpePaymentService.cs`.
  - `backend/src/PawTrack.API/Controllers/WebhooksController.cs` (Líneas 20–60: endpoint `POST /api/webhooks/sinpe`).
  - `backend/src/PawTrack.Application/Subscriptions/Commands/ActivateSubscription/ActivateSubscriptionCommand.cs`.
- **Contexto y Causa Raíz:**
  - Actualmente, el sistema genera una referencia alfanumérica criptográfica (`SinpePaymentService`) de 8 caracteres. El usuario realiza el pago manual por SINPE Móvil y el administrador lo activa mediante el panel de control o mediante llamada webhook (`/api/webhooks/sinpe`) con firma HMAC-SHA256.
  - No existe aún integración directa con API bancaria abierta (ej. BAC Credomatic o FlexiPago) para conciliación en tiempo real sin intervención humana.
- **Impacto:**
  - Proceso manual o semi-automático que requiere confirmación por parte del equipo de operaciones de PawTrack.
- **Pasos para Completar / Resolver:**
  1. Formalizar contrato con pasarela/adquirente (BAC Credomatic, FlexiPago, etc.).
  2. Implementar servicio de validación de firma y parseo del payload real del procesador.
  3. Configurar secreto de webhook `Webhooks:SinpeSecret` en Azure Key Vault.
  4. Mantener el fallback administrativo en `AdminActivateSubscriptionCommand`.

- **Checklist:**
  - [ ] Definir procesador adquirente para Costa Rica (BAC / FlexiPago).
  - [ ] Adaptar modelo `SinpePaymentNotification` al formato oficial del banco.
  - [ ] Añadir validación de certificados/IPs permitidas en `WebhooksController.cs`.
  - [ ] Pruebas E2E de activación automática de suscripciones y fondos de recompensa (bounty).

---

### TODO-08: Habilitación Operativa de Canales de Difusión Multicanal (Meta & Telegram)

- **Ubicación en el Código:**
  - `backend/src/PawTrack.Infrastructure/Broadcast/Channels/WhatsAppChannelBroadcaster.cs`.
  - `backend/src/PawTrack.Infrastructure/Broadcast/Channels/TelegramChannelBroadcaster.cs`.
  - `backend/src/PawTrack.Infrastructure/Broadcast/Channels/FacebookChannelBroadcaster.cs`.
- **Contexto y Causa Raíz:**
  - La arquitectura y el código de difusión multicanal están 100% implementados y probados. Cada broadcaster verifica su propiedad `IsEnabled` comprobando que las credenciales no sean nulas en la configuración.
  - En local y entornos donde no se configuran tokens, el sistema omite el canal de forma segura (`logger.LogInformation("... skipped — credentials not configured")`).
- **Impacto:**
  - En producción, si las credenciales de Meta Graph API o Telegram Bot no están inyectadas desde Key Vault, los reportes de mascotas perdidas solo saldrán por Correo Electrónico.
- **Pasos para Completar / Resolver:**
  1. Crear la app en Meta for Developers y obtener `PhoneNumberId` y `AccessToken` permanente de WhatsApp Business API.
  2. Crear canal de difusión y Bot de Telegram mediante BotFather; obtener `BotToken` y `ChatId`.
  3. Crear página de Facebook y generar token de página con permisos `pages_manage_posts`.
  4. Agregar las claves en Azure Key Vault (`Broadcast:WhatsApp:*`, `Broadcast:Telegram:*`, `Broadcast:Facebook:*`).

- **Checklist:**
  - [ ] Dar de alta app Meta Business y obtener credenciales de WhatsApp Cloud API.
  - [ ] Dar de alta Bot de Telegram y canal oficial de alertas.
  - [ ] Cargar secretos en Azure Key Vault de producción.
  - [ ] Ejecutar prueba de difusión real en staging/producción.

---

### TODO-09: Verificación de Retención y Purga de Datos (Cumplimiento Ley 8968) — ✅ COMPLETADO

- **Ubicación en el Código:**
  - `backend/src/PawTrack.Infrastructure/Persistence/Configurations/CollarConfiguration.cs` (Índice dedicado `IX_CollarLocations_RecordedAt`).
  - `backend/src/PawTrack.Infrastructure/Persistence/Configurations/QrScanEventConfiguration.cs` (Índice dedicado `IX_QrScanEvents_ScannedAt`).
  - `backend/src/PawTrack.Infrastructure/Collars/CollarLocationPurgeJob.cs` (Inyección de `IDistributedJobLock` con arrendamiento de 1 hora).
  - `backend/src/PawTrack.Infrastructure/Compliance/PersonalDataRetentionJob.cs`
  - `backend/src/PawTrack.Infrastructure/Notifications/Jobs/QrScanRetentionJob.cs`
- **Contexto y Causa Raíz:**
  - Los jobs de cumplimiento de retención (eliminación de ubicaciones de collares >30 días, chats cerrados >30 días, notificaciones leídas >90 días) requerían índices dedicados sobre columnas de timestamp para evitar table scans y bloqueos durante `ExecuteDeleteAsync`.
- **Impacto:**
  - Purga masiva altamente eficiente con índices específicos para `RecordedAt` y `ScannedAt`, y garantía de ejecución atómica única mediante bloqueo distribuido.
- **Checklist:**
  - [x] Verificar y agregar índice dedicado en `CollarLocations` sobre `RecordedAt`.
  - [x] Verificar y agregar índice dedicado en `QrScanEvents` sobre `ScannedAt`.
  - [x] Inyectar `IDistributedJobLock` en `CollarLocationPurgeJob`.
  - [x] Validar que las consultas de eliminación masiva operen con índices optimizados.

---

### TODO-10: Configuración Global de Compilación y Calidad de Código — ✅ COMPLETADO

- **Ubicación en el Código:**
  - Raíz del repositorio: `Directory.Build.props`
  - Raíz del repositorio: `.editorconfig`
- **Contexto y Causa Raíz:**
  - El proyecto carecía de archivos estándar de gobierno de compilación y estilo en la raíz de la solución.
- **Impacto:**
  - `Directory.Build.props` unifica `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<Deterministic>true</Deterministic>`, `<LangVersion>latest</LangVersion>`, metadatos de producto/empresa y activa `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` para configuraciones de Release/CI.
  - `.editorconfig` estandariza indentación, UTF-8, finales de línea CRLF, directivas using fuera de namespace y estilo de código para C#, TypeScript, JavaScript, JSON y Markdown.
- **Checklist:**
  - [x] Crear `.editorconfig` unificado en la raíz.
  - [x] Crear `Directory.Build.props` con políticas de advertencias y versiones.
  - [x] Verificar compilación limpia en todos los proyectos backend (0 errores, 0 warnings).
  - [x] Verificar typecheck y linter limpios en frontend.

---

## 📌 Guía de Seguimiento y Actualización de este Documento

1. Cada vez que se resuelva un ítem de esta lista, marcar el checkbox correspondiente con `[x]` tanto en la tabla inicial como en la sección de detalle.
2. Si un TODO requiere una decisión de producto previa, no modificar código hasta contar con la definición formal.
3. Este documento debe actualizarse al cierre de cada sprint de ingeniería.
