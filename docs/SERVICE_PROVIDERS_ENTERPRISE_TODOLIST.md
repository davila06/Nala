# Todo List Enterprise - Proveedores de Servicios PawTrack CR

> Alcance: modulo B2B unico para adiestradores, groomers, hoteles, guarderias,
> paseadores, fotografos y otros servicios para mascotas. Incluye directorio,
> perfil, catalogo de servicios, verificacion, reservas, pagos, operacion y
> controles de nivel enterprise.
>
> Principio de arquitectura: crear `ServiceProviders` como modulo propio. No
> extender `Stores` ni reutilizar `StoreOrder`; inventario y entrega no modelan
> disponibilidad, capacidad ni citas.
>
> Estado real: la base funcional de `ServiceProviders` ya existe (registro,
> catalogo, disponibilidad, reservas, verificacion y operacion administrativa).
> Lo que queda es la capa de negocio/operacion que bloquea venta real, pagos,
> compliance y lanzamiento. Actualizado: 2026-09-07.

## Avance tecnico implementado (2026-09-07)

- [x] Agregado `ProviderPayment` con referencia SINPE, monto CRC, clave de idempotencia y estados `Pending`, `Reported`, `Confirmed`, `Disputed` y `Refunded`.
- [x] Flujo protegido de crear intención de pago, reportar pago del cliente y confirmar pago por Admin.
- [x] Auditoría de reporte y confirmación de pago mediante `AuditLogEntry`.
- [x] Índices únicos SQL para una intención por reserva y para `Idempotency-Key`.
- [x] Abstracción `IProviderPaymentGateway` con adaptador `ManualProviderPaymentGateway` para SINPE y futuros adquirentes.
- [x] Snapshot comercial inmutable de reserva: subtotal, impuestos, comisión, total y política de cancelación.
- [x] Migración `AddProviderPayments` generada y build del API validado.
- [x] Pruebas unitarias del agregado y handlers de pagos.
- [x] Expiración de reservas `Requested` y `AwaitingPayment`, incluyendo pagos pendientes.
- [x] Agregado `ProviderIncident` con investigación, resolución, apelación y cierre.
- [x] Persistencia SQL e índices operativos para incidentes.
- [x] Migraciones enterprise aplicadas en LocalDB.
- [x] API de incidentes con ownership de proveedor, cola administrativa paginada, investigación, resolución y cierre auditados.
- [x] Pestaña administrativa de incidentes integrada al panel existente.

Este avance implementa el flujo SINPE manual existente en PawTrack. No representa
una integración adquirente/webhook: todavía requiere contratar/configurar el
proveedor de pagos y definir firma, replay protection, conciliación y reembolsos
externos antes de producción.

# Version priorizada (P0 -> P4)

## Estado actual verificado

- [x] Modulo `ServiceProviders` separado de `Stores`.
- [x] Registro, perfil, directorio publico y aprobacion administrativa.
- [x] Catalogo, disponibilidad semanal, cierres y reservas.
- [x] Capacidad transaccional SQL, confirmacion, reprogramacion y vencimiento.
- [x] Verificacion documental privada y revision admin.
- [x] Notificaciones, recordatorios y lock distribuido para jobs.
- [x] Documentacion operativa base en `docs/SERVICE_PROVIDERS_OPERABILITY.md`.
- [x] Pruebas unitarias y una prueba de integracion de flujo clave.

## P0 - Bloqueadores de negocio y rollout

### 0.1 Decisiones de producto y legal

- [ ] Definir modelo de pagos: deposito, SINPE, comision, impuestos, reembolsos y disputas.
- [ ] Definir politica de cancelacion, no-show, reembolso y chargeback por categoria.
- [ ] Aprobar categorias iniciales: `Trainer`, `Groomer`, `Hotel`, `Daycare`, `Walker`, `Photographer`, `Other`.
- [ ] Definir si una cuenta puede operar uno o varios perfiles comerciales.
- [ ] Definir si un perfil puede ser persona fisica, empresa o ambos.
- [ ] Definir especies admitidas, restricciones por tamano, edad, raza, temperamento, vacunas y condiciones medicas.
- [ ] Definir modalidades: establecimiento, a domicilio, virtual, grupal, recogida/entrega, estadia nocturna y multisesion.
- [ ] Definir cobertura geografica, sedes, radio de servicio y zonas excluidas.
- [ ] Definir evidencias requeridas por categoria y vigencia, sin presentar PawTrack como licencia o aval estatal.
- [ ] Revisar terminos, privacidad y consentimiento con asesoria legal antes de pagos o captacion.

### 0.2 Exposicion productiva y riesgo operativo

- [ ] Definir criterios de verificacion visible: email, identidad, negocio, seguro, credenciales profesionales y perfil destacado.
- [ ] Definir politicas de contenido, bienestar animal, discriminacion, servicios prohibidos y reclamaciones.
- [x] Definir flujo de incidentes, investigación, apelación y resolución; la UI de soporte queda pendiente.
- [ ] Definir feature flags para: staff/admin, beta cerrada, directorio publico, reservas sin pago, pagos y disponibilidad general.

---

## P1 - Implementacion tecnica prioritaria

### 1.1 Pagos y reservas reales

- [x] Diseñar abstraccion de pagos con proveedores intercambiables y almacenamiento de referencias externas/estado/montos.
- [ ] Implementar validacion de firma, replay protection, idempotencia y auditoria del webhook.
- [x] Implementar reservas pendientes de pago y expiracion segura con lock distribuido.
- [x] Capturar precio, duracion, impuesto y politica de cancelacion al confirmar reserva.
- [x] Capturar precio, duracion, impuesto y politica de cancelacion al confirmar reserva.
- [x] Implementar `ConfirmProviderBookingPaymentCommand` e intención de pago SINPE manual.
- [ ] Implementar reconciliacion diaria de pagos, reembolsos y reservas inciertas.

### 1.2 Dominio, persistencia y API

- [ ] Completar modelo extensible por categoria con campos tipados y validacion fuerte.
- [~] Definir `ProviderPayment`, `ProviderIncident` y reglas de historial inmutable; falta `ProviderAuditLog` dedicado.
- [x] Definir estados de reserva y pago incluyendo `AwaitingPayment`, `Disputed` y `Refunded`.
- [x] Reforzar restricciones únicas para una intención por reserva e idempotencia.
- [ ] Revisar `AsNoTracking`, pagina, orden, filtros y ausencia de N+1 en queries criticas.
- [ ] Consolidar contratos de MediatR/domain events y versionado de DTOs/webhooks.
- [~] Ownership de servicios, disponibilidad, reservas, pagos, verificaciones e incidentes implementado; falta matriz RBAC de soporte dedicada.
- [ ] Completar `Result<T>`/Problem Details y rate limits revisados para todos los endpoints expuestos.

### 1.3 Seguridad y privacidad

- [x] Mantener direccion exacta de servicio a domicilio privada hasta el punto autorizado.
- [ ] Guardar documentos y evidencia en Blob privado con URLs firmadas y descarga auditada.
- [ ] Validar contenido de archivos: MIME, firma, extension, tamano y antivirus/malware scan.
- [ ] Sanitizar texto enriquecido y prevenir XSS en descripciones, comentarios y reseñas.
- [ ] Preparar retencion, exportacion, rectificacion y eliminacion/anonimizacion de datos.
- [ ] Completar threat model y pruebas OWASP antes de beta.

---

## P2 - Frontend, UX y operacion B2B

- [ ] Crear feature `service-providers` en frontend con API client tipado, hooks y pruebas co-localizadas.
- [ ] Añadir rutas publicas: directorio, detalle de proveedor y flujo de reserva.
- [ ] Añadir rutas de proveedor: registro, portal, perfil, servicios, agenda, reservas, verificacion, sedes y analitica.
- [ ] Añadir rutas de cliente: mis reservas, detalle, reprogramacion, cancelacion, recibo y reseña.
- [ ] Añadir pestañas admin para aprobacion, verificacion, incidentes, moderacion y soporte.
- [ ] Integrar proveedores activos en mapa publico sin mostrar ubicaciones privadas.
- [ ] Implementar filtros accesibles, paginacion, vacios, carga, error y estado de URL compartible.
- [ ] Crear calendario responsive con zona horaria explicita, slots bloqueados y capacidad restante.
- [ ] Diseñar formularios por categoria con validacion cliente alineada al backend.
- [ ] Implementar carga de imagen/documento con progreso, restricciones y mensajes seguros.
- [ ] Cumplir WCAG 2.2 AA en rutas criticas.

---

## P3 - Administracion, soporte y observabilidad

- [ ] Construir cola paginada para revisar proveedores y evidencias con SLA y decisiones auditadas.
- [ ] Permitir aprobacion, rechazo con motivo, suspension urgente y reactivacion.
- [ ] Construir vista de reservas y pagos con busqueda por ID, proveedor, cliente, fecha, estado y referencia externa.
- [ ] Definir RBAC de soporte con minimo privilegio y justificacion de acceso a PII.
- [~] Modelo, persistencia, auditoría, cola API, transiciones y UI implementados; falta RBAC de soporte dedicado.
- [ ] Definir SLIs/SLOs para directorio, reserva, pago, disponibilidad y notificaciones.
- [ ] Instrumentar Application Insights con eventos anonimizados y correlation IDs.
- [ ] Crear dashboards y alertas para reservas, agenda, webhooks, pagos, archivos y latencia.
- [ ] Implementar health checks y degradacion controlada para dependencias criticas.
- [ ] Preparar backup, restauracion, RPO/RTO, retencion y continuidad operativa.

---

## P4 - Calidad y lanzamiento

- [ ] Crear pruebas unitarias de entidades y handlers con invariantes, capacidad y ownership.
- [ ] Crear pruebas de repositorio/consulta para paginacion, filtros, ordenamiento y ausencia de N+1.
- [ ] Crear pruebas de integracion para registro, directorio, reservas concurrentes y acceso denegado.
- [ ] Crear pruebas de contrato para pagos y webhooks.
- [ ] Crear pruebas frontend y E2E Playwright del flujo completo con secuencia real.
- [ ] Añadir pruebas de accesibilidad automatizadas y revision manual en rutas principales.
- [ ] Ejecutar SAST, dependency scanning, secret scanning, DAST/OWASP y autorizacion antes de produccion.
- [ ] Integrar checks en CI con artefactos diagnosticos y gates.
- [ ] Actualizar README, OpenAPI, arquitectura y runbooks.
- [ ] Ejecutar UAT con al menos un proveedor por categoria inicial.
- [ ] Preparar plan de incidentes, rollback y comunicacion.

---

## Criterios de salida para produccion

- [ ] Las categorias, precios, politicas, verificacion y responsabilidades estan aprobados por producto, operaciones y legal.
- [ ] Ningun endpoint permite acceder o mutar recursos de otro usuario/proveedor.
- [ ] Las reservas concurrentes no pueden superar la capacidad ni duplicar pagos.
- [ ] El directorio mantiene paginacion, filtros correctos y rendimiento dentro del SLO acordado.
- [ ] Los documentos privados no son publicos y toda descarga autorizada queda auditada.
- [ ] Las pruebas unitarias, integracion, frontend, E2E, accesibilidad y seguridad pasan en CI.
- [ ] Dashboards, alertas, runbooks, backups y rutas de escalamiento estan probados.
- [ ] El lanzamiento se realiza mediante feature flag y puede revertirse sin perder reservas ni auditoria.

---

## Fuera de alcance inicial recomendado

- [ ] Marketplace multi-proveedor en un solo checkout.
- [ ] Ranking personalizado con IA o recomendaciones avanzadas.
- [ ] Mensajeria directa sin moderacion ni seguridad.
- [ ] Seguimiento GPS en tiempo real para paseadores.
- [ ] Integracion con agendas externas sin contrato e idempotencia.
- [ ] Verificacion automatica con registros gubernamentales sin convenio formal.
- [ ] Liquidacion financiera automatica antes de completar la capa de pagos, disputas y cumplimiento.

---

## Resumen ejecutivo

La base tecnica ya existe. El trabajo restante no es un backlog genérico: son tres bloqueadores reales:

1. decisiones de producto y legal,
2. pagos y politicas comerciales,
3. seguridad/operacion/observabilidad antes de rollout.

Los modulos de registro, catalogo, disponibilidad, reservas y verificacion ya quedaron en una base sólida; la siguiente fase debe centrarse en pagos, compliance y lanzamiento controlado.
