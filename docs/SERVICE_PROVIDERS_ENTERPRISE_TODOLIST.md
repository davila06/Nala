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
> Estado: fundacion, catalogo, disponibilidad, reservas, verificacion y operacion
> administrativa implementados. Pagos, disputas, filtros avanzados y E2E siguen
> pendientes de decisiones de producto e integraciones externas. Actualizado:
> 2026-09-07.

## Estado de implementacion actual

- [x] Modulo `ServiceProviders` separado de `Stores`, con rol, perfil, directorio
      publico, registro y aprobacion administrativa.
- [x] Catalogo de servicios con publicar, pausar, archivar y editar sin borrar
      historial.
- [x] Disponibilidad semanal y cierres excepcionales, con desactivacion logica.
- [x] Slots publicos, capacidad transaccional SQL y reservas con cancelacion,
      reprogramacion, confirmacion, inicio, finalizacion, inasistencia y vencimiento.
- [x] Notificaciones de reservas y recordatorios; jobs protegidos por lock
      distribuido.
- [x] Verificacion documental privada, revision admin, descarga autorizada,
      vencimiento, revalidacion y superseding.
- [x] Portal de proveedor, directorio, reserva cliente, listados operativos y
      consola administrativa.
- [x] Pruebas unitarias focalizadas y prueba de integracion del registro/directorio.
- [x] Runbook inicial en `docs/SERVICE_PROVIDERS_OPERABILITY.md`.

Bloqueadores antes de pagos y lanzamiento general:

- [ ] Aprobar modelo de pagos: deposito, SINPE, comision, impuestos, reembolsos y
      disputas.
- [ ] Definir politica de cancelacion/no-show y responsabilidad por categoria.
- [ ] Decidir requisitos de evidencia y limites B2B por categoria.
- [ ] Implementar pruebas de concurrencia sobre SQL Server real y E2E Playwright.
- [ ] Definir filtros geograficos y ranking del directorio sin exponer ubicaciones
      privadas.

---

## 1. Resultado esperado

- [ ] Un proveedor puede registrarse, verificar su correo y completar un perfil
      comercial pendiente de revision.
- [ ] Un administrador puede revisar, aprobar, rechazar, suspender y reactivar
      proveedores con trazabilidad completa.
- [ ] Cualquier visitante puede encontrar proveedores activos por categoria,
      ubicacion, modalidad y otros filtros seguros.
- [ ] Un proveedor puede publicar servicios, precios, modalidades, cobertura y
      disponibilidad sin exponer datos sensibles.
- [ ] Un cliente puede solicitar, reservar, pagar cuando aplique, reprogramar o
      cancelar un servicio para una de sus mascotas.
- [ ] El sistema evita reservas duplicadas, respeta capacidad y comunica los
      cambios relevantes a cada parte.
- [ ] Operaciones cuenta con backoffice, auditoria, metricas, soporte y runbook.

---

## 2. Decisiones de producto que bloquean el desarrollo

### 2.1 Alcance inicial y categorias

- [ ] Aprobar categorias iniciales: `Trainer`, `Groomer`, `Hotel`, `Daycare`,
      `Walker`, `Photographer`, `Other`.
- [ ] Definir si una cuenta puede operar uno o varios perfiles comerciales.
- [ ] Definir si un perfil puede pertenecer a persona fisica, empresa o ambos.
- [ ] Definir categorias adicionales, etiquetas y si un admin puede gestionarlas.
- [ ] Definir especies admitidas por servicio y restricciones por tamano, edad,
      raza, temperamento, vacunas o condiciones medicas.
- [ ] Definir modalidades: en establecimiento, a domicilio, virtual, grupal,
      recogida y entrega, estadia nocturna y multisesion.
- [ ] Definir cobertura geografica: sede, radio de servicio, cantones/provincias
      y zonas excluidas.
- [ ] Definir campos obligatorios y opcionales por categoria; evitar forzar los
      mismos datos para un hotel y un adiestrador.
- [ ] Definir politicas publicas de contenido, bienestar animal, discriminacion,
      servicios prohibidos y reclamaciones.

### 2.2 Modelo comercial

- [ ] Definir planes y limites: perfil basico, destacado, reservas, varias sedes,
      analitica, personal, promociones y soporte prioritario.
- [ ] Decidir si los planes B2B se generalizan desde `StorePlus`/`StorePartner`
      o se crean tiers especificos de proveedor.
- [ ] Definir comision por reserva, costo de procesamiento, impuestos, facturacion
      y responsables de cobro.
- [ ] Definir si el MVP admite pago completo, deposito, pago posterior o solo
      solicitud de reserva.
- [ ] Definir politicas de cancelacion, no-show, reembolso, disputa y chargeback.
- [ ] Definir criterios para perfiles destacados y anuncios; separar pago de
      visibilidad de la aprobacion de calidad o seguridad.

### 2.3 Confianza y responsabilidad

- [ ] Definir niveles visibles de verificacion: email, identidad, negocio,
      credencial profesional, seguro y proveedor destacado.
- [ ] Acordar evidencias solicitadas por categoria y su vigencia; no presentar
      una verificacion PawTrack como licencia o aval estatal.
- [ ] Definir politica para servicios a domicilio: direccion privada, contacto
      protegido, consentimiento del cliente y registro de llegada opcional.
- [ ] Definir flujo de queja, incidente de bienestar, suspension preventiva,
      apelacion y resolucion.
- [ ] Revisar terminos, privacidad, consentimiento y retencion de datos con
      asesoria legal local antes de habilitar reservas pagadas.

---

## 3. Arquitectura y limites de modulo

- [ ] Crear modulo `ServiceProviders` en Domain, Application, Infrastructure y
      API, respetando Clean Architecture y CQRS.
- [ ] Establecer contratos de integracion por MediatR/domain events; no acceder
      directamente a entidades de Auth, Pets, Notifications o Subscriptions.
- [ ] Definir interfaces de repositorio de lectura/escritura y consultas con
      `AsNoTracking`, paginacion y limites explicitos.
- [ ] Diseñar un modelo extensible de atributos por categoria que conserve campos
      tipados para reglas criticas y no dependa de JSON sin validacion.
- [ ] Crear una matriz de propiedad de datos: Auth posee usuarios, Pets posee
      mascotas, ServiceProviders posee perfiles/reservas, Payments posee transacciones
      y Notifications posee entregas.
- [ ] Definir eventos: proveedor enviado/aprobado/suspendido, servicio publicado,
      reserva creada/confirmada/cancelada/completada, pago actualizado e incidente.
- [ ] Acordar idempotencia y deduplicacion de comandos externos, especialmente
      reserva, pago y webhooks.
- [ ] Definir estrategia de versionado para DTOs publicos y contratos de webhooks.

---

## 4. Dominio y persistencia

### 4.1 Entidades y reglas base

- [ ] Crear `ServiceProvider` con `Id` Guid v7, `OwnerUserId`, tipo legal, nombre,
      descripcion, contacto, estado, datos de mapa, logo, timestamps y version de
      concurrencia.
- [ ] Crear `ServiceProviderCategory` y asociacion muchos-a-muchos si un proveedor
      puede ofrecer varias categorias.
- [ ] Crear `ProviderLocation` para sedes con direccion, coordenadas, telefono,
      horario, radio de cobertura, estado y sede principal.
- [ ] Crear `ProviderService` con categoria, nombre, descripcion, especie,
      modalidad, duracion, precio, moneda, impuestos, capacidad, requisitos, imagen,
      estado de publicacion y ventana de reserva.
- [ ] Crear `ProviderStaffMember` si se requiere que una empresa asigne reservas a
      personas concretas; incluir estado, especialidades y permisos limitados.
- [ ] Crear `ProviderAvailabilityRule` para horarios recurrentes, excepciones,
      cierres y anticipacion minima/maxima de reserva.
- [ ] Crear `ProviderBooking` con proveedor, servicio, mascota, cliente, sede o
      direccion protegida, inicio/fin, cantidad, precio capturado, estado y notas.
- [ ] Crear `ProviderBookingParticipant` solo si los servicios grupales requieren
      lista de mascotas/clientes independiente.
- [ ] Crear `ProviderPayment` o integrar mediante contrato con Payments, sin
      almacenar datos de tarjeta, secretos SINPE ni comprobantes sin control.
- [ ] Crear `ProviderVerification` y `ProviderVerificationDocument` con estado,
      evidencia privada, vencimiento, revalidacion y revisor.
- [ ] Crear `ProviderAuditLog` inmutable para acciones administrativas, cambios de
      estado, cambios de tarifa, descargas documentales y acciones sobre reservas.
- [ ] Crear `ProviderIncident` para reportes de seguridad/bienestar, evidencia,
      visibilidad, estado de investigacion y resolucion.

### 4.2 Maquinas de estado e invariantes

- [ ] Definir estados de proveedor: `Draft`, `PendingReview`, `Active`,
      `Rejected`, `Suspended`, `Deactivated`.
- [ ] Definir estados de servicio: `Draft`, `Published`, `Paused`, `Archived`.
- [ ] Definir estados de reserva: `Requested`, `AwaitingPayment`, `Confirmed`,
      `InProgress`, `Completed`, `CancelledByCustomer`, `CancelledByProvider`,
      `NoShow`, `Disputed`, `Refunded`.
- [ ] Implementar metodos de dominio para cada transicion y prohibir cambios
      invalidos desde handlers o controladores.
- [ ] Impedir reserva para proveedor no activo, servicio no publicado, mascota no
      propiedad del cliente, franja cerrada o requisito no cumplido.
- [ ] Impedir solapamiento para recursos exclusivos y sobreventa para capacidad
      grupal; proteger con indice/constraint transaccional, no solo validacion previa.
- [ ] Capturar precio, duracion, impuesto y politica de cancelacion al confirmar
      una reserva para preservar el historial.
- [ ] Evitar borrado fisico de reservas, pagos, verificacion, incidentes y auditoria.
- [ ] Definir UTC en persistencia y zona horaria `America/Costa_Rica` para entrada,
      visualizacion, ventanas de disponibilidad y recordatorios.

### 4.3 EF Core, migraciones e indices

- [ ] Configurar relaciones, restricciones, longitudes, precision monetaria y
      conversiones de enums en configuraciones de Infrastructure.
- [ ] Crear indices para directorio publico, propietario, categoria, ubicacion,
      estado, servicio publicado, franja de agenda, reserva cliente/proveedor y colas
      administrativas.
- [ ] Agregar restricciones unicas para perfil por regla de negocio, sede primaria,
      capacidad/slot y referencias idempotentes.
- [ ] Crear migraciones nuevas sin modificar migraciones aplicadas en ambientes
      compartidos.
- [ ] Preparar script de rollback validado y una estrategia de datos para activar
      el feature por etapas.
- [ ] Verificar que las consultas de directorio y agenda no introduzcan N+1 ni
      joins en memoria.

---

## 5. Application y API

### 5.1 Comandos de proveedor

- [ ] `RegisterServiceProviderCommand` con verificacion de email y proteccion
      anti-enumeracion.
- [ ] `CompleteServiceProviderProfileCommand` y `UpdateServiceProviderProfileCommand`.
- [ ] CRUD de categorias, sedes, servicios, fotos, reglas de disponibilidad,
      bloqueos excepcionales y miembros del equipo.
- [ ] `SubmitProviderVerificationCommand`, carga privada de documentos y solicitud
      de revalidacion.
- [ ] `AcceptBookingCommand`, `DeclineBookingCommand`, `RescheduleBookingCommand`
      y `CompleteBookingCommand` con reglas de estado y ownership.
- [ ] `CancelProviderBookingCommand` con motivo y calculo determinista de politica
      de reembolso.
- [ ] `RespondToProviderIncidentCommand` con controles de permisos y auditoria.

### 5.2 Comandos de cliente

- [ ] `CreateProviderBookingCommand` con idempotency key, bloqueo transaccional y
      validacion de mascota/slot/capacidad.
- [ ] `ConfirmProviderBookingPaymentCommand` o integracion con un intent de pago,
      segun la decision comercial.
- [ ] `CancelMyProviderBookingCommand` y `RequestBookingRescheduleCommand`.
- [ ] `CreateProviderReviewCommand` solo tras una reserva completada, una resena
      por reserva, con edicion limitada y moderacion.
- [ ] No exponer telefono, correo, direccion privada ni documentos en comandos o
      DTOs publicos.

### 5.3 Consultas y endpoints

- [ ] Directorio publico paginado con filtros validados: categoria, especie,
      modalidad, provincia/canton, distancia, precio, disponibilidad y verificado.
- [ ] Detalle publico con perfil, servicios publicados, sedes publicas, politicas,
      disponibilidad segura y calificacion agregada.
- [ ] Consultas autenticadas: mis proveedores, mis servicios, mi agenda, reservas
      entrantes, mis reservas, pagos, resenas e incidentes segun ownership.
- [ ] Endpoints admin: cola de aprobacion, detalle, revision, suspension,
      reactivacion, verificacion, incidentes, moderacion de resenas y exportes.
- [ ] Aplicar `[Authorize]` por rol/policy (`ProviderOwner`, `ProviderStaff`,
      `Admin`, cliente) y validar siempre ownership en el handler.
- [ ] Usar FluentValidation en pipeline para todos los requests; no validar reglas
      de negocio manualmente en controladores.
- [ ] Devolver `Result<T>`/Problem Details RFC 7807 sin mensajes internos de
      excepciones ni detalles sensibles.
- [ ] Aplicar request size limits, tipos MIME permitidos, rate limits y limites de
      pagina en todos los endpoints expuestos.
- [ ] Documentar OpenAPI, ejemplos de errores y politicas de compatibilidad.

---

## 6. Pagos, agenda y notificaciones

- [ ] Diseñar abstraccion de pagos con proveedores intercambiables y almacen de
      referencias externas, estado y montos; nunca datos bancarios completos.
- [ ] Implementar validacion de firma, replay protection, idempotencia y auditoria
      para cada webhook de pago.
- [ ] Mantener reconciliacion diaria de pagos, reembolsos y reservas en estado
      incierto.
- [ ] Definir expiracion de reservas pendientes de pago y job seguro con lock
      distribuido.
- [ ] Implementar disponibilidad recurrente y excepciones sin generar millones de
      slots persistidos innecesarios.
- [ ] Implementar reservas atomicas con control de concurrencia y reintento seguro.
- [ ] Enviar notificaciones in-app/email/push para registro, aprobacion, reserva,
      pago, recordatorio, cambio, cancelacion, no-show, reembolso y vencimiento.
- [ ] Aplicar preferencias de comunicacion, deduplicacion, rate limiting y colas
      de reintento a todas las notificaciones.
- [ ] Registrar Outbox y procesar eventos de manera idempotente antes de enviar
      comunicaciones externas.

---

## 7. Seguridad, privacidad y confianza

- [ ] Modelar policies de autorizacion y pruebas negativas para cada rol y recurso.
- [ ] Mantener la direccion exacta de servicio a domicilio privada hasta el punto
      definido por la reserva y consentimiento del cliente.
- [ ] Cifrar datos sensibles en transito y en reposo usando los servicios Azure
      aprobados; secretos solo mediante Key Vault.
- [ ] Almacenar documentos y evidencia en Blob privado, con URLs firmadas de vida
      corta emitidas tras verificar ownership/rol y auditando descargas.
- [ ] Validar contenido de archivos: MIME, firma binaria, extension, tamano,
      antivirus/malware scan y rechazo de contenido activo.
- [ ] Sanitizar texto enriquecido, limitar URLs externas y prevenir XSS en
      descripciones, resenas y notas.
- [ ] Diseñar protecciones contra scraping, enumeracion de proveedores, fraude de
      reservas, abuso de cupones y spam de solicitudes.
- [ ] Registrar eventos de seguridad sin guardar secretos, direcciones completas,
      URLs SAS ni PII innecesaria.
- [ ] Preparar retencion, exportacion, rectificacion y eliminacion/anonimizacion de
      datos conforme a la politica de privacidad aplicable.
- [ ] Completar threat model antes de beta y pruebas OWASP antes de produccion.

---

## 8. Frontend PWA

- [ ] Crear feature `service-providers` con API client tipado, hooks de React Query,
      componentes, paginas y pruebas co-localizadas.
- [ ] Añadir rutas publicas: directorio, detalle de proveedor y flujo de reserva.
- [ ] Añadir rutas de proveedor: registro, pendiente, portal, perfil, servicios,
      agenda, reservas, verificacion, sedes, equipo y analitica.
- [ ] Añadir rutas de cliente: mis reservas, detalle, reprogramacion, cancelacion,
      recibo y resena.
- [ ] Añadir pestanas Admin para aprobacion, verificacion, incidentes, moderacion y
      soporte de reservas.
- [ ] Integrar proveedores activos en mapa publico con consentimiento de ubicacion
      y sin mostrar direcciones privadas.
- [ ] Implementar filtros accesibles, URL state compartible, vacios, carga, error,
      paginacion y manejo offline coherente con PWA.
- [ ] Crear calendario responsive con zona horaria explicita, slots deshabilitados,
      capacidad restante y prevencion visual de doble envio.
- [ ] Diseñar formularios por categoria, con validacion cliente alineada al backend
      y explicacion de requisitos sin sustituir la validacion del servidor.
- [ ] Implementar carga de imagen/documento con progreso, restricciones, reintento
      y mensajes que no revelen informacion privada.
- [ ] Cumplir WCAG 2.2 AA: teclado, foco, contraste, labels, errores anunciados,
      lector de pantalla, zoom y objetivos tactiles.
- [ ] Revisar rendimiento movil: lazy routes, imagenes responsivas, placeholders,
      cache de consultas y limites de payload.

---

## 9. Administracion y soporte operativo

- [ ] Construir cola paginada para revisar proveedores y evidencias, con filtros,
      SLA, notas internas y decisiones auditadas.
- [ ] Permitir aprobacion, rechazo con motivo estandar, suspension urgente,
      reactivacion y solicitud de evidencia adicional.
- [ ] Construir vista de operaciones de reservas y pagos con busqueda por ID,
      proveedor, cliente, fecha, estado y referencia externa.
- [ ] Establecer RBAC de soporte con minimo privilegio y justificar/acceder a PII
      solo cuando sea necesario.
- [ ] Crear proceso de incidentes: ingreso, triage, evidencia, comunicacion,
      decision, apelacion, retencion y cierre.
- [ ] Establecer SLA de revision, reserva, reembolso, incidente y escalamiento.
- [ ] Crear herramientas de exportacion restringida y registrar quien exporta datos.
- [ ] Publicar manual de proveedor, manual de administracion y runbook de soporte.

---

## 10. Observabilidad, rendimiento y confiabilidad

- [ ] Definir SLIs/SLOs para directorio, reserva, pago, disponibilidad y entrega de
      notificaciones; acordar alertas y error budgets.
- [ ] Instrumentar Application Insights con eventos de negocio anonimizados:
      registro, aprobacion, busqueda, conversion, reserva, pago, cancelacion y fallo.
- [ ] Propagar correlation IDs entre API, jobs, outbox, pagos y notificaciones.
- [ ] Crear dashboards y alertas para fallos de reserva, conflictos de agenda,
      webhooks, colas, pagos pendientes, rechazo de archivos y latencia.
- [ ] Implementar health checks para dependencias criticas y degradacion controlada
      cuando pagos, Blob o notificaciones no esten disponibles.
- [ ] Programar jobs idempotentes con `IDistributedJobLock`: expiracion de reservas,
      recordatorios, vencimientos de verificacion y reconciliacion de pagos.
- [ ] Ejecutar pruebas de carga sobre directorio y reserva; definir presupuesto de
      latencia, tasa de errores y concurrencia esperada antes de lanzamiento.
- [ ] Revisar backup, restauracion, RPO/RTO, retencion de auditoria y plan de
      continuidad para datos de reservas/pagos.

---

## 11. Calidad y pruebas

- [ ] Crear pruebas unitarias de entidades: transiciones, invariantes, requisitos,
      capacidad, solapamientos, captura de precio y politicas de cancelacion.
- [ ] Crear pruebas unitarias de handlers: autorizacion, ownership, validacion,
      idempotencia, reintentos y errores de dependencias.
- [ ] Crear pruebas de repositorio/consulta para paginacion, filtros, ordenamiento,
      `AsNoTracking` y ausencia de N+1 en rutas criticas.
- [ ] Crear pruebas de integracion de API: registro, aprobacion, directorio,
      reservas concurrentes, archivos privados, pagos/webhooks y acceso denegado.
- [ ] Crear pruebas de contrato para el proveedor de pagos y webhooks.
- [ ] Crear pruebas de jobs para expiracion, recordatorios, vencimientos y
      reconciliacion, incluyendo ejecuciones repetidas.
- [ ] Crear pruebas frontend para formularios, filtros, agenda, estados de reserva,
      errores de API y controles de rol.
- [ ] Crear E2E con Playwright: proveedor se registra -> admin aprueba -> publica
      servicio -> cliente reserva -> pago/confirmacion -> finalizacion -> resena.
- [ ] Añadir pruebas de accesibilidad automatizadas y revision manual con teclado y
      lector de pantalla en las rutas principales.
- [ ] Ejecutar SAST, dependency scanning, secret scanning, DAST/OWASP y pruebas de
      autorizacion antes de produccion.
- [ ] Integrar todos los checks en CI, con artefactos diagnosticos y gates que no
      permitan desplegar cambios incompatibles.

---

## 12. Documentacion, despliegue y lanzamiento

- [ ] Actualizar `README`, documentacion de arquitectura, OpenAPI y diagramas del
      modulo y sus eventos.
- [ ] Actualizar terminos, politica de privacidad, politica de cancelacion y reglas
      de proveedores antes de activar captacion o pagos.
- [ ] Crear plan de migracion y feature flags: staff/admin, beta cerrada, directorio
      publico, reservas sin pago, pagos y disponibilidad general.
- [ ] Preparar datos de prueba anonimizados, cuentas de demo y casos operativos de
      soporte para UAT.
- [ ] Ejecutar UAT con al menos un proveedor de cada categoria inicial y documentar
      hallazgos/resoluciones.
- [ ] Preparar plan de comunicacion de incidentes y rollback sin perder reservas ni
      estados de pago.
- [ ] Validar infraestructura Azure, Key Vault, Blob privado, Application Insights,
      backups, alertas y RBAC antes de despliegue.
- [ ] Ejecutar validacion de produccion: migraciones, health checks, smoke tests,
      metricas, alertas y primer flujo de reserva controlado.
- [ ] Definir owner de producto, owner tecnico y responsable operativo para cada
      alerta, cola y proceso recurrente.

---

## 13. Criterios de salida para produccion

- [ ] Las categorias, precios, politicas, verificacion y responsabilidades estan
      aprobados por producto, operaciones y legal.
- [ ] Ningun endpoint permite acceder o mutar recursos de otro usuario/proveedor.
- [ ] Las reservas concurrentes no pueden superar la capacidad ni duplicar pagos.
- [ ] El directorio mantiene paginacion, filtros correctos y rendimiento dentro del
      SLO bajo carga acordada.
- [ ] Los documentos privados no son publicos y toda descarga autorizada queda
      auditada.
- [ ] Las pruebas unitarias, integracion, frontend, E2E, accesibilidad y seguridad
      pasan en CI.
- [ ] Dashboards, alertas, runbooks, backups y rutas de escalamiento estan probados.
- [ ] El lanzamiento se realiza mediante feature flag y puede revertirse sin borrar
      reservas, pagos ni auditoria.

---

## 14. Fuera de alcance inicial recomendado

- [ ] Marketplace multi-proveedor en un solo checkout.
- [ ] Algoritmo de ranking personalizado o recomendaciones con IA.
- [ ] Mensajeria directa sin moderacion, retencion ni herramientas de seguridad.
- [ ] Seguimiento GPS de paseadores en tiempo real.
- [ ] Integracion con sistemas de agenda externos sin contratos e idempotencia.
- [ ] Verificacion automatica con registros gubernamentales sin convenio formal.
- [ ] Liquidacion financiera automatica a proveedores antes de completar la capa de
      pagos, disputas, cumplimiento y reconciliacion.
