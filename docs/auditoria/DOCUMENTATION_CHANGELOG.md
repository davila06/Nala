# Changelog documental de auditoría

## 2026-09-27

- Orquestador: creado [plan](DOCUMENTATION_UPDATE_PLAN.md), [alcance](../PRODUCT_SCOPE.md), [matriz](FEATURE_TRACEABILITY_MATRIX.md) y [reporte de brechas](DOCUMENTATION_GAP_REPORT.md); actualizado [índice](../README.md) sin borrar históricos.
- Inventario: agregada nota de corte en [FEATURES](../FEATURES.md), conservando la matriz objetivo y sus decisiones de producto.
- Arquitectura y seguridad: añadidos [resumen](../ARCHITECTURE.md) y [controles](../SECURITY.md) con subdocumentos en `docs/architecture/` y `docs/security/`; no se modificó infraestructura ni software.
- API y planes: creados [API](../API.md), [comercial](../commercial/PLANS.md), catálogos auxiliares y [CONFIGURATION](../CONFIGURATION.md), [DATA_MODEL](../DATA_MODEL.md), [INTEGRATIONS](../INTEGRATIONS.md), [TESTING](../TESTING.md), [KNOWN_LIMITATIONS](../KNOWN_LIMITATIONS.md).
- Especialistas: nuevos documentos bajo `docs/domains/`, `docs/editions/`, `docs/growth/`, `docs/ai/` y `docs/business/`, con diferencias entre capacidades y propuestas.
- Archivados: ninguno; [README](../README.md) preserva la clasificación histórica de documentos previos. Fuentes anteriores no se reemplazaron de manera silenciosa.

Cada afirmación nueva usa evidencia de código enlazada en su documento. Pruebas de integración, despliegue y proveedores siguen pendientes ([TESTING](../TESTING.md)); este changelog no los acredita.

## 2026-09-27 — AI Knowledge Hub

- Creado `/ai/` como hub Evidence First con contexto, inventario técnico, producto/historia, arquitectura, dominios, integraciones, seguridad, releases, siete prompts reutilizables, scorecard y gobernanza de agentes.
- Creados `docs/adrs/` con siete ADR en estado `DRAFT`; son transcripciones rastreables de la sección ADR del manual técnico, no decisiones formalmente aprobadas.
- Creados `docs/business/ONTOLOGY.md`, `docs/business/CAPABILITIES.md`, `docs/strategy/` y `docs/compliance/` como síntesis/propuestas con evidencia y límites explícitos.
- Actualizado el índice [docs/README.md](../README.md) para descubrir el hub. No se modificó código productivo ni se ejecutaron pruebas de producto, migraciones o despliegues.
- Validación documental final: 67 archivos revisados, 0 enlaces locales rotos, 0 problemas de formato básico y 30 capacidades asignadas una vez cada una. Diagnóstico del editor sin errores en los documentos comprobados; `git diff --check` limpio. Scorecard cualitativo 63/100; no certifica readiness de producción.

## 2026-09-28 — Revalidación focal de entitlements

- `FEATURES.md` §24: se actualizaron checks obsoletos y se clasificaron modelo/configuración, migraciones, servicio, snapshot, idempotencia, gates por dominio, UI, cuotas y aprobaciones con evidencia relativa.
- Trazabilidad F12 se cambió a `PARCIALMENTE_IMPLEMENTADO`: el servicio y varias rutas están activos, pero permanece fallback legacy y no existe CRUD verificado de definiciones de entitlement.
- `DOCUMENTATION_GAP_REPORT.md` registra medidor sin montar, ausencia de alertas de umbral, gates por dominio incompletos y límites de validación legal/comercial/producción.
- Validación del código vigente: build de `PawTrack.sln` aprobado; suite backend 1,803/1,803 (1,634 unitarias + 169 integración) aprobadas. No se ejecutaron migraciones ni despliegues.

## 2026-09-28 — Auditoría de alcance real

- Se actualizó [PRODUCT_SCOPE](../PRODUCT_SCOPE.md) con la matriz vigente de
  QR, NFC, GPS, telemedicina, expediente, recordatorios, suscripciones,
  marketplace, IA, municipalidades, refugios, veterinarias e integraciones.
- Se actualizaron [FEATURES](../FEATURES.md) y la [matriz de trazabilidad](FEATURE_TRACEABILITY_MATRIX.md)
  para separar implementación real, parcial, propuesta y capacidades ausentes.
- Se documentó que ACS, Dynamics 365 y Power Platform no tienen SDK, código,
  configuración ni endpoints en el repositorio. Se separaron adaptadores Azure
  de operación desplegada y se registraron límites de proveedor/hardware.
- Se corrigieron cifras de verificación: 1,634 unitarias, 169 integración y
  147 frontend; no se certifican staging, producción ni proveedores externos.

## 2026-09-28 — Auditoría integral de producto y estrategia

- Creados [estado real](../NALA_ACTUAL_PRODUCT_STATE.md) y [mapa de capacidades](../NALA_CAPABILITY_MAP.md), con estados separados para implementación, parcialidad, propuesta y operación externa no verificada.
- Creados [brechas competitivas](../NALA_COMPETITIVE_GAP_ANALYSIS.md), [estrategia Costa Rica](../NALA_CR_MARKET_STRATEGY.md), [expansión LATAM](../NALA_LATAM_EXPANSION.md) y [visión global](../NALA_GLOBAL_VISION.md). Las comparaciones y recomendaciones están marcadas como hipótesis estratégicas sujetas a validación.
- Creados [estrategia IA](../NALA_AI_STRATEGY.md) y [estrategia de monetización](../NALA_PRICING_STRATEGY.md), distinguiendo matching visual, entitlements y pagos existentes de RAG, copilotos, telemedicina, payout y claims comerciales aún no demostrados.
- Creada la [auditoría documental](../DOCUMENTATION_AUDIT.md) y el [resumen ejecutivo](../NALA_EXECUTIVE_SUMMARY.md). No se eliminaron documentos históricos ni se modificó código, infraestructura o pruebas.

## 2026-09-28 — Pricing, planes y monetización

- Creados 11 informes pedidos: inventario de features, mapeo de tiers, puntuación heurística por plan, análisis competitivo, precios CR/LATAM, roadmap de monetización, escenarios de unit economics, auditoría documental de pricing, resumen ejecutivo y matriz maestra de features/planes/upsell.
- La [matriz maestra](../NALA_FEATURE_PLAN_UPSELL_MATRIX.md) es navegación de estrategia, no fuente de enforcement. Se separa tier actual de paquete recomendado y se marca cada cuota sin datos como `REQUIERE VALIDACIÓN CON DATOS DE USO`.
- Actualizado [índice](../README.md) para enlazar los informes nuevos y corregido [PRICING_AND_PLANS](../PRICING_AND_PLANS.md): UserFamilia tiene límite técnico `MaxPets=25`, no “ilimitado”, según migración y fallback legacy.
- Preservados pricing briefs anteriores; no se borró/archive ningún documento. Auditoría de `docs/` contabiliza 204 Markdown, pero deja `SIN EVIDENCIA` los archivos que no se leyeron en esta ampliación.
- Competidores se contrastaron con páginas oficiales accesibles el 2026-09-28; varios sitios redirigieron, bloquearon o no dieron contenido. World Bank PPA se usa como indicador direccional, no como medida de elasticidad ni ingreso disponible.
- La tarea VS Code `test-backend` terminó con código de salida 0, pero el resumen de conteos no quedó recuperable; no se documentan cantidades por proyecto. Azure Retail Pricing para SQL `GP_S_Gen5_1`/East US devolvió cero filas. No se afirma costo real, CAC, LTV, margen, break-even, venta, producción ni aprobación legal/comercial.
- Revalidación: `Test-GoLiveGovernance.ps1` corre en el workflow de despliegue productivo antes de publicar artefactos; no se encontró enforcement de las flags en endpoints de compra, catálogo o UI. El trial Verified de 30 días en primera aprobación se confirmó en `ServiceProvider.Activate()` y no se reinicia al reactivar.

## 2026-09-28 — P0 Enterprise+ de Familia, Clínicas y Billing

- Familia: Free ahora tiene una plaza `MaxFamilyMembers` (titular); las invitaciones pendientes reservan cupos, la aceptación revalida plan/cupo/duplicado y ambos flujos serializan cambios mediante locks distribuidos SQL por cuenta/usuario. El DTO/API devuelve conteo y máximo de invitaciones pendientes; la PWA usa `MaxFamilyMembers` y `MaxPets` del entitlement, no números `5`/`ilimitado` hardcodeados.
- Clínicas: el endpoint independiente de scan QR/RFID registra el contacto; una consulta clínica QR/RFID sólo identifica y requiere grant activo `read` para historial o `write` para registrar. Se actualizó auditoría de accesos, mensajes, OpenAPI/guía QR y pruebas de denegación/permiso por scope.
- Billing: renovación usa el `AmountCrc` aceptado en la suscripción, no una lista estática cambiante. Las suscripciones promocionales de importe cero no se auto-cobran.
- Catálogo/UI: Freemium, PlanGate, Dashboard, perfil y selector de promociones usan precios del catálogo activo o el importe contratado; sin catálogo no se inventa tarifa. Se quitaron promesas de IA/movimiento/mascotas ilimitadas y bundle GPS no verificado.
- Validación final backend: **1.814/1.814** (1.645 unitarias + 169 integración). Frontend: **168/168 pruebas, 58/58 archivos**. Typecheck: exit code 2 por 12 TS6133 en `frontend/src/features/lost-pets/pages/ReportLostPage.tsx`, archivo fuera del P0. Los tests no validan `sp_getapplock` contra SQL Server. Pruebas P0 focales de familia, clínica, pricing y dashboard pasan.
- Limitaciones: no se añadieron roles clínicos/Family Viewer nuevos ni se ejecutó piloto externo, cotización/proveedor de hardware o carga en producción. El contenido de este changelog no certifica rollout.

## 2026-09-28 — Revisión P2 de eventos de crecimiento

- Actualizado [GROWTH_EVENTS](../growth/GROWTH_EVENTS.md) con destinos actuales de eventos, identificadores, fanout, retención configurable y controles de consentimiento/emergencia respaldados por código y pruebas frontend.
- Los nuevos eventos de activación, correlación de funnel y variantes de upsell siguen `PROPUESTO`/`REQUIRES_HUMAN_APPROVAL`: privacidad y producto deben aprobar campos, supresión en todos los destinos, retención y criterios de experimento antes de activarlos. No se afirma despliegue ni resultados comerciales.

## 2026-10-01 — Auditoría de contenido público del landing

- Creado [LANDING_CONTENT_AUDIT](LANDING_CONTENT_AUDIT.md) con alcance, fuentes, claims, límites, prioridades y aprobaciones pendientes. Revisión dirigida; no cubre semánticamente todos los documentos ni producción.
- Actualizado `ai/03_domains/nfc.md`, `PRODUCT_SCOPE`, `FEATURES`, `FEATURE_TRACEABILITY_MATRIX`, `NALA_CAPABILITY_MAP` y `DOCUMENTATION_GAP_REPORT`: NFC tiene guía de configuración manual y SKU modelado, sin integración nativa ni operación de hardware verificada.
- Registrados en la matriz de claims los bloqueadores de contacto de caso activo y NFC, además de la aprobación legal pendiente de PawTrack CR/NALA. Login y landing usan la denominación indicada por el solicitante; documentos legales no se modificaron.
- El backend `Round23SecurityRegressionTests` pasó 6/6; acredita JWT/rate limit del contacto, no control de ownership ni privacidad por propietario. No se cambió backend ni API.
- Cambios de marketing se describen en el informe; términos, política y aprobaciones siguen `draft`/pendientes. No se ejecutó despliegue.

## 2026-10-01 — Catálogo de planes y gate de aprobación comercial

- `SubscriptionPricing` ya no mantiene una segunda tabla de importes; conserva reglas de cálculo, IVA y clasificación de tiers. Nuevas suscripciones usan el importe persistido en `SubscriptionPlans`; se añadió prueba con precio divergente de la referencia histórica.
- Planes ahora registran referencia de aprobación, Admin y fecha; editar/desactivar revoca la aprobación. Se agregó endpoint Admin para aprobar/revocar, DTO público sin evidencia interna y filtro del catálogo por plan activo y aprobado.
- El gate se aplica a nuevas compras, promociones, downgrades y activaciones SINPE/Admin. La migración `AddSubscriptionPlanCommercialApproval` está generada, no aplicada; planes existentes no se aprobaron ni se sembraron automáticamente.
- Pruebas: 31 unitarias de pricing/alta; 25 unitarias de dominio/compra/promoción/downgrade/activación; integración focal del catálogo cubre oculto → aprobar → publicar → revocar → ocultar → reaprobación → desactivar.
- Pendiente de rollout: aplicar migración solo con autorización; revisar referencias legales/comerciales de cada tier, volver a aprobar en Admin los que correspondan, decisión de renovaciones automáticas tras revocación y validación del entorno productivo. No se modificó `PawTrackDev` ni otra base.

## 2026-10-01 — Fuente única de precios y fail-closed comercial

- Retirada la lista de importes por tier de `SubscriptionPricing`; quedan reglas de períodos/descuento, clasificación de tiers e IVA. El precio de suscripción nuevo se consulta de `SubscriptionPlans`; prueba un caso donde la fila vale ₡3.000 aunque la referencia histórica fuera ₡2.990.
- Añadidos estado y evidencia por plan (referencia identificadora, usuario Admin, timestamp), endpoints Admin de aprobación/revocación, DTO público sin metadata interna y filtros de catálogo.
- Aprobación requerida en alta, promoción gratuita/descuento, downgrade y activaciones SINPE/Admin. Editar o desactivar revoca el approval; la renovación existente conserva el importe ya aceptado.
- Agregada migración `AddSubscriptionPlanCommercialApproval`; **generada, no aplicada**. Las filas existentes serán no aprobadas; no se precargaron aprobaciones ni se modificó PawTrackDev.
- Validación ejecutada: 52 unitarias focales (dominio/pricing/creación/promoción/downgrade/activación); integración de catálogo con publicar/revocar/reaprobar/desactivar (2 tests). Las pruebas usan DbContext InMemory y no prueban aplicar migración SQL.
- Pendientes: revisar evidencia legal/comercial de cada plan antes de aprobar; acordar renovación futura tras revocación; planificar despliegue de migración, comparar catálogo por entorno y medir estados base/missing-tier. No se afirma aprobación externa ni producción.

## 2026-10-01 — Revalidación de precios y aprobación Admin

- Alineados los análisis de pricing/unit economics con el dato local de UserPlus ₡3.000 y recalculados los escenarios hipotéticos. Las referencias ₡2.990 se mantienen sólo como hipótesis/históricas; no se atribuye aprobación o venta.

## 2026-10-01 — Aplicación local de migraciones

- Aplicadas en `PawTrackDev` las migraciones pendientes hasta `AddSubscriptionPlanCommercialApproval`, incluyendo las migraciones previas de operaciones y ledger de pagos que estaban pendientes en esa base.
- Verificado que el backend compila y que la integración focal del catálogo comercial pasa 3/3 pruebas. No se aplicó ninguna migración desde el landing ni se modificaron precios o aprobaciones comerciales.
- Staging y producción siguen sin verificación; las aprobaciones comerciales deben registrarse explícitamente por Admin después de revisar la evidencia correspondiente.
- `AdminSubscriptionPlansTab` reemplaza `window.prompt` por el `Modal` compartido y bloquea referencias fuera del formato admitido por el dominio.
- Verificación: integración de catálogo 3/3; pruebas Admin focales 4/4; typecheck frontend y `git diff --check` correctos. La ejecución de toda la solución no produjo un resumen final verificable en la salida disponible y no se declara aprobada.
- La migración sigue generada/no aplicada; producción no consultada y no se cambiaron datos de base.

## 2026-10-02 — Registro de devoluciones externas de reservas

- Se agregó el endpoint Admin `PUT /api/admin/service-providers/payments/{paymentId}/external-refund` para registrar devoluciones SINPE ya ejecutadas fuera de PawTrack, con monto incremental, referencia, motivo, idempotencia y auditoría. La ruta rechaza pagos ligados a un `PaymentIntent`, que conservan el flujo de devolución del gateway.
- Se actualizó [BOOKING_FLOW](../domains/BOOKING_FLOW.md) con los límites operativos: el registro no mueve fondos ni acredita la transferencia externa; el pago de reservas continúa `PARCIALMENTE_IMPLEMENTADO`.
- Validación: compilación aislada de API; suites de pagos/cancelación 12/12 y reembolso manual 3/3. Las pruebas específicas cubren precisión monetaria, replay idempotente, conflicto de payload y exclusión de tarjeta. No se aplicaron migraciones ni se verificaron SINPE, proveedor, staging o producción.

## 2026-10-02 — Plan híbrido de uso diario para tiendas

- Se actualizó [ROADMAP_TIENDAS_USO_DIARIO](../ROADMAP_TIENDAS_USO_DIARIO.md) como plan canónico aprobado por producto para un piloto híbrido de una tienda/sede, seguido por las fases de integridad de pedido, inventario auditable, operación financiera/fiscal externa, importación, notificaciones, personal/sucursales e integraciones.
- Se revalidó el código del workspace: stock escalar, reserva/expiración y rutas de reporte/verificación manual/devolución externa. Se corrigieron [B2B](../B2B_ESTADO_ACTUAL.md), [STATUS](../STATUS.md), [PRODUCT_SCOPE](../PRODUCT_SCOPE.md), [FEATURES](../FEATURES.md), [API_REFERENCE](../API_REFERENCE.md), [API_AUTHORIZATION_MATRIX](../API_AUTHORIZATION_MATRIX.md), [manual](../Manuales/MANUAL_TIENDAS.md), [legal](../legal.md), [runbook SINPE](../RUNBOOK_PAGOS_SINPE.md), [QA E2E](../GUIA_QA_E2E.md), [diccionario](../DICCIONARIO_DATOS.md), [marketplace](../domains/MARKETPLACE.md), [limitaciones](../domains/MARKETPLACE_LIMITATIONS.md), [KNOWN_LIMITATIONS](../KNOWN_LIMITATIONS.md), [API](../API.md) y [pricing](../PRICING_AND_PLANS.md).
- Se agregó `ENT-STORE` a [MASTER_TODO](../MASTER_TODO.md), se actualizó el índice y la [matriz de trazabilidad](FEATURE_TRACEABILITY_MATRIX.md), y se registraron discrepancias en [DOCUMENTATION_GAP_REPORT](DOCUMENTATION_GAP_REPORT.md).
- Eliminado `docs/pendientesTiendas.md` por ser backlog histórico duplicado; el índice y documentos de apoyo apuntan ahora al plan y backlog canónicos. Otros documentos históricos no se borraron.
- Validación documental: `git diff --check` en archivos tocados; búsqueda de referencias al backlog y afirmaciones obsoletas. No se ejecutaron builds, tests, migraciones, POS, proveedores, despliegue ni piloto. Operación productiva, migración aplicada y aprobaciones legal/fiscal permanecen `NO_VERIFICADO`.

## 2026-10-02 — Settlement B2C, idempotencia de pedidos y pagos de reservas

- B2C: `SinpePaymentModal` mantiene el reporte SINPE como aviso pendiente y no informa éxito por autorización de tarjeta; el callback de éxito solo corre tras estado `Active` y acción explícita. Tres pruebas cubren reporte, autorización y settlement.
- Tiendas: `POST /api/store-orders` exige `Idempotency-Key`; el handler usa hash canónico, reproduce payload equivalente y devuelve 409 `IDEMPOTENCY_KEY_CONFLICT` para payload distinto. Índice único filtrado cliente/clave; checkout conserva la clave en retries, la transmite en header y guía al usuario a revisar sus pedidos ante el conflicto.
- Migración `AddStoreOrderIdempotencyAndProviderRefundAccounting` agrega clave/hash a pedidos y campos acotados de reembolso de `ProviderPayment`; se aplicó y verificó dentro de una base LocalDB temporal desechable, pero no a `PawTrackDev` ni a otro entorno persistente.
- Reservas: tarjeta reutiliza quote de servidor, `PaymentIntent`, settlement y ledger existentes; SINPE sigue manual. La devolución externa Admin registra evidencia, pero no transfiere fondos.
- Validación: backend completo 1922/1922, incluyendo `StoreOrderIdempotencySqlTests` (base temporal vacía, upgrade de pedido legacy e inserción concurrente con clave repetida); PWA completa 222/222 antes del último test de conflicto, luego checkout 2/2, build y typecheck correctos; landing 53/53. Reservas focales 23/23 y pedido/modelo/API 44/44 más controller 3/3. No se verificaron gateway/SINPE reales, staging ni producción.
