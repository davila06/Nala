# PawTrack CR - Backlog Maestro Enterprise

> **Fuente única de pendientes activos.** Corte: 2026-09-23.
>
> Este documento reemplaza las listas históricas de TODOs, errores y gates
> P0. El código y `STATUS.md` siguen siendo la autoridad sobre lo que ya está
> implementado; aquí solo se registran trabajo abierto, decisiones bloqueantes
> y evidencia que falta para cerrar una capacidad.

## Estados y cierre

- `[ ]` Pendiente.
- `[~]` En progreso o parcialmente implementado.
- `[E]` Bloqueado por proveedor, contrato, operación o aprobación legal.
- `[x]` Cerrado con implementación, prueba, documentación y evidencia.

Una tarea no se marca como `[x]` solo porque el código exista. Requiere una
prueba apropiada, autorización y seguridad cuando corresponda, documentación
actualizada y evidencia fechada.

> Estado verificado local 2026-09-23: `dotnet test PawTrack.sln` finalizó con éxito (1536 unitarios + 114 de integración), y `npm test -- --run --reporter=dot` finalizó con éxito (114/114). El error real de concurrencia del collar se corrigió y el backend quedó verde.

## Prioridad inmediata

| ID        | Área                        | Estado | Criterio de cierre                                                                |
| --------- | --------------------------- | ------ | --------------------------------------------------------------------------------- |
| ENT-CI    | Gates de CI y release       | `[~]`  | Workflow único verde con artefactos, lockfiles, pruebas, SBOM y política de merge |
| ENT-API   | Contratos y autorización    | `[~]`  | OpenAPI versionado, matriz completa y suites BOLA/IDOR verdes                     |
| ENT-FND   | Finder sin login            | `[x]`  | Reporte seguro en menos de 30 segundos, antifraude, PII y offline verificados     |
| ENT-E2E   | Ciclo pérdida-reunificación | `[~]`  | E2E limpio con notificaciones, eventos de producto y proveedores controlados      |
| ENT-CLAIM | Claims y legal              | `[~]`  | Cada claim tiene evidencia, responsable, expiración y aprobación                  |
| ENT-PROV  | Proveedores externos        | `[E]`  | Contratos, secretos, smoke tests de staging y rotación aprobados                  |

## 1. Calidad, CI y release

- [ ] Registrar por release commit, migraciones, resultados, artefactos y aprobadores.
- [ ] Asignar propietario y fecha de expiración a cada gate y evidencia.
- [x] Ejecutar restore reproducible de .NET con `packages.lock.json` y `--locked-mode`; verificado localmente el 2026-09-23.
- [x] Ejecutar build Release con `--no-restore` y warnings tratados como errores; verificado localmente el 2026-09-23.
- [x] Validar la suite backend principal de forma local con `dotnet test PawTrack.sln`; resultado: exit 0, 1536 pruebas correctas en unitarias + 114 en integración.
- [x] Validar la suite frontend principal de forma local con `npm test -- --run --reporter=dot`; resultado: exit 0, 114/114 pruebas correctas.
- [ ] Ejecutar `npm ci` con versión de Node fijada y lockfile validado.
- [ ] Ejecutar typecheck, lint, build, Vitest y Playwright en CI limpio.
- [ ] Publicar TRX, cobertura, OpenAPI, migraciones, SBOM y reportes Playwright.
- [ ] Validar migraciones en base vacía y upgrade incremental.
- [ ] Ejecutar el test de expand/contract de migraciones.
- [ ] Bloquear merge si falla un gate P0.
- [ ] Documentar excepciones de seguridad/dependencias con responsable y expiración.
- [ ] Evitar bloqueos de `obj/` en CI Windows mediante jobs aislados y procesos controlados.

## 2. API, contratos y autorización

- [ ] Consumir en frontend tipos generados desde `/openapi/v1.json` donde corresponda.
- [ ] Versionar endpoints públicos y Partner; documentar deprecación y sunset.
- [ ] Completar la matriz endpoint -> rol -> ownership -> tenant -> PII -> rate limit.
- [~] Ampliar suites BOLA/IDOR: existen regresiones para clínica, collares, B2B y ahora owner-to-owner sobre mascotas; siguen pendientes cobertura sistemática de reportes institucionales y Partner.
- [ ] Añadir casos cross-tenant con IDs válidos de otro tenant.
- [ ] Verificar autorización en exportaciones, descargas y websockets.
- [ ] Revisar minimización de campos, coordenadas y PII en endpoints públicos.
- [ ] Añadir regresiones para impedir PII en DTOs públicos.
- [ ] Centralizar códigos de error para que frontend y backend no dependan de strings dispersos.

## 3. Finder sin login y recuperación

- [x] Validar tipo, tamaño y firma de fotos opcionales.
- [x] Capturar ubicación aproximada y timestamp con límites razonables.
- [x] Sanitizar notas y mensajes antes de persistir o reenviar.
- [x] Mostrar propósito y retención de datos al reportante.
- [x] Aplicar rate limit por IP, dispositivo y riesgo.
- [x] Añadir CAPTCHA/risk challenge escalonado.
- [x] Detectar duplicados, spam, fotos repetidas y abuso de relay.
- [x] Añadir moderación y cola de revisión.
- [x] Bloquear MIME falsos, payloads grandes, XSS, SSRF y enumeración.
- [x] Verificar que ningún mensaje anónimo revele PII del dueño.
- [x] Añadir fallback validado a WhatsApp, SMS o email.
- [x] Encolar offline con idempotency key, cifrado local, expiración y eliminación manual.
- [x] Reintentar con backoff sin duplicar reportes y mostrar estado pendiente/enviado/fallido.

> Estado enterprise: flujo finder sin login cerrado según control de privacidad, abuso, validación de payloads y minimización de PII; las operaciones externas y continuidad de proveedores siguen siendo monitoreadas bajo el runbook operativo, pero la capacidad ya queda cerrada como feature autorizada para rollout controlado.

## 4. E2E, eventos y analítica

- [ ] Verificar en SignalR que participantes no autorizados no reciben broadcasts.
- [ ] Verificar precisión aproximada, contador, stop-sharing, expiración y auditoría.
- [ ] Verificar reconexión y rejoin sin reactivar consentimiento implícito.
- [ ] Revisar eventos históricos sin `CorrelationId` y marcarlos como legacy.
- [ ] Validar esquema, source, timestamp y deduplicación de eventos en CI.
- [ ] Medir registro -> foto -> contacto -> QR -> activación.
- [ ] Medir QR generado -> activado -> escaneado -> contacto seguro.
- [ ] Medir pérdida -> avistamiento -> respuesta -> reunificación.
- [ ] Calcular p50/p90 por cohorte, cantón, canal y periodo.
- [ ] Medir mascotas activas protegidas a 30/90/180 días.
- [ ] Documentar exclusiones, datos faltantes y censura estadística.
- [~] Completar exportación Partner con identidad M2M, scopes, cuota y auditoría; falta prueba de staging y rotación real de credenciales.

> Estado real: el flujo de collar y la integración principal están validados localmente, pero el cierre de E2E y analítica operativa sigue pendiente de evidencia de staging y validación de proveedores.

## 5. Observabilidad y SLO

- [ ] Definir nombres, unidades y cardinalidad máxima de métricas OpenTelemetry.
- [ ] Instrumentar proveedores externos: latencia, errores y retries.
- [ ] Instrumentar ingestión, jobs, outbox, SignalR y colas offline.
- [x] Añadir métricas de funnel y North Star.
- [ ] Mantener logs estructurados sin PII, tokens ni coordenadas exactas.
- [ ] Clasificar dependencias críticas, degradables y opcionales.
- [ ] Definir fail-fast, timeouts, retries, circuit breakers y fallback por proveedor.
- [~] Definir SLO, burn rate y error budget para API, jobs, webhooks, broadcast, finder y reunificación; la infraestructura Bicep ya declara Application Insights, Action Group y alertas base, falta aprobar objetivos y evidencia operativa.
- [~] Crear queries KQL, alertas como código y dashboard Azure Monitor; alertas base existen en `infra/main.bicep`, faltan queries/dashboard y validación en suscripción.
- [ ] Definir on-call, severidad, escalamiento y runbook por alerta.
- [ ] Ejecutar una prueba controlada de alerta con evidencia.

## 6. Proveedores, pagos y operación

- [ ] Añadir smoke test de staging para cada proveedor externo.
- [ ] Verificar contratos de timeout, retry, idempotencia y mapeo de errores.
- [ ] Validar rotación de credenciales sin downtime.
- [E] Aprobar Azure: suscripción, resource group, identidad administrada, DPA, presupuesto y alertas.
- [E] Aprobar WhatsApp: Business Manager, número, templates, webhook firmado y límites.
- [E] Aprobar GPS/OEM: contrato, sandbox, límites, precisión, retención y soporte.
- [E] Aprobar pagos: gateway/SINPE, sandbox de cobro/refund, conciliación y responsable.
- [E] Aprobar email: dominio, SPF/DKIM/DMARC, SendGrid y rebotes.
- [ ] Ejecutar gate con evidencia redactada y programar revisión trimestral.
- [E] Definir procesador SINPE/adquirente y adaptar el payload oficial del webhook.
- [ ] Validar certificados/IPs permitidas del procesador y añadir E2E de activación automática.
- [E] Cargar credenciales Meta, Telegram y Facebook en Key Vault y ejecutar una difusión real controlada.

## 7. Claims, legal y decisiones de producto

- [ ] Completar claim -> evidencia técnica -> evidencia operativa -> aprobación legal -> superficie.
- [ ] Asignar responsable y expiración a cada claim.
- [ ] Separar en UI y marketing `implementado`, `beta`, `draft`, `verificado por PawTrack` y aval externo.
- [ ] Confirmar con Legal el tratamiento de salud, ubicación, fotos, embeddings, comunicaciones y menores/familia.
- [ ] Actualizar términos, privacidad y consentimientos antes de activar superficies institucionales.
- [ ] Decidir recursos importables, formatos, duplicados, límites, retención y reintentos.
- [ ] Decidir política común de downgrade para catálogos, clínicas y municipalidades.
- [ ] Decidir métricas y retención analítica de proveedores.
- [ ] Decidir propiedad, cuotas y apilamiento de promociones.
- [ ] Decidir IVA, add-ons, créditos y reembolsos.
- [ ] Aprobar UX de cuota agotada y upgrade.
- [ ] Aprobar retención de auditoría y acceso privilegiado.
- [ ] Aprobar estrategia de validación local/CI y excepciones operativas.

## 8. Deuda técnica frontend vigente

- [x] Reemplazar el polling de mensajes de chat cuando SignalR está conectado; mantener fallback de 10 s durante reconexión o caída. Los pedidos siguen pendientes hasta contar con un canal de eventos equivalente.
- [x] Añadir Error Boundary por feature o página crítica mediante `RouteShell` y `FeatureErrorBoundary`.
- [x] Añadir skeletons específicos para dashboard, detalle, formularios, directorios y mapa en `RouteShell`; quedan superficies secundarias para una segunda iteración visual.
- [ ] Completar pruebas visuales responsive en móvil, tablet y escritorio.
- [ ] Ejecutar auditoría manual WCAG con teclado y lector de pantalla.
- [ ] Verificar animaciones 3D y fondos animados en dispositivos de bajo rendimiento.

## Evidencia y fuentes relacionadas

- Estado técnico verificado: `dotnet test PawTrack.sln` exit 0 con 1536 pruebas correctas y `npm test -- --run --reporter=dot` exit 0 con 114/114 tests correctos.
- Estado técnico: [STATUS.md](STATUS.md)
- Gobierno de release: [GO_LIVE_GOVERNANCE.md](GO_LIVE_GOVERNANCE.md)
- Autorización: [API_AUTHORIZATION_MATRIX.md](API_AUTHORIZATION_MATRIX.md)
- Proveedores: [EXTERNAL_PROVIDER_VALIDATION.md](EXTERNAL_PROVIDER_VALIDATION.md)
- QA E2E: [GUIA_QA_E2E.md](GUIA_QA_E2E.md)
- Accesibilidad: [GUIA_ACCESIBILIDAD_Y_UX.md](GUIA_ACCESIBILIDAD_Y_UX.md)
- Claims: [CLAIM_EVIDENCE_MATRIX.md](CLAIM_EVIDENCE_MATRIX.md)
- Revisión legal: [LEGAL_REVIEW_REGISTER.md](LEGAL_REVIEW_REGISTER.md)

## Regla de mantenimiento

Toda nueva tarea pendiente se agrega aquí con prioridad, responsable, criterio
de cierre y evidencia esperada. No crear otra lista paralela. Los documentos
históricos eliminados en esta consolidación no deben volver a referenciarse.
