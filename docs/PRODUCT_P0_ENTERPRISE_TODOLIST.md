# PawTrack CR - TODO Enterprise P0

> Estado: activo. Fecha de inicio: 2026-09-09.
> Objetivo: cerrar los bloqueadores de confianza, activacion, evidencia y
> calidad necesarios antes de escalar PawTrack por cantones o vender claims
> regulatorios/comerciales.
>
> Este documento convierte `PRODUCT_STRATEGY_TOP1.md` y `STATUS.md` en tareas
> ejecutables. `[ ]` pendiente, `[~]` en progreso, `[x]` implementado y validado.
> Cada `[x]` requiere evidencia reproducible.

## P0-A - Gobernanza y claims

- [ ] Revisar todos los claims publicos: `production-ready`, `SENASA-ready`,
      tasas de recuperacion, verificado, certificado y seguro.
- [ ] Crear matriz claim -> evidencia tecnica -> evidencia operativa -> aprobacion
      legal -> superficie donde se publica.
- [ ] Retirar de UI/marketing cualquier claim sin evidencia aprobada.
- [ ] Definir responsable y fecha de expiracion para cada claim.
- [ ] Separar claramente `implementado`, `beta`, `draft`, `verificado por
PawTrack` y `avalado por autoridad externa`.
- [ ] Confirmar con legal el tratamiento de salud, ubicacion, fotos, embeddings,
      comunicaciones y datos de menores/familia.
- [ ] Actualizar terminos, privacidad y consentimientos antes de activar nuevas
      superficies institucionales.

## P0-B - Secretos y supply chain

- [ ] Confirmar que `secrets/sa_password.txt` es solo local y no contiene una
      credencial real reutilizada.
- [ ] Rotar inmediatamente cualquier secreto real expuesto.
- [x] Activar secret scanning en CI y bloquear pushes con secretos mediante Gitleaks.
- [x] Agregar dependency scanning para npm y NuGet en CI.
- [ ] Definir politica de renovacion para JWT, Key Vault, API keys y certificados.
- [ ] Documentar respuesta ante secreto comprometido.

## P0-C - Calidad de entrega

- [x] Corregir `test-backend` para ejecutar `PawTrack.sln`, no `backend/`.
- [~] Ejecutar build, unitarias, integración, typecheck, lint y frontend tests
  en CI con gates obligatorios. — Stack local validado: backend/Azurite +
  `recovery-cycle.spec.ts` Playwright 1/1; falta consolidar todos los gates
  como workflow obligatorio único.
- [ ] Añadir `TreatWarningsAsErrors` o política equivalente por proyecto.
- [x] Escenario completo implementado en `frontend/e2e/recovery-cycle.spec.ts`;
      validado contra backend/Azurite vivos: 1/1 passing el 2026-09-09, incluyendo
      broadcast autorizado, chat enmascarado, QR público y handover.
- [ ] Evitar builds/test paralelos que bloqueen `obj/` en Windows CI.

## P0-I - Enterprise security and operations additions

- [x] TOTP MFA with protected secrets, one-time recovery codes, privileged-role policy, and migration.
- [x] Authenticated rate-limit partitioning by API key/user with IP fallback.
- [x] Clinic operations UI for veterinarian permissions and appointment scheduling.
- [x] Medical retention job covers superseded record versions and expired clinic-export metadata.
- [~] WebAuthn, complete tenant/RBAC model, outbound signed webhooks, Store/Municipality exports, SQL concurrency tests, and complete B2B E2E workflows. — Outbound webhook subscriptions, Product Analytics fanout and delivery are implemented with HMAC signatures, idempotency headers, timeout, distributed lock, exponential retry, terminal failure and metrics; the other blocks remain open.

## P0-D - Eventos y funnel de activación

### Contrato de eventos

- [x] Definir contrato frontend/backend versionado con `eventName`,
      `schemaVersion`, `eventId`, `occurredAt`, identidad anónima y `source`.
- [x] Generar `eventId` único por emisión y deduplicar en backend con índice
      único e ingestión idempotente.
- [x] Persistir eventos en `ProductEvents` con índices de ingestión y consulta.
- [x] Exponer ingestión pública limitada y consulta de funnel restringida a Admin.
- [x] Agregar agregación SQL por evento, periodo y cantón.
- [x] Definir retención y borrado de eventos mediante `PersonalDataRetentionJob`;
      exportación y acceso administrativo granular siguen pendientes.
- [x] Mantener el contrato de telemetría frontend separado de eventos de dominio
      y Outbox.

### Eventos mínimos

- [x] `PetRegistered`.
- [x] `PetProfileCompleted`.
- [x] `QrGenerated`.
- [ ] `QrActivated`.
- [x] `QrScanned`.
- [x] `LostPetReported`.
- [x] `SightingCreated`.
- [x] `FoundPetReported`.
- [ ] `FirstResponseRecorded`.
- [x] `HandoverStarted`.
- [x] `HandoverCompleted`.
- [x] `PetReunited`.

### Funnel y métricas

- [ ] Medir registro -> foto -> contacto -> QR -> activación.
- [ ] Medir QR generado -> QR escaneado -> contacto seguro.
- [ ] Medir pérdida -> primer avistamiento -> respuesta -> reunificación.
- [ ] Medir tiempos p50/p90 por cantón y canal.
- [ ] Medir mascotas activas protegidas en 30/90/180 días.
- [x] Endpoint Admin de funnel con periodo, cantón y agregación SQL implementado;
      dashboard visual Admin implementado. Exportación partner con scopes sigue
      pendiente porque falta una identidad M2M canónica.
- [x] Crear export agregado CSV para Admin sin exponer PII.
- [x] Mantener export clínico protegido por `medical:export`; `medical:read`
      no concede permiso de exportación.

> Avance validado: ingestión y deduplicación backend implementadas. Retención y
> dashboard visual Admin implementado. Exportación partner global con scopes y
> retención granular configurable siguen pendientes; export institucional ya
> valida organización propia/Admin.

## P0-E - Finder sin login

- [x] Perfil QR público, flujo de mascota encontrada y contacto relay anónimo
      existen; el relay entrega mensajes al dueño sin exponer PII.
- [x] Mostrar perfil mínimo público sin exponer PII del propietario.
- [x] Permitir reportar mascota encontrada sin crear cuenta.
- [ ] Permitir foto, ubicación aproximada, timestamp y mensaje sanitizado.
- [ ] Añadir consentimiento y aviso de privacidad contextual.
- [ ] Añadir rate limit, antifraude, captcha/risk challenge y abuso.
- [ ] Añadir fallback WhatsApp/SMS/email cuando el contacto seguro falle.
- [ ] Soportar conexión lenta y cola offline sin perder el reporte.
- [ ] No exponer dirección, teléfono, correo ni coordenadas exactas del dueño.
- [ ] Crear pruebas de autorización, abuso, payloads grandes y PII.

## P0-F - E2E del ciclo de recuperación

- [x] Escenario completo implementado y ejecutado en
      `frontend/e2e/recovery-cycle.spec.ts` contra API, SQL/migraciones,
      Azurite y frontend preview.
- [x] Registro de usuario y mascota mediante fixtures API.
- [x] Reporte de pérdida, QR público y validación de ausencia de PII.
- [x] Handover con código seguro y cierre de reunificación.
- [~] Notificación/broadcast, comunicación enmascarada y evento `PetReunited`
  como eventos de producto. Broadcast y chat ya tienen cobertura en el E2E;
  la cobertura de eventos de producto y notificación externa sigue pendiente.
- [x] Playwright conserva trace y screenshot en fallos; el workflow E2E publica
      el reporte como artefacto CI.
- [x] E2E incluye chat enmascarado entre finder y propietario y verifica que no
      se filtra el teléfono.

## P0-G - Contratos API y autorización

- [x] OpenAPI 1.0 se publica en runtime y CI verifica su superficie crítica,
      lint Redocly y conserva el contrato como artefacto; diff automático contra
      snapshot aprobado sigue pendiente.
- [ ] Generar o validar tipos frontend desde el contrato.
- [ ] Detectar breaking changes en CI.
- [ ] Versionar endpoints públicos y partner.
- [x] Crear matriz endpoint -> rol -> ownership -> PII -> rate limit en
      `docs/API_AUTHORIZATION_MATRIX.md`.
- [~] Pruebas BOLA de chat, mascotas, tiendas/pedidos, municipalidades y
  expediente clínico ya existen; handlers clínicos validan ownership, grants
  y precedencia QR/chip sobre `PetId`; API key scope de export corregido;
  autorización institucional exige perfil municipal y cantón para actores no
  Admin; proveedores y reservas ya tienen regresiones de participante para
  lecturas, transiciones, reprogramaciones, pagos e incidentes; scopes
  Admin/Nala de reportes y catálogo quedan protegidos en controller y servicio;
  faltan suites sistemáticas de clínicas, collares y reportes partner.
- [ ] Revisar endpoints públicos para minimización de datos.

## P0-H - Criterios de salida

- [ ] No existe claim público sin evidencia y aprobación.
- [ ] Secret scanning y dependency scanning bloquean regresiones.
- [ ] CI ejecuta todos los gates y publica artefactos.
- [ ] Se puede calcular la North Star Metric desde eventos reales.
- [ ] Finder puede reportar en menos de 30 segundos sin registro.
- [ ] El ciclo pérdida -> reunificación pasa E2E en ambiente limpio.
- [ ] Ningún endpoint crítico permite acceso cross-tenant.
- [ ] Dashboard P0 tiene métricas por cantón, canal y fecha.
- [ ] Legal, producto, operaciones y seguridad firman el gate de lanzamiento.

## Orden recomendado

1. Calidad de entrega, secretos y claims.
2. Contrato de eventos y funnel.
3. Finder sin login.
4. E2E del ciclo completo.
5. Contratos API y matriz BOLA/IDOR.
6. Dashboard territorial y gate de escala.
