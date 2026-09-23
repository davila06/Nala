# PawTrack CR - Roadmap de Implementación Enterprise

> Corte: 2026-09-23  
> Estado: backlog operativo vigente  
> Fuente de estado: [STATUS.md](STATUS.md)  
> Fuente de gobierno: [GO_LIVE_GOVERNANCE.md](GO_LIVE_GOVERNANCE.md)

Este documento consolida las tareas técnicas y operativas necesarias para
llevar las capacidades actuales a un nivel enterprise verificable. No convierte
una capacidad implementada en una promesa comercial: cada cierre exige código,
autorización, pruebas, documentación y evidencia reproducible.

## Estados y reglas

- `[ ]` Pendiente.
- `[~]` En progreso o parcialmente implementado.
- `[x]` Implementado y validado con evidencia.
- `[E]` Bloqueado por evidencia externa, contrato o aprobación legal.

Una tarea solo puede pasar a `[x]` cuando incluye:

1. Implementación o configuración reproducible.
2. Prueba automatizada o smoke test apropiado.
3. Control de seguridad/autorización cuando corresponda.
4. Documentación y runbook actualizados.
5. Evidencia fechada en CI, staging o producción controlada.

## Tablero ejecutivo

| ID        | Área                      | Prioridad | Estado actual | Cierre requerido                                |
| --------- | ------------------------- | --------- | ------------- | ----------------------------------------------- |
| ENT-CI    | Gates CI obligatorios     | P0        | `[~]`         | Workflow único verde con artefactos             |
| ENT-API   | Contratos y hardening API | P0        | `[~]`         | OpenAPI diff, tipos, BOLA y DTO review          |
| ENT-SIG   | E2E SignalR               | P1        | `[~]`         | Dos clientes reales y expiración verificada     |
| ENT-FND   | Finder sin login          | P0        | `[~]`         | Flujo seguro, antifraude, offline y PII tests   |
| ENT-MET   | North Star y funnel       | P1        | `[~]`         | Métricas por periodo/cantón/canal con evidencia |
| ENT-OBS   | Observabilidad y SLO      | P1        | `[~]`         | Métricas, alertas y dashboard operativo         |
| ENT-PROV  | Proveedores externos      | P0        | `[~]`         | Gate técnico verde + contratos reales           |
| ENT-CLAIM | Claims y legal            | P0        | `[~]`         | Matriz de claims aprobada y vigente             |

## Fase 0 - Preparación y control de cambios

- [x] Confirmar `STATUS.md` como fuente técnica canónica.
- [x] Confirmar `PRICING_AND_PLANS.md` como fuente comercial subordinada a código.
- [x] Confirmar `EXTERNAL_PROVIDER_VALIDATION.md` como gate de proveedores.
- [x] Confirmar que evidencia local de proveedores está ignorada por Git.
- [ ] Crear un registro de release con commit, migraciones, resultados y aprobadores.
- [ ] Definir propietario operativo por cada gate y fecha de expiración de la evidencia.
- [ ] Verificar que cada cambio de capacidad actualice código, tests, documentación y soporte.

## ENT-CI - Gates CI obligatorios

### Workflow y build

- [x] Crear un workflow único de calidad enterprise con jobs separados para backend, frontend, seguridad y contratos.
- [ ] Ejecutar `dotnet restore` con lock/reproducibilidad definida.
- [ ] Ejecutar `dotnet build PawTrack.sln --configuration Release --no-restore`.
- [ ] Fallar ante warnings en Release (`TreatWarningsAsErrors` ya está configurado en `Directory.Build.props`).
- [ ] Ejecutar `npm ci` con lockfile y Node version fijada.
- [ ] Ejecutar `npm run typecheck` para app, worker y configuración Node.
- [ ] Ejecutar `npm run lint -- --max-warnings 0`.
- [ ] Ejecutar `npm run build`.

### Pruebas y artefactos

- [ ] Ejecutar unit tests backend y publicar TRX.
- [ ] Ejecutar integration tests backend con servicios efímeros y publicar TRX.
- [ ] Ejecutar tests frontend Vitest y publicar cobertura.
- [ ] Ejecutar Playwright en frontend preview, no contra Vite dev.
- [ ] Publicar traces, screenshots, videos y reportes Playwright en fallos.
- [ ] Publicar OpenAPI, resultados de migración y SBOM como artefactos.
- [ ] Bloquear merge si falla cualquier gate P0.

### Seguridad y migraciones

- [x] Ejecutar Gitleaks en CI.
- [x] Ejecutar npm audit y análisis NuGet en CI.
- [ ] Fijar severidad, excepciones con expiración y proceso de aprobación de vulnerabilidades.
- [ ] Ejecutar `Test-ExpandContractMigrations.ps1` contra la referencia base.
- [ ] Validar migraciones en base vacía y upgrade incremental.
- [ ] Verificar que ninguna credencial aparezca en logs o artefactos.

**Evidencia de cierre:** workflow verde en rama protegida, artefactos descargables y resumen de gates asociado al commit.

## ENT-API - Contratos, BOLA/IDOR y minimización

### OpenAPI

- [~] Generar tipos TypeScript desde `/openapi/v1.json` como artefacto CI; falta consumirlos para reemplazar tipos manuales del frontend.
- [x] Guardar snapshot aprobado de OpenAPI en `docs/openapi-v1.surface.json`.
- [x] Ejecutar diff automático con `scripts/Test-OpenApiBreakingChanges.ps1` y bloquear breaking changes sin nueva versión.
- [x] Validar Redocly/OpenAPI lint en CI.
- [ ] Verificar versionado de endpoints públicos y Partner.
- [ ] Documentar deprecación, sunset y compatibilidad por versión.

### Autorización

- [x] Aplicar allowlist `Owner` para crear/editar mascotas.
- [x] Bloquear clínicos, municipalidades y otros roles no propietarios en `POST /api/pets`.
- [ ] Completar matriz endpoint -> rol -> ownership -> tenant -> PII -> rate limit.
- [~] Suites BOLA de clínicas: aislamiento de APIs de clínica y creación de mascotas por Owner están cubiertos; falta matriz sistemática de todos los recursos clínicos.
- [x] Suites BOLA de collares: status/history/location, ownership y rechazo de atacante están cubiertos en integración.
- [~] Reportes institucionales tienen pruebas de cantón, perfil municipal y organización cross-tenant; faltan suites equivalentes para todas las descargas/exportaciones Partner.
- [ ] Añadir casos cross-tenant con IDs válidos de otro tenant.
- [ ] Verificar autorización también en exportaciones, descargas y websockets.
- [~] Revisar endpoints públicos para minimizar campos, coordenadas y PII; el endpoint de hallazgos ya redondea coordenadas.
- [~] Añadir pruebas que bloqueen regresiones de PII en DTOs públicos; la proyección de hallazgos tiene regresión de coordenadas.

**Evidencia de cierre:** snapshot OpenAPI aprobado, diff sin breaking changes no versionados y suite BOLA verde.

## ENT-SIG - E2E de coordinación SignalR

- [x] Requerir consentimiento explícito antes de compartir ubicación.
- [x] Emitir precisión aproximada por defecto.
- [x] Permitir opt-in de precisión exacta.
- [x] Mostrar contador de destinatarios conectados.
- [x] Implementar `StopLocationSharing` visible.
- [x] Persistir sesiones sin coordenadas.
- [x] Expirar sesiones con TTL y job distribuido.
- [x] Auditar `Started`, `Stopped` y `Expired` sin latitud/longitud.
- [x] Crear fixture Playwright con dos usuarios autenticados en el mismo `lostEventId`.
- [ ] Verificar que un participante no autorizado no recibe broadcasts.
- [ ] Verificar que el destinatario ve precisión aproximada.
- [ ] Verificar contador al entrar/salir un participante.
- [ ] Verificar stop-sharing inmediato y ausencia del siguiente broadcast.
- [ ] Verificar expiración y auditoría sin coordenadas en base de datos.
- [ ] Verificar reconexión SignalR y rejoin sin reactivar consentimiento implícito.

**Evidencia de cierre:** Playwright con dos navegadores, captura de estados y consulta de auditoría sin coordenadas.

## ENT-FND - Finder sin login

### Flujo funcional

- [x] Mantener perfil QR público mínimo.
- [x] Permitir reporte de mascota encontrada sin cuenta.
- [ ] Capturar foto opcional con validación de tipo, tamaño y firma.
- [ ] Capturar ubicación aproximada, nunca domicilio ni coordenada exacta del dueño.
- [ ] Capturar timestamp con límites razonables de futuro/pasado.
- [ ] Sanitizar notas y mensajes antes de persistir o reenviar.
- [x] Mostrar consentimiento contextual antes de ubicación/foto/contacto.
- [x] Exigir `PrivacyConsent` también en el comando/backend, no solo en la UI.
- [ ] Informar retención y propósito de datos al reportante.

### Abuso y seguridad

- [~] Aplicar rate limit por IP mediante la política `sightings`; faltan dispositivo y fingerprint de riesgo.
- [ ] Añadir CAPTCHA/risk challenge escalonado, no obligatorio para cada usuario de bajo riesgo.
- [~] Detectar duplicados por teléfono normalizado, ubicación aproximada y ventana de 15 minutos; faltan hash de foto y scoring de spam.
- [ ] Añadir moderación de contenido y cola de revisión.
- [ ] Bloquear payloads grandes, tipos MIME falsos y contenido activo.
- [ ] Añadir pruebas de PII, SSRF, XSS, abuso de relay y enumeración de mascotas.
- [ ] Verificar que mensajes anónimos no revelen teléfono, correo o dirección del dueño.
- [ ] Añadir fallback controlado a WhatsApp/SMS/email solo con proveedor validado.

### Offline y resiliencia

- [ ] Encolar reporte offline con idempotency key.
- [ ] Cifrar datos sensibles en cola local.
- [ ] Limitar retención de cola y permitir eliminación manual.
- [ ] Reintentar con backoff y no duplicar reportes.
- [ ] Mostrar estado pendiente, enviado o fallido sin perder evidencia.

**Evidencia de cierre:** suite de abuso, PII, payloads grandes, offline/retry y prueba manual en red lenta.

## ENT-MET - Métricas y North Star

### Eventos

- [x] Emitir `QrActivated` desde la descarga explícita del QR para colocarlo en el collar.
- [x] Emitir `FirstResponseRecorded` de forma idempotente por `lostEventId`.
- [x] Correlacionar pérdida, respuesta, handover y reunificación por incidente.
- [ ] Revisar eventos históricos sin `CorrelationId` y etiquetarlos como legacy.
- [ ] Validar esquema, source, timestamp y deduplicación en CI.

### Funnel

- [ ] Registro -> foto -> contacto -> QR -> activación.
- [ ] QR generado -> activado -> escaneado -> contacto seguro.
- [ ] Pérdida -> primer avistamiento -> primera respuesta -> reunificación.
- [x] Calcular p50/p90 por cohorte, cantón, periodo, canal y especie; exponer dimensiones y percentiles en dashboard.
- [x] Medir recuperación, tiempo de respuesta y tiempo de reunificación sin mezclar incidentes.
- [x] Calcular mascotas activas protegidas a 30/90/180 días.
- [ ] Documentar exclusiones, datos faltantes y censura estadística.

### Productos y permisos

- [x] Dashboard Admin de funnel.
- [x] Export CSV agregado sin PII.
- [ ] Export Partner con identidad M2M canónica y scopes.
- [ ] Añadir autorización, auditoría y cuota a exportaciones Partner.
- [ ] Crear pruebas de tenant, rango, cantón y minimización.

**Evidencia de cierre:** consultas SQL reproducibles, dashboard, export autorizado y definición aprobada de North Star.

## ENT-OBS - Observabilidad y SLO

### Instrumentación

- [ ] Definir nombres, unidades y cardinalidad máxima de métricas OpenTelemetry.
- [x] Instrumentar requests con correlation ID end-to-end.
- [x] Instrumentar métricas HTTP de latencia y errores con etiquetas de baja cardinalidad.
- [ ] Instrumentar latencia, errores y retries de proveedores externos.
- [ ] Instrumentar ingestión, jobs, outbox, SignalR y colas offline.
- [ ] Añadir métricas de negocio del funnel y North Star.
- [ ] Añadir logs estructurados sin PII, tokens ni coordenadas exactas.

### Health y resiliencia

- [x] Mantener `/health/live` y `/health/ready`.
- [x] Añadir health checks específicos de SQL y Blob; Key Vault, correo, pagos, GPS y broadcast siguen pendientes.
- [ ] Clasificar dependencia crítica, degradable y opcional.
- [ ] Verificar startup fail-fast cuando falte una dependencia crítica.
- [ ] Definir timeouts, retries, circuit breakers y fallback por proveedor.

### SLO y alertas

- [ ] Definir SLO de API, jobs, webhooks, broadcast, finder y reunificación.
- [ ] Calcular burn rate y error budget.
- [ ] Crear queries KQL y alertas como código.
- [ ] Crear dashboard Azure Monitor por servicio, tenant y región.
- [ ] Definir on-call, severidad, escalamiento y runbook por alerta.
- [ ] Ejecutar prueba controlada de alerta y registrar evidencia.

**Evidencia de cierre:** dashboard, alertas activas, queries versionadas y simulación de incidente.

## ENT-PROV - Proveedores externos y Azure

### Código y pruebas

- [x] Mantener adaptadores separados para Azure, WhatsApp, GPS, pagos y correo.
- [x] Mantener gate `Test-ExternalProviderReadiness.ps1`.
- [ ] Añadir smoke test de staging para cada proveedor.
- [ ] Añadir contratos de timeout, retry, idempotencia y error mapping.
- [ ] Validar rotación sin downtime de cada credencial.
- [x] Verificar presencia de configuración externa sin imprimir secretos mediante `external-provider-configuration` health check.

### Evidencia externa

- [ ] Azure: suscripción, resource group, managed identity, DPA, presupuesto y alertas aprobados.
- [ ] WhatsApp: Business Manager, número, templates, webhook firmado y límites aprobados.
- [ ] GPS: contrato/OEM, sandbox, límites API, precisión, retención y soporte aprobados.
- [ ] Payments: gateway/SINPE, sandbox cobro/refund, conciliación, términos y responsable aprobados.
- [ ] Email: dominio, SPF/DKIM/DMARC, SendGrid, rebotes y límites aprobados.
- [ ] Ejecutar gate con evidencia real de cada proveedor.
- [ ] Programar expiración y revisión trimestral de la evidencia.

**Evidencia de cierre:** archivo de evidencia redactada, smoke tests de staging y aprobación operacional.

## ENT-CLAIM - Claims y legal

### Código/documentación

- [x] Retirar claims públicos de recuperación, usuarios y mercado sin fuente.
- [x] Separar `implementado`, `beta`, `verificado por PawTrack` y aval externo.
- [x] Alinear Markdown y HTML generados con las fuentes canónicas.
- [x] Crear matriz claim -> evidencia técnica -> evidencia operativa -> aprobación legal -> superficies en `docs/CLAIM_EVIDENCE_MATRIX.md`.
- [ ] Añadir expiración y responsable a cada claim.
- [ ] Gatear claims en UI/marketing por configuración aprobada.

### Aprobación externa

- [ ] Legal/privacy aprobar tratamiento de salud, ubicación, fotos, embeddings, comunicaciones y menores.
- [ ] Legal aprobar lenguaje `SENASA-ready` y ausencia de integración oficial.
- [ ] Comercial aprobar precios, SLA, patrocinio y métricas publicables.
- [ ] Release manager verificar `GO_LIVE_GOVERNANCE.md` antes de publicar.

**Evidencia de cierre:** matriz firmada, referencias de aprobación y revisión de superficies web/documentales.

## Criterios de salida enterprise

- [ ] Todos los gates CI son obligatorios y publican artefactos.
- [ ] Backend y frontend compilan/testean en un runner limpio.
- [ ] No existen accesos BOLA/IDOR críticos conocidos.
- [ ] Finder sin login es seguro, medible y resiliente offline.
- [ ] SignalR tiene E2E multiusuario y expiración verificada.
- [ ] North Star puede calcularse desde eventos reales y documentados.
- [ ] SLO, alertas y on-call están activos en Azure.
- [ ] Proveedores tienen contratos, credenciales y smoke tests vigentes.
- [ ] Claims tienen evidencia y aprobación legal/comercial.
- [ ] Release manager firma el checklist de go-live.

## Orden recomendado

1. Gates CI y OpenAPI.
2. BOLA/IDOR y minimización de DTOs.
3. E2E SignalR.
4. Finder sin login y antifraude.
5. Eventos faltantes y North Star.
6. Observabilidad y SLO como código.
7. Validación real de proveedores y aprobación legal.
