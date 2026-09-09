# PawTrack CR — TODO B2B Enterprise

> Checklist maestro para completar y endurecer todas las funciones B2B/B2G.
> Fecha: 2026-09-09
> Alcance activo: tiendas de mascotas, clínicas veterinarias, proveedores de servicios, aliados/refugios, adopciones y publicidad.
> Municipalidades B2G: diferidas por decisión de producto; no forman parte del ciclo actual de implementación.
> Objetivo: no declarar B2B terminado hasta cumplir funcionalidad, seguridad, billing, UX, pruebas y operación enterprise.

> Estado técnico consolidado: [B2B_ESTADO_ACTUAL.md](B2B_ESTADO_ACTUAL.md).
> El marketplace ya existe como operación base; el pricing y el payout de
> proveedores siguen deliberadamente sin aprobación comercial.

## Cómo usar este documento

- `[ ]` Pendiente
- `[~]` En progreso o parcialmente implementado
- `[x]` Implementado y validado con pruebas
- Cada tarea debe terminar con evidencia: código, prueba automatizada, captura/flujo validado o procedimiento operativo.

## Corte técnico 2026-09-09

La revisión del código actual confirma que las siguientes capacidades clínicas
ya tienen backend, migraciones y pruebas: perfil público y revisión de cambios,
API Partner v1 con scopes, sandbox fail-closed, expediente append-only, grants
con `read`/`write`/`export`, exportación clínica con cuotas y Blob privado,
agenda veterinaria con protección de solapamiento, permisos por veterinario,
firma RSA-SHA256 opcional, MFA TOTP/Data Protection y los flujos Generic de collares.

Los pendientes de código de mayor prioridad son: tenancy multiusuario y RBAC
transversal, UI de grants del propietario, compra/renovación
autoservicio para Store, exportaciones de tiendas, webhooks
salientes, pruebas negativas cross-tenant, E2E B2B, carga/concurrencia real,
observabilidad/SLO y validación externa de firmas. SENASA, precios, SLA,
impuestos y políticas comerciales/legal no son tareas de código autónomas.

---

## 0. Decisiones bloqueantes de producto y facturación

Estas tareas deben completarse antes de seguir agregando features, porque existen contradicciones entre documentos y tiers.

- [x] Usar `SubscriptionTier` + `SubscriptionPricing` como fuente técnica única; sincronizar catálogo comercial y aprobar cualquier cambio antes de publicarlo.
- [ ] Eliminar duplicaciones de B2C en `docs/planes.md` y conservar una sola definición por plan.
- [x] Confirmar tiers técnicos actuales de clínicas: estado base gratuito, `ClinicPlus` ₡15,000/mes y `ClinicPartner` ₡35,000/mes. Falta aprobación comercial formal solo si se modifica este catálogo.
- [x] Confirmar periodicidad técnica actual de municipalidades: `MuniBasica`, `MuniFull` y `MuniRedRegional` se modelan como cobro anual. Falta implementar compra/renovación real.
- [x] Confirmar nombres canónicos de enums y categorías del marketplace: `Trainer`, `Groomer`, `Hotel`, `Daycare`, `Walker`, `Photographer`, `Other`.
- [ ] Crear catálogo centralizado de productos/precios/taxes/moneda/versiones; no dejar precios solo en comentarios o handlers.
- [ ] Definir política de cambios de precio, grandfathering, upgrades, downgrades, cancelaciones, prorrateo y reembolsos.
- [ ] Definir si B2B soporta únicamente CRC o también USD y facturación internacional.
- [ ] Definir quién puede comprar cada tier, requisitos SENASA, verificación de identidad y documentos requeridos.
- [ ] Aprobar métricas enterprise: disponibilidad, tiempo de respuesta API, soporte, retención de datos y SLA por tier.

---

## 1. Fundación multi-tenant y permisos

- [ ] Documentar el modelo de tenancy: usuario propietario, organización B2B, miembros, sedes y roles.
- [ ] Separar explícitamente identidad de usuario, organización, clínica, tienda y municipalidad.
- [ ] Definir roles mínimos: Owner, Admin, Manager, Staff, Veterinarian, Cashier, Analyst, ReadOnly, APIClient.
- [ ] Implementar autorización por organización/recurso; comprobar que ningún usuario puede leer o mutar datos de otro tenant.
- [ ] Implementar políticas RBAC/ABAC centralizadas para backend y frontend.
- [ ] Añadir invitaciones de miembros con expiración, revocación, reenvío y auditoría.
- [x] Añadir MFA para administradores y usuarios con API keys. — TOTP, secretos protegidos, recovery codes de un solo uso, política privilegiada y WebAuthn/passkeys con ceremonia FIDO2, desafíos distribuidos, credenciales SQL, contador anti-replay, revocación y UI de enrolamiento.
- [ ] Añadir sesiones, refresh tokens, revocación y logout global para cuentas B2B.
- [~] Aplicar rate limits por tenant, usuario, API key y endpoint. — Rate limits por IP y endpoint existen; particionado por tenant/API key/tier sigue pendiente.
- [~] Crear auditoría inmutable para accesos, cambios de permisos, exportaciones, certificados, pedidos y acciones administrativas. — Auditoría clínica, certificados y exports existe; cobertura transversal de permisos/pedidos/tenants sigue pendiente.
- [~] Definir retención y borrado de datos por tenant, incluyendo solicitudes de exportación y eliminación. — Exports clínicos expiran en 24h; el job diario ahora purga versiones médicas supersedidas y metadatos de export vencidos según settings. Falta política completa por tenant.
- [ ] Ejecutar pruebas BOLA/IDOR, privilege escalation, tenant isolation y abuso de endpoints.

---

## 2. Billing y enforcement de planes

- [x] Crear catálogo de planes B2B versionado en base de datos o configuración fuertemente tipada. — `SubscriptionPricing.cs` (fuente única, reemplaza el diccionario inline).
- [ ] Implementar precios en CRC y USD según la decisión comercial aprobada.
- [x] Implementar suscripción, estado, periodo, fecha de cobro, cancelación y reactivación (base ya existía). — **Corregido bug crítico**: `Subscription.ValidateUserTier` rechazaba `StorePlus`/`StorePartner`; las tiendas nunca pudieron suscribirse. Ya corregido + probado.
- [ ] Implementar upgrades/downgrades con prorrateo y vigencia claramente definida.
- [ ] Implementar invoice/receipt, referencia de pago, estado de pago y conciliación SINPE/tarjeta.
- [ ] Implementar webhook idempotente del proveedor de pagos, si aplica.
- [x] Implementar feature gates en backend como autoridad final. — **Corregido bug crítico**: `GetActiveForUserAsync`/`GetActiveForClinicAsync` no filtraban por `ExpiresAt`; una suscripción vencida seguía dando acceso completo para siempre. Ya corregido a nivel de repositorio + `SubscriptionService.IsActive`.
- [x] Implementar gates consistentes para StorePlus/Partner, ClinicPlus/Partner — ya existían para Clinic; ahora también para Store (`SetStoreLocationActiveCommand`, `GetStoreAnalyticsQuery`).
- [x] Implementar jobs para expiración. — `SubscriptionExpirationJob` (BackgroundService, corre cada hora) creado desde cero; no existía ningún mecanismo de expiración automática.
- [~] Implementar notificaciones de renovación, fallo de pago, vencimiento y cambio de plan. — Renovación/vencimiento implementados; fallo de pago y cambio de plan siguen pendientes.
- [ ] Crear pantalla B2B de plan actual, límites, facturación, historial y acciones disponibles. — **Verificado ausente**: info de suscripción dispersa en dashboards. No existe ruta `/my-plan`.
- [x] Agregar pruebas de gates y transiciones críticas. — 8 tests de dominio + 11 de pricing + tests de handlers Activate/Cancel con sync de Store y Clinic.
- [x] Verificar que ningún precio permanezca hardcodeado sin fuente única. — Corregido en `CreateSubscriptionCommandHandler`.

---

## 3. Clínicas veterinarias

### 3.1 Base y directorio

- [x] Registro, revisión administrativa, perfil, directorio y mapa básico.
- [x] Escaneo QR y microchip RFID manual.
- [x] Resultado de mascota y notificación al propietario.
- [x] Resolver los campos públicos: teléfono, website, horario, dirección, logo, descripción y servicios. — Perfil autenticado editable y DTO público dedicado.
- [x] Crear perfil público de clínica con mapa, contacto, horario, servicios y estado de verificación. — `GET /api/clinics/public/{clinicId}` + `/clinicas/:clinicId`.
- [x] Añadir filtros y paginación del directorio por ubicación, servicios, horario y disponibilidad. — Endpoint público con paginación acotada, búsqueda y filtro de emergencias en SQL; servicios/horarios quedan disponibles en el perfil público.
- [x] Añadir flujo de corrección/actualización del perfil y revisión administrativa de cambios. — Solicitud pendiente, cola admin, aprobación/rechazo y aplicación auditada; licencia SENASA permanece fuera de alcance.
- [ ] Validar licencia SENASA, fecha de vencimiento y re-verificación periódica.

### 3.2 ClinicPlus

- [x] Posición destacada y ordenamiento en mapa/directorio.
- [x] Badge de clínica verificada.
- [x] Estadísticas de escaneos mensuales.
- [x] Banner/sponsorship de Case Room.
- [x] Logo en alertas cercanas: verificar delivery real en WhatsApp, push, email y plantillas. — **BUG CRÍTICO RESUELTO (2026-09-01)**: `NearbyClinicRef` ahora transporta `LogoUrl` (`IChannelBroadcaster.cs`). El logo real se entrega en los 3 canales que lo prometían: **Email** — `<img>` inline en el HTML (`EmailSender.SendBroadcastLostPetAsync`); **WhatsApp** — mensaje `type: "image"` adicional para la clínica más cercana con logo, ya que la API de Meta no permite imágenes embebidas en mensajes de texto (`WhatsAppChannelBroadcaster.SendSponsorLogoAsync`); **Telegram** — se detectó un segundo bug (nunca mencionaba clínicas ni en texto) y se corrigió agregando la sección de texto + una llamada `sendPhoto` dedicada (`TelegramChannelBroadcaster`). Facebook queda fuera de alcance (nunca formó parte de la promesa comercial original). Cobertura: 10 tests nuevos en `backend/tests/PawTrack.UnitTests/Broadcast/` (Email/WhatsApp/Telegram), suite completa verificada en verde (1020 unit + 73 integration).
- [x] Métricas de visibilidad: backend existe; completar y validar la pestaña frontend. — **COMPLETO**: tab "📈 Visibilidad" implementado y funcional en `ClinicDashboardPage`. `ClinicVisibilidadSection` muestra Profile Views, Map Clicks, Search Appearances, Alert Impressions, Scan Result Views. Gateado a ClinicPlus.
- [~] Definir métricas y nomenclatura: profile views, map clicks, search appearances, scans y matched scans. — Las cinco métricas existen en `ClinicVisibilityStatsDto`; falta formalizar glosario, retención y SLA comercial.
- [x] Aplicar deduplicación, anonimización/hash de IP y retención documentada. — IP hash SHA-256 implementado en `TrackView` endpoint; purge a 90 días en `ClinicProfileViewPurgeHostedService`.
- [ ] Implementar soporte prioritario con SLA, cola y trazabilidad operacional.

### 3.3 ClinicPartner

- [x] Certificados PDF y código de verificación público.
- [x] API keys y endpoints de lookup.
- [x] Widget embebible.
- [x] QR dentro del PDF — **ya estaba implementado** (`QuestPdfCertificateService.GenerateQrPng`, usado tanto en el certificado estándar como en el pasaporte de vacunación). El estado ❌ de `featuresB2B.md` estaba desactualizado.
- [~] Firma digital: reemplazar firma visual por firma criptográfica verificable. — RSA-SHA256 detached signature sidecar implementado; el endpoint público ahora valida PDF + `.sig` con Key Vault o clave pública explícita de desarrollo. Falta custodia/rotación de claves en Azure y validación externa Adobe/independiente.
- [x] **Resuelto** — inconsistencia de logo en alertas corregida: `LogoUrl` agregado a `NearbyClinicRef` y entregado en Email (inline), WhatsApp (mensaje de imagen separado) y Telegram (sendPhoto). Ver detalle en la sección 3.2.
- [~] Definir CA, certificado por clínica, rotación, revocación y custodia en Azure Key Vault/HSM. — El firmador usa Managed Identity y `Certificates:KeyVaultKeyId`; falta infraestructura Azure y política de rotación por clínica.
- [ ] Validar firma PDF en Adobe/validadores independientes y documentar la cadena de confianza.
- [x] Completar permisos por API key: expiración, rotación, revocación, last-used. — `ClinicApiKey.ExpiresAt` (1 año por defecto), rotación conserva scopes y rechaza keys expiradas/revocadas, scopes desconocidos fallan cerrado, M2M no accede a gestión humana, descargas médicas exigen `medical:export` y `LastUsedAt` se persiste dentro del request.
- [x] Scopes por API key (permisos granulares). — `scan`, `medical:read`, `medical:write`, `medical:export`, `certificates`, `analytics`; middleware aplica el scope por ruta y migración hace backfill.
- [x] Añadir versionado de API, OpenAPI publicada, ejemplos, errores RFC 7807 y changelog. — `/api/v1`, `/openapi/v1.json`, `docs/API_CLINIC_PARTNER_v1.md` y `docs/CHANGELOG.md`.
- [x] Añadir sandbox para integradores HIS. — Frontera documentada con ambiente, DB/Blob, datos y credenciales aislados.
- [ ] Completar integración con lectores RFID USB/BLE solo si queda dentro del alcance contractual; si no, retirarla del plan comercial.
- [~] Añadir multi-veterinario: perfiles, agenda/atribución, permisos y auditoría. — Backend y UI de agenda/permisos existen; UI de atribución per-acción y reportes operativos siguen pendientes.
- [x] Añadir exportaciones auditadas y límites por periodo. — Export clínico con grant `export`, scope `medical:export`, cuotas 20/mes por clínica + 1/24h por mascota, Blob privado y expiración de 24h.

### 3.4 Expediente y consentimiento

- [x] Grants de acceso y expediente compartido base.
- [x] Validar consentimiento explícito, alcance, expiración y revocación por mascota. — Grants ahora soportan expiración y revocación; lectura/escritura se valida en los flujos clínicos.
- [~] Implementar permisos separados para leer, agregar, editar, eliminar y exportar. — Grants tienen `read`, `write`, `export`; lectura, escritura, append-only y export clínico aplican gates. Falta UI explícita del propietario para permisos y política de borrar/exportar por rol.
- [x] Registrar cada acceso clínico con actor, clínica, mascota y timestamp. — `ClinicMedicalAccessLog` ahora conserva operación, permiso, método (`inline_scan`, `recent_scan`, `active_grant`), resultado y motivo; también registra denegaciones después de identificar la mascota.
- [x] Añadir bloqueo de edición posterior o historial de versiones para registros médicos. — Operaciones de edición/supresión crean supersession/revisión append-only.
- [~] Definir retención, exportación y eliminación conforme a la política de privacidad. — El job diario aplica ventanas configurables a versiones médicas y exports clínicos; falta consolidar la política legal completa.
- [x] Probar que el plan del dueño no permite bypass del consentimiento ni acceso de clínica no autorizada. — Tests de grants y ownership; ampliar a matriz completa de endpoints queda pendiente.

---

## 4. Tiendas de mascotas

### 4.1 StoreBasic

- [x] Registro, aprobación administrativa, perfil público, catálogo y mapa/directorio.
- [ ] Completar perfil comercial: teléfono, WhatsApp, horario, dirección, métodos de entrega y políticas.
- [ ] Añadir gestión de inventario, disponibilidad y productos archivados.
- [ ] Añadir validación y moderación de imágenes, descripciones, precios y enlaces.
- [ ] Añadir estados de tienda: pendiente, activa, suspendida, cerrada y rechazada.
- [ ] Añadir privacidad para datos de clientes y no exponer información sensible en el catálogo.

### 4.2 StorePlus

- [x] Recepción de pedidos in-app y máquina de estados.
- [x] Referencia de pago/SINPE y panel operativo básico.
- [x] Notificaciones de nuevos pedidos y cambios de estado.
- [ ] Implementar flujo completo de confirmación de pago, rechazo, expiración y reembolso.
- [ ] Añadir idempotencia para crear pedidos, reportar pago y cambiar estados.
- [ ] Añadir control de concurrencia para evitar doble aceptación o doble descuento de inventario.
- [ ] Añadir carrito, impuestos, costo de envío/retiro y total reproducible.
- [ ] Añadir comprobante de pedido para cliente y tienda.
- [ ] Definir si PawTrack intermedia fondos o solo comunica la referencia SINPE.
- [ ] Implementar estadísticas básicas de ventas con periodo, zona horaria y filtros documentados.
- [ ] Definir soporte y SLA StorePlus.

### 4.3 StorePartner

- [x] Implementar `GetStoreAnalyticsQuery` y endpoints de analytics avanzados. — `GET /api/stores/me/analytics`, gateado StorePlus (totales) / StorePartner (desglose diario + top productos).
- [x] Definir métricas: ventas, órdenes, ticket promedio, productos. — `TotalOrders`, `DeliveredOrders`, `CancelledOrders`, `TotalRevenueCrc`, `AverageOrderValueCrc`, top 5 productos por ingreso.
- [x] Añadir filtros por periodo y sede. — `year`/`month`/`locationId` en el query.
- [x] Añadir exportación CSV/PDF con permisos y auditoría. — Store Partner dispone de `GET /api/stores/me/analytics/export`; reutiliza el gate analytics, limita el alcance a la tienda/sede del actor, aplica límite de 20 exportaciones/mes y registra `StoreAnalyticsExported`.
- [x] Implementar modelo `StoreLocation`/sedes con tenant común y permisos por sede. — entidad + migración `AddStoreLocationsAndOrderAttribution`, CRUD completo gateado a StorePartner.
- [x] Migrar pedidos y analytics para soportar `StoreId` + `LocationId`. — `StoreOrder.LocationId` (nullable), `PlaceStoreOrderCommand` valida pertenencia/estado activo de la sede.
- [x] Añadir consolidado multi-sucursal y vista local por sede. — sin `locationId` = consolidado; con `locationId` = vista de esa sede (solo Partner).
- [ ] Implementar badge Partner verificado visible en directorio, mapa y perfil (UI pendiente). — `Store.IsFeatured` y ordering correcto; badge visual en `StoreDirectoryPage` existe pero visibilidad insuficiente comparada con clínicas.
- [ ] Implementar posicionamiento prioritario con reglas transparentes y límites contra abuso.
- [ ] Añadir onboarding y soporte de cuenta enterprise.
- [ ] Añadir gestión de inventario por sede, transferencias entre sedes y catálogo diferenciado (fuera de alcance de esta ronda).

---

## 4.4 Proveedores de servicios B2B

Categorías soportadas: **adiestradores, groomers, hoteles, guarderías,
paseadores, fotógrafos y otros**. El proveedor no es una tienda y tiene su
propio catálogo, disponibilidad, reservas, verificación y operación de
incidentes.

- [x] Módulo separado de tiendas con registro, aprobación, directorio y perfil.
- [x] Perfiles de proveedor implementados: registro público, estado pendiente, edición autenticada y publicación pública condicionada al estado.
- [x] Categorías canónicas implementadas en dominio, API y frontend.
- [x] Directorio público, filtros por categoría/modalidad/precio/radio y capa de mapa.
- [x] Catálogo editable, disponibilidad semanal, cierres excepcionales y reservas con control de capacidad.
- [x] Verificación documental privada, vencimiento/revalidación, auditoría y consola admin.
- [x] Snapshot comercial de reserva: subtotal, impuestos, fee, total y política.
- [x] Pago manual/SINPE con idempotencia, confirmación, expiración, disputa y reembolso.
- [x] Incidentes de bienestar, seguridad, política, pago y privacidad con soporte `Admin`/`Support`.
- [x] Notificaciones de solicitudes, cambios de reserva, recordatorios y estado de proveedor.
- [x] Pruebas unitarias focalizadas e integración HTTP base.
- [x] Ejecutar concurrencia contra SQL Server real y E2E Playwright con backend/Azurite sembrados. — Carrera de registro WebAuthn validada 1/1 en SQL Server; aislamiento negativo B2B validado 1/1 con backend real. Export Store Partner permanece opt-in hasta sembrar una suscripción enterprise activa.
- [ ] Aprobar tier o comisión de proveedor; actualmente no existe pricing comercial.
- [ ] Aprobar membresía de proveedores: perfil base gratis, verificado ₡3,000/mes y destacado ₡5,000/mes, o validar un plan único de ₡3,990/mes.
- [ ] Implementar billing recurrente, periodo gratuito inicial, feature gates, cancelación, expiración y renovación de la membresía.
- [ ] Integrar gateway externo autorizado, payout, conciliación, factura e impuestos.
- [ ] Completar KYC empresarial, contratos digitales y responsabilidad por categoría.
- [ ] Implementar tiers, límites, ranking promocionado y SLA solo después de aprobar el catálogo.
- [ ] Definir política por categoría para cancelación, no-show, evidencia y servicio a domicilio.
- [ ] Añadir geofiltros/ranking sin revelar ubicaciones privadas.

## 5. Aliados, refugios y adopciones

- [x] Registro/verificación de aliados y perfil público base.
- [x] Bandeja de alertas, coordinación y KPI base.
- [x] ShelterBasic/ShelterPlus y flujo de adopciones base.
- [ ] Definir formalmente tipos de aliado, permisos, cobertura geográfica y responsable legal.
- [ ] Añadir roles y miembros para refugios y organizaciones grandes.
- [ ] Añadir workflow de revisión documental, expiración y revalidación.
- [ ] Añadir auditoría de acciones de campo y evidencias adjuntas.
- [ ] Añadir métricas de adopción: publicados, solicitudes, visitas, colocaciones y tasa de éxito.
- [ ] Añadir moderación antifraude para publicaciones, fotos, contactos y solicitudes.
- [ ] Implementar consentimiento y protección de datos de adoptantes.
- [ ] Añadir exportaciones y reportes para organizaciones.
- [ ] Definir SLA de alertas y canales de escalamiento.

---

## 6. Municipalidades B2G — DIFERIDO

> Este módulo queda fuera del alcance de implementación actual. Se conserva el
> inventario técnico y el roadmap para una futura decisión de producto; no se
> deben ejecutar estas tareas ni vender estos tiers como compra autoservicio.

- [x] Perfil, capturas, estados, búsqueda, fotos gateadas, estadísticas y dashboard regional base.
- [x] Transferencia de capturas y multi-cantón base.
- [x] Tiers MuniBasica/MuniFull/MuniRedRegional en `SubscriptionTier` y `SubscriptionPricing` (precios anuales ₡150k/₡300k/₡500k). Sync con `MunicipalityProfile.Tier` al activar/cancelar/expirar suscripción.
- [ ] Resolver definitivamente precios y periodicidad de facturación municipal. — Tiers definidos como anuales en código, pero docs no lo reflejan de forma única.
- [ ] Implementar catálogo/billing de MuniBasic, MuniFull y MuniRedRegional. — **Verificado ausente**: enums y precios existen pero NO hay UI/flow de compra para municipalidades. Sin integración SINPE ni renovación automática anual.
- [ ] Implementar roles por municipalidad, cantón y dependencia.
- [ ] Completar aislamiento de datos entre cantones y municipalidades.
- [ ] Completar dashboard visual con estadísticas, mapas, tendencias y exportaciones por tier.
- [ ] Añadir reportes oficiales configurables y trazabilidad de generación.
- [ ] Añadir integración/exportación para SENASA/PANI donde esté aprobada.
- [ ] Añadir SLA, disponibilidad y soporte contractual B2G.
- [ ] Añadir flujo de transferencia con aceptación, historial y rollback administrativo.
- [ ] Añadir carga masiva validada y reporte de errores por fila.
- [ ] Definir retención de fotos, datos de captura y documentos públicos.
- [ ] Crear pruebas de autorización cross-canton y cross-municipality.

---

## 7. Vallas publicitarias y monetización B2B

- [x] Activar placements base Map, Dashboard, Directory y Feed. — Map ya no depende de activar la capa de tiendas; Feed se entrega cuando hay casos de mascotas perdidas en el mapa.
- [x] Activar inventario contextual: perfil QR público, historial de escaneos, Case Room, directorio y perfil de clínicas, directorio y perfil de proveedores, adopciones, ferias, confirmación de registro y activación de CollarTag.
- [x] Estados, aprobación, imágenes, CTA, dismissal de 24 horas y paginación.
- [x] Instrumentar eventos sin PII por campaña y placement: impresión, clic y descarte (`BillboardImpression`, `BillboardClicked`, `BillboardDismissed`).
- [~] Restringir por política el placement Case Room a recuperación: clínicas de emergencia, GPS, microchip, búsqueda y seguros; prohibir promociones generales. — La restricción comercial está definida, falta enforcement con categoría aprobada en el modelo de campaña.
- [~] Separar los resultados destacados geolocalizados de la valla general en clínicas y proveedores. — Los placements de directorio/perfil ya están activos; falta ranking patrocinado con radio, categoría y disclosure.
- [~] Definir el catálogo comercial versionado por placement, elegibilidad de categoría, cobertura geográfica, límite de frecuencia y precio. — El inventario técnico ya está centralizado en `BILLBOARD_PLACEMENTS`; precio/segmentación requieren aprobación comercial.
- [ ] Definir catálogo comercial de campañas, CPM/flat fee, duración y segmentación.
- [ ] Implementar contrato/cotización, estado de pago y facturación de anunciantes.
- [ ] Implementar límites por placement, tenant, frecuencia y prioridad.
- [~] Añadir métricas de impresiones, clics, CTR, dismissal y conversión. — Eventos de producto listos para agregación; faltan almacenamiento agregado, CTR de fuente de verdad, conversiones y reporte exportable para anunciante.
- [ ] Añadir deduplicación y protección contra tráfico automatizado.
- [ ] Añadir consentimiento/privacidad para tracking y documentar retención.
- [ ] Añadir moderación de contenido, revisión legal y lista de categorías prohibidas.
- [ ] Añadir preview responsive antes de aprobación.
- [ ] Crear reporte para anunciante y auditoría de cambios.

---

## 8. Frontend enterprise y experiencia B2B

- [ ] Crear navegación B2B consistente por organización, módulo, sede y rol.
- [x] Mostrar plan, límites, estado de suscripción y permisos en cada dashboard. — Banner con tier real, fecha de vencimiento y CTA dinámico en `StoreDashboardPage` y `ClinicDashboardPage` usando `useMySubscription`.
- [ ] Eliminar botones que aparentan estar disponibles cuando el backend los rechaza.
- [ ] Añadir estados completos: loading, empty, error, forbidden, suspended, expired y pending approval. — Parcial: loading y error OK; no existe página global de "plan vencido" con CTA a renovar.
- [x] Completar Clinic Visibility tab. — **COMPLETO** (audit 2026-09-01): tab funcional con 5 métricas.
- [x] Completar Store Partner Analytics. — **COMPLETO**: `StoreAnalyticsPage` con selector mes/año, desglose diario y top-productos.
- [x] Completar vistas multi-sucursal y selector de sede. — **COMPLETO**: `StoreLocationsPage` con CRUD gateado a StorePartner.
- [ ] Mostrar badges y posiciones promocionadas de forma consistente y accesible. — Clínicas OK; tiendas: badge existe pero visibilidad insuficiente.
- [ ] Añadir tablas con filtros, paginación, exportación y zona horaria visible.
- [ ] Añadir confirmaciones para acciones irreversibles y cambios de estado.
- [ ] Añadir accesibilidad WCAG 2.2 AA: teclado, foco, contraste, labels y lector de pantalla.
- [ ] Añadir responsive para operación en tablet/móvil y escritorio.
- [ ] Añadir i18n preparado para español/inglés sin romper formatos CRC/fecha.
- [ ] Validar performance de dashboards con datasets grandes.

---

## 9. API, integraciones y plataforma

- [~] Publicar OpenAPI por módulo y por versión. — Clinic Partner v1 está publicado/documentado; faltan contratos versionados para Stores, Municipalidades, webhooks y SDKs.
- [ ] Estandarizar envelopes de error, códigos HTTP, correlation ID y Problem Details.
- [~] Versionar API pública B2B y definir política de deprecación. — `/api/v1` existe para Clinic Partner; falta política transversal por módulo.
- [ ] Añadir idempotency keys a mutaciones de pedidos, pagos, transferencias y uploads.
- [ ] Añadir paginación cursor-based donde existan listas grandes.
- [ ] Evitar N+1 queries y aplicar índices, `AsNoTracking` en lecturas y límites de filas.
- [ ] Añadir timeouts, retries con backoff y circuit breakers para integraciones externas.
- [ ] Añadir webhooks salientes firmados, reintentos, replay protection y delivery log.
- [ ] Validar CORS del widget y scopes por dominio registrado.
- [ ] Añadir SDK o ejemplos oficiales para HIS, tiendas y municipalidades.
- [~] Crear entorno sandbox con datos sintéticos. — Sandbox fail-closed y contrato Partner documentados; falta despliegue operativo verificable con datos sintéticos por módulo.

---

## 10. Seguridad, privacidad y cumplimiento

- [ ] Completar threat model de cada módulo B2B.
- [ ] Aplicar secretos únicamente desde Azure Key Vault/managed identity.
- [ ] Rotar API keys, secretos de integración y certificados sin downtime.
- [ ] Cifrar datos sensibles en tránsito y reposo.
- [ ] Hash/anonymize IPs, tokens y datos de auditoría donde corresponda.
- [ ] Revisar uploads: MIME real, extensión, tamaño, malware scanning, dimensiones y re-encoding.
- [ ] Proteger URLs de Blob con SAS de corta duración y permisos mínimos.
- [ ] Añadir anti-abuse para escaneos, búsquedas, pedidos, login y API.
- [ ] Revisar logs para no escribir PII, tokens, API keys ni secretos.
- [ ] Ejecutar SAST, dependency scanning, secret scanning, DAST y penetration test.
- [ ] Documentar privacidad, consentimiento médico, retención y derechos del titular.
- [ ] Preparar procedimiento de incidente, revocación masiva de keys y comunicación a clientes.

---

## 11. Pruebas enterprise

- [x] Unit tests para dominio, pricing, gates, estados, permisos y validadores. — **1329 unit tests pasando** (2026-09-09). Incluyen clínica, MFA, expediente append-only, exports, agenda, permisos veterinarios, suscripciones, analytics, sedes, API keys y collares.
- [x] Integration tests para los endpoints B2B principales. — **102 integration tests pasando** (2026-09-09), incluyendo auth, clinics, stores, collars, adoptions y flujos enterprise.
- [ ] Contract tests para API pública, widget, webhooks y pagos.
- [ ] Tests de aislamiento multi-tenant y autorización negativa.
- [ ] Tests de concurrencia para pedidos, pagos, inventario y transferencias.
- [ ] Tests de idempotencia y reintentos.
- [ ] Tests de migraciones sobre base vacía y base con datos existentes.
- [ ] Tests de jobs: expiración, purga, notificaciones, estadísticas y agregaciones.
- [ ] E2E frontend para onboarding, activación, operación, upgrade y suspensión.
- [ ] Accessibility tests y pruebas manuales con teclado/lector.
- [ ] Performance/load tests con objetivos documentados por endpoint.
- [ ] Chaos/failure tests para Blob, email, WhatsApp, pagos y servicios externos.
- [ ] Snapshot tests para PDFs, certificados, respuestas API y documentos críticos.
- [ ] Revisar cobertura y mutation testing en reglas de autorización y billing.

> ⚠️ **Hallazgo histórico (2026-08-25, corregido 2026-09-01):** La suite de integración fallaba por `UseNetTopologySuite` vs InMemory. El estado verificado actual es 102 integration tests y 1329 unit tests pasando.

> ✅ **Hallazgo histórico (2026-09-01):** Se corrigió la entrega de logos ClinicPlus en Email, WhatsApp y Telegram. La cobertura actual se valida dentro de la suite vigente de 1329 unit tests y 102 integration tests.

---

## 12. Observabilidad y operación

- [ ] Definir dashboards por módulo: errores, latencia, volumen, conversión y uso de gates.
- [ ] Instrumentar correlation ID y distributed tracing.
- [ ] Crear alertas para errores de pagos, API keys, jobs, colas y notificaciones.
- [ ] Medir SLI/SLO por tier comercial.
- [ ] Crear runbooks para incidentes B2B, pagos, pérdida de datos, abuso y caída de integraciones.
- [ ] Añadir health checks dependenciales y readiness/liveness correctos.
- [ ] Validar backups, restore drills, RPO/RTO y retención.
- [ ] Crear procedimiento de despliegue, rollback y migraciones sin downtime.
- [ ] Crear soporte interno con clasificación P1/P2/P3/P4 y tiempos por plan.
- [ ] Añadir auditoría y exportación de logs para clientes enterprise sin exponer PII de terceros.
- [ ] Crear reporte mensual de uso, SLA y seguridad para clientes Partner/B2G.

---

## 13. Datos, migraciones y calidad

- [ ] Crear migraciones para todas las nuevas entidades y probarlas en SQL Local/CI/staging.
- [ ] Actualizar scripts de esquema local y seed de usuarios/organizaciones B2B.
- [ ] Crear índices para búsquedas por tenant, sede, periodo, estado y ownership.
- [ ] Preparar backfill de datos existentes al nuevo modelo de organización/sede.
- [ ] Validar consistencia entre enums, seeds, DTOs y valores persistidos.
- [ ] Crear datos sintéticos representativos para clínicas, tiendas y municipalidades.
- [ ] Ejecutar análisis de datos huérfanos, duplicados y registros sin tenant.
- [ ] Definir políticas de archival/purge por entidad.

---

## 14. Documentación y preparación comercial

- [ ] Actualizar `docs/planes.md` con la matriz final aprobada, sin duplicados.
- [ ] Actualizar `docs/precios.md`, `docs/featuresB2B.md` y documento maestro para que coincidan.
- [ ] Separar claramente implementado, parcial, roadmap y servicio operacional.
- [ ] Crear manual de clínica, tienda, aliado y municipalidad con flujos actuales.
- [ ] Crear guía de onboarding por tier.
- [ ] Crear guía de API Partner con autenticación, scopes, límites y ejemplos.
- [ ] Crear guía del widget con dominios permitidos y configuración.
- [ ] Crear matriz de responsabilidades PawTrack/cliente/proveedor de pagos.
- [ ] Crear términos comerciales, privacidad, consentimiento y SLA B2B.
- [ ] Preparar demos y cuentas de prueba por cada tier.
- [ ] Crear checklist de due diligence para clientes enterprise.

---

## 15. Gate de salida B2B Enterprise

No marcar el objetivo como terminado hasta cumplir todos los puntos:

- [ ] Precios, tiers y nombres únicos y sincronizados entre documentación, código y billing.
- [ ] Cada feature vendida tiene backend, frontend, autorización, prueba y evidencia operacional.
- [ ] No existen features premium solo descritas o solo visibles en UI.
- [ ] No existen endpoints críticos sin autorización, idempotencia, rate limit y auditoría.
- [ ] No existen datos cross-tenant en pruebas negativas.
- [ ] Billing probado para alta, renovación, fallo, upgrade, downgrade, cancelación y reactivación.
- [ ] Clínicas, tiendas, aliados y municipalidades tienen onboarding y soporte definidos.
- [ ] Analytics, multi-sucursal, firma digital y RFID avanzado están implementados o retirados explícitamente del catálogo comercial.
- [~] API pública, widget y webhooks tienen contrato versionado y sandbox. — Clinic Partner API/widget tienen contrato v1 y sandbox documentado; webhooks salientes y contratos de Stores/Municipalidades siguen pendientes.
- [ ] Backups, restore, observabilidad, alertas y runbooks fueron probados.
- [ ] Seguridad externa y pruebas de carga completadas sin hallazgos bloqueantes.
- [ ] Product owner y responsable técnico firman la matriz de aceptación final.

---

## Orden recomendado de ejecución

1. Normalizar producto, tiers y precios.
2. Cerrar tenancy, permisos y billing.
3. Completar brechas de clínicas y tiendas que ya se venden como premium.
4. Completar frontend enterprise y API contracts.
5. Endurecer seguridad, privacidad y auditoría.
6. Completar pruebas, performance y observabilidad.
7. Actualizar documentación comercial y ejecutar el gate de salida.
