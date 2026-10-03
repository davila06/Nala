# PawTrack CR - Backlog Maestro Enterprise

> **Fuente única de pendientes activos.** Corte: 2026-10-02.
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

> Últimas suites completas locales: backend 1922/1922, incluido el gate SQL sobre una base temporal; PWA 222/222 (70 archivos). Tras el mapeo HTTP 409 se ejecutaron tests de controller 3/3, checkout 2/2, `npm --prefix frontend run build` y landing 53/53. La migración no se aplicó a `PawTrackDev`, staging ni producción; gateway/SINPE real siguen sin prueba.

## Prioridad inmediata

Para `ENT-CLINIC` (2026-09-26): el backfill de `AddClinicOrganizations`
está aplicado solo en local. La migración filtrada de membresías y las pruebas
de modelo/LocalDB están implementadas, pero faltan staging, matriz dinámica
BOLA por recurso/actor/sede, homologación fiscal, confirmación real de entrega
de mensajes y pilotos de disposición a pagar. Los permisos de staff/finanzas
son por clínica; pertenecer a la organización no da acceso a datos de un sitio.

| ID         | Área                        | Estado | Criterio de cierre                                                                                                |
| ---------- | --------------------------- | ------ | ----------------------------------------------------------------------------------------------------------------- |
| ENT-CI     | Gates de CI y release       | `[~]`  | Workflow único verde con artefactos, lockfiles, pruebas, SBOM y política de merge                                 |
| ENT-API    | Contratos y autorización    | `[~]`  | OpenAPI versionado, matriz completa y suites BOLA/IDOR verdes                                                     |
| ENT-FND    | Finder sin login            | `[x]`  | Reporte seguro en menos de 30 segundos, antifraude, PII y offline verificados                                     |
| ENT-E2E    | Ciclo pérdida-reunificación | `[~]`  | E2E limpio con notificaciones, eventos de producto y proveedores controlados                                      |
| ENT-CLINIC | Uso diario en clínicas      | `[~]`  | Agenda, consulta, inventario, caja, comunicación y analítica enterprise                                           |
| ENT-STORE  | Uso diario en tiendas       | `[ ]`  | Piloto híbrido de una sede; pedidos idempotentes, inventario trazable, caja/fiscalidad externa y gates de rollout |
| ENT-CLAIM  | Claims y legal              | `[~]`  | Cada claim tiene evidencia, responsable, expiración y aprobación                                                  |
| COMP-AI    | IA-first operativa          | `[ ]`  | Copiloto/agentes con herramientas, aprobación, evaluación y Responsible AI                                        |
| COMP-NET   | Liderazgo territorial       | `[ ]`  | 2-3 cantones con densidad, outcomes y partners verificables                                                       |
| MKT-AZURE  | Azure Marketplace           | `[ ]`  | SaaS offer, fulfillment, tenant mapping, seguridad y private preview                                              |
| ENT-PROV   | Proveedores externos        | `[E]`  | Contratos, secretos, smoke tests de staging y rotación aprobados                                                  |

La direccion de producto de `ENT-STORE` es un modelo hibrido por etapas. En el
codigo actual ya hay stock escalar, reserva/expiracion, registro manual de
reporte/verificacion de pago externo e idempotencia de creacion de pedidos con
`Idempotency-Key`; no equivalen a kardex, POS, pago automatizado, conciliacion
ni despliegue. La migracion de idempotencia/reembolso se probó en una base SQL
temporal desechable, pero no se aplicó a una base persistente;
los gates y limites estan en
[ROADMAP_TIENDAS_USO_DIARIO.md](ROADMAP_TIENDAS_USO_DIARIO.md). La migracion de
stock/pago y su operacion en un entorno compartido siguen `NO_VERIFICADO`.

## 1. Calidad, CI y release

- [ ] Registrar por release commit, migraciones, resultados, artefactos y aprobadores.
- [ ] Asignar propietario y fecha de expiración a cada gate y evidencia.
- [x] Ejecutar restore reproducible de .NET con `packages.lock.json` y `--locked-mode`; verificado localmente el 2026-09-23.
- [x] Ejecutar build Release con `--no-restore` y warnings tratados como errores; verificado localmente el 2026-09-23.
- [x] Validar la suite backend principal de forma local con `dotnet test PawTrack.sln --no-restore --verbosity minimal -p:BaseOutputPath=backend/test-out/`; resultado: exit 0, 1922/1922 pruebas.
- [x] Validar la suite frontend principal de forma local con `npm --prefix frontend test -- --run --reporter=dot`; resultado: exit 0, 222/222 pruebas en 70 archivos.
- [ ] Ejecutar `npm ci` con versión de Node fijada y lockfile validado.
- [ ] Ejecutar typecheck, lint, build, Vitest y Playwright en CI limpio.
- [ ] Publicar TRX, cobertura, OpenAPI, migraciones, SBOM y reportes Playwright.
- [x] Validar migración en base SQL temporal vacía y upgrade incremental preservando un pedido legacy; `StoreOrderIdempotencySqlTests` aplica todas las migraciones en LocalDB temporal, comprueba los campos nullable y verifica que un insert concurrente con la misma clave es rechazado. La base se elimina al final; no valida staging/producción.
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

## 4A. Competitividad e IA-first

- [ ] Implementar copiloto de recuperación con herramientas limitadas por rol,
      aprobación humana y auditoría.
- [ ] Crear benchmark consentido de matching visual con precision/recall,
      calibración, falsos positivos y métricas por especie/territorio.
- [ ] Añadir explicaciones de matching y de proyección sin presentar una
      predicción como certeza.
- [ ] Crear registro de modelos, datasets, prompts, evaluaciones, drift,
      fairness, rollback y red-team.
- [ ] Construir RAG regional con fuentes legales/operativas versionadas y
      citación.
- [ ] Cerrar escalamiento humano y SLA para casos críticos antes de prometer
      hotline o recuperación 24/7.
- [ ] Activar identidad portable QR/NFC/microchip con exportación verificable y
      API versionada.

> Matriz competitiva vigente: [COMPETITIVE_INTELLIGENCE_2026-09-23.md](COMPETITIVE_INTELLIGENCE_2026-09-23.md).

## 4B. Azure Marketplace y canal institucional

- [ ] Elegir oferta inicial `NALA Recovery & Care Cloud` y unidad de cobro por
      tenant antes de introducir metering por evento.
- [ ] Implementar persistencia de `MarketplaceSubscription` y mapping a tenant,
      plan y entitlements.
- [ ] Implementar SaaS fulfillment idempotente: activate, update, suspend,
      reinstate y cancel.
- [ ] Implementar landing page, onboarding, reconciliación y alertas de
      discrepancia.
- [ ] Preparar SSO/RBAC, soporte, SLA, DR, SBOM, seguridad y privacidad para
      compradores institucionales.
- [ ] Ejecutar private preview con 2-3 organizaciones ancla en Costa Rica.
- [ ] Completar publicación, certificación, private offers, partner/CSP y
      co-sell readiness en Partner Center.

> Plan de implementación: [AZURE_MARKETPLACE_GO_TO_MARKET.md](AZURE_MARKETPLACE_GO_TO_MARKET.md). Hoy no existe integración Marketplace en el código.

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

## 9. ENT-STORE: piloto híbrido y uso diario

La direccion de producto es empezar con una tienda y una sede. El POS/terminal y
proveedor fiscal existentes conservan autoridad sobre cobro, caja y comprobante;
NALA solo puede ser autoridad de existencias si todas las operaciones que las
cambian quedan registradas en NALA. Esta direccion no representa aprobacion
legal/fiscal, contrato, piloto seleccionado ni despliegue.

- [ ] Aprobar responsables, tienda piloto, autoridad por dato, flujo de venta presencial y politicas locales antes de habilitar stock como real.
- [~] Implementar idempotencia de `POST /api/store-orders`: header requerido, hash de payload, replay idéntico, rechazo de payload distinto, índice único por cliente y retry frontend. Migración y carrera de índice probadas en LocalDB temporal; falta aplicar con aprobación a un entorno compartido y validar allí rollback/operación.
- [ ] Cerrar transiciones por retiro/entrega, actor/motivo/historial y reconfirmacion de cambios de precio.
- [ ] Implementar kardex y recepcion/conteo/ajuste/merma/devolucion en una sede; verificar atomicidad SQL, reservas y conciliacion de stock.
- [ ] Definir registro operativo de ventas y cierres mientras el POS externo procesa pagos y emite comprobantes; no presentar el endpoint manual como confirmacion bancaria.
- [ ] Fortalecer importacion CSV/JSON con SKU/stock, vista previa, progreso, errores descargables, lotes y pruebas de tenant/duplicados.
- [ ] Entregar notificaciones durables de pedidos/estados, dashboard diario, alertas, runbooks y observabilidad sin PII.
- [ ] Agregar roles de empleados y sucursales solo despues del gate de una sede; crear autorizacion BOLA tienda x sede y pruebas.
- [ ] Pasar pruebas de concurrencia SQL, HTTP, seguridad, E2E del comprador/tienda y UAT piloto; probar migracion/rollback y feature flag antes del rollout.

El detalle, dependencias y gates de salida estan en el roadmap; este checklist es
la fuente unica de tareas activas y se cerrara solo con implementacion y evidencia.

## Evidencia y fuentes relacionadas

- Estado técnico verificado al 2026-10-02: suites completas backend 1922/1922 y PWA 222/222; prueba SQL temporal de migración/idempotencia 1/1; controller 3/3, checkout 2/2, landing 53/53, build PWA y typecheck correctos. No se aplicó la migración a base persistente ni se verificaron proveedores, staging o producción.
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
