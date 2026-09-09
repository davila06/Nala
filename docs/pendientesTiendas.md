# PawTrack CR - TODO Enterprise de Tiendas

> Alcance: tiendas de mascotas, catalogo, productos, carrito, pedidos, pagos SINPE,
> sedes, analytics, portal de vendedor, directorio publico y administracion.
>
> Objetivo: llevar el modulo de tiendas desde un MVP operativo hasta una operacion
> comercial enterprise, segura, auditable y escalable en Costa Rica.
>
> Estado de referencia: 2026-09-08.

## Como usar este documento

- `[ ]` Pendiente.
- `[~]` En progreso o parcialmente implementado.
- `[x]` Implementado y validado con evidencia.
- Cada tarea debe cerrar con codigo, prueba automatizada, captura/flujo validado,
  decision de producto o procedimiento operativo.
- No marcar una tarea como `[x]` solo porque exista el endpoint: debe existir
  ownership, persistencia, autorizacion, manejo de errores y prueba apropiada.

---

# 1. Estado actual

## 1.1 Funcionalidad ya disponible

- [x] Registro publico de tiendas.
- [x] Creacion de usuario con rol `Store` y tienda en estado `Pending`.
- [x] Verificacion de correo durante el registro.
- [x] Aprobacion y suspension administrativa.
- [x] Directorio publico de tiendas activas.
- [x] Detalle publico de tienda con productos disponibles.
- [x] Integracion de tiendas con mapa.
- [x] Perfil basico de tienda.
- [x] Catalogo de productos por tienda.
- [x] Categorias de producto: alimento, accesorios, grooming, salud, juguetes,
      ropa y otros.
- [x] Precio en CRC, descripcion, disponibilidad booleana e imagen en Blob Storage.
- [x] Reemplazo y limpieza de imagen anterior en Blob Storage.
- [x] Carrito persistente de una sola tienda.
- [x] Checkout con retiro en tienda o entrega a domicilio.
- [x] Nota opcional del cliente.
- [x] Creacion de pedido con lineas y referencia de pago.
- [~] Campos heredados de pago manual mediante SINPE, conservados solo por
  compatibilidad tecnica; no forman parte del flujo activo.
- [x] Confirmacion manual por la tienda.
- [x] Maquina de estados de pedidos.
- [x] Historial de pedidos del cliente.
- [x] Bandeja de pedidos entrantes para la tienda.
- [x] Actualizacion de estado por la tienda.
- [x] Notificaciones de nuevos pedidos y cambios relevantes.
- [x] Analytics basico y avanzado para `StorePlus`/`StorePartner`.
- [x] Modelo de sedes `StoreLocation`.
- [x] CRUD de sedes para `StorePartner`.
- [x] Atribucion backend de pedidos a sede.
- [x] Gates de plan en backend para funciones premium.
- [x] Consultas paginadas principales en backend.
- [x] Carga en lote de productos del pedido para evitar N+1.
- [x] Indices SQL para estado de tienda, productos, pedidos, referencias de pago
      y sedes.

## 1.2 Rutas principales verificadas

### Cliente

- `/tiendas`: directorio publico.
- `/map?storeId={id}`: mapa y detalle de tienda.
- `/mis-pedidos`: historial del comprador.

### Vendedor

- `/tienda/registro`: registro de tienda.
- `/tienda/pendiente`: estado de aprobacion.
- `/tienda/portal`: dashboard.
- `/tienda/portal/productos`: catalogo.
- `/tienda/portal/ordenes`: pedidos entrantes.
- `/tienda/portal/analitica`: analytics.
- `/tienda/portal/sedes`: sedes `StorePartner`.

### Administracion

- Pestaña administrativa de tiendas pendientes y tiendas revisadas.
- Revision, aprobacion y suspension de tiendas.

## 1.3 Endpoints principales

- `POST /api/stores/register`
- `GET /api/public/stores`
- `GET /api/public/stores/{id}`
- `GET /api/stores/mine`
- `PUT /api/stores/profile`
- `GET /api/stores/products`
- `POST /api/stores/products`
- `PUT /api/stores/products/{id}`
- `DELETE /api/stores/products/{id}`
- `POST /api/stores/products/{id}/image`
- `GET /api/stores/me/locations`
- `POST /api/stores/me/locations`
- `PUT /api/stores/me/locations/{id}`
- `PATCH /api/stores/me/locations/{id}/active`
- `GET /api/stores/me/analytics`
- `POST /api/store-orders`
- `GET /api/store-orders/mine`
- `GET /api/store-orders/incoming`
- `PUT /api/store-orders/{id}/confirm`
- `PUT /api/store-orders/{id}/status`

## 1.4 Limites actuales del MVP

El modulo es apto para un piloto controlado con pocas tiendas y SINPE manual.
No debe presentarse todavia como:

- Plataforma de pagos integrada.
- Marketplace con inventario transaccional.
- Operacion fiscal completa.
- Plataforma de fulfillment o logistica.
- Sistema multi-sede con inventario independiente.
- Marketplace con checkout multi-vendedor.

---

# 2. P0 - Decisiones bloqueantes de negocio, legal y operacion

Estas decisiones deben aprobarse antes de ampliar el codigo, porque determinan
el dominio de pedidos, pagos, responsabilidades y soporte.

## 2.1 Modelo comercial

- [x] PawTrack unicamente comunica pedidos; no intermedia fondos ni cobra
      comision por cada pedido.
- [x] No se implementara comision transaccional en esta fase.
- [ ] Confirmar tiers `StoreBasic`, `StorePlus` y `StorePartner` y sus precios.
- [ ] Definir limites por tier: productos, pedidos, analytics y soporte.
- [x] Una cuenta solo puede administrar una tienda en esta fase.
- [x] Se omiten miembros, cajeros, administradores, analistas y permisos por sede.
- [x] Se omite el checkout multi-tienda.
- [ ] Definir si el catalogo puede ser publico sin aceptar pedidos.

## 2.2 Entrega, retiro y responsabilidad

- [x] La logistica de entrega depende completamente de la tienda.
- [x] PawTrack no realiza, contrata ni garantiza el transporte.
- [ ] Definir zonas, tarifas, tiempos estimados y exclusiones que cada tienda
      debe publicar antes de aceptar pedidos.
- [ ] Definir datos minimos de direccion y tratamiento de ubicacion del cliente.
- [ ] Definir procedimiento de retiro, verificacion de identidad y no-show.
- [ ] Definir politica de cancelacion por cliente y tienda.
- [ ] Definir politica para producto incorrecto, incompleto, danado o no entregado.
- [ ] Definir responsabilidad por productos veterinarios, alimentos, suplementos
      y productos con requisitos especiales.
- [ ] Definir reglas para productos prohibidos, vencidos o de venta restringida.

## 2.3 Legal, privacidad y fiscalidad

- [ ] Revisar terminos de marketplace, condiciones de compra y responsabilidad.
- [ ] Definir politica de privacidad para datos de comprador, direccion, telefono
      y notas del pedido.
- [ ] Definir si se requiere consentimiento separado para compartir datos con la
      tienda y con el repartidor.
- [ ] Definir factura electronica, comprobante, impuestos y responsabilidades de
      cada parte en Costa Rica.
- [ ] Definir retencion, exportacion, rectificacion y eliminacion de datos de
      pedidos y pagos.
- [ ] Definir KYC de comercios, datos tributarios y verificacion de identidad.
- [ ] Revisar requisitos regulatorios para cobro, intermediacion y reembolsos.

## 2.4 Pagos

- [x] Se omite la implementacion de pagos integrados y conciliacion automatica.
- [x] Cualquier pago se gestiona manualmente entre cliente y tienda, fuera de
      PawTrack, segun el acuerdo comercial que ambos definan.
- [x] PawTrack no confirma depositos, no custodia fondos y no procesa reembolsos.
- [ ] Revisar que la interfaz y los textos no presenten a PawTrack como operador
      de pagos ni como garante de una transferencia.

---

# 3. P0 - Pedidos, pagos e inventario

## 3.1 Idempotencia y consistencia

- [ ] Agregar `Idempotency-Key` obligatoria para crear pedidos.
- [ ] Persistir la clave con alcance por cliente y endpoint.
- [ ] Garantizar que reintentos devuelvan el mismo pedido, no uno nuevo.
- [x] Se omite idempotencia de pagos porque PawTrack no procesara pagos en esta
      fase.
- [ ] Agregar idempotencia o versionado optimista a transiciones de pedido.
- [ ] Definir `CorrelationId` para pedido, notificaciones y soporte.
- [ ] Mantener las referencias de pago heredadas solo por compatibilidad, sin
      ampliar su comportamiento ni presentarlas como pagos procesados.
- [ ] Probar reintentos concurrentes y caidas despues de persistir.

## 3.2 Inventario y reserva

- [x] No se manejara inventario ni existencias dentro de PawTrack en esta fase.
- [x] El pedido representa una solicitud comercial, no una reserva automatica de
      unidades ni una garantia de disponibilidad.
- [x] Recomendacion: la tienda revisa disponibilidad y confirma el pedido antes
      de apartar fisicamente el producto.
- [x] Si la tienda decide apartarlo, la reserva es manual y queda bajo su
      responsabilidad, por el plazo que comunique al cliente.
- [x] Mostrar una leyenda clara: crear el pedido no garantiza existencia,
      precio final, entrega ni retiro hasta que la tienda lo confirme.
- [ ] Mantener inventario, reservas atomicas, transferencias y control de
      sobreventa fuera del alcance hasta aprobar una nueva fase.

## 3.3 Maquina de estados

Estados actuales:

```text
PendingPayment
PaymentReported
Confirmed
Preparing
ReadyForPickup
OutForDelivery
Delivered
Cancelled
Rejected
```

- [x] Solo la tienda puede cambiar el estado operativo de un pedido.
- [ ] Definir formalmente las transiciones permitidas por la tienda.
- [x] Agregar estado `Rejected` para pedido no aceptado.
- [x] Exigir motivo en cancelacion y rechazo, tanto en dominio como en portal
      de tienda.
- [x] La actualizacion de estado permite cancelacion por tienda con motivo
      obligatorio.
- [ ] Notificar al cliente cada cambio de estado relevante.
- [ ] Mantener historial inmutable de transiciones.
- [ ] Evitar doble confirmacion o doble finalizacion bajo concurrencia.

## 3.4 Cancelacion, disputa y reembolso

- [x] La tienda controla la aceptacion, cancelacion y cierre operativo del pedido.
- [ ] Definir si el cliente puede solicitar cancelacion, sin modificar el estado
      directamente; la tienda decide y registra el resultado.
- [x] La actualizacion de estado permite cancelacion por tienda con motivo
      obligatorio.
- [ ] Definir ventana de cancelacion por estado.
- [ ] Definir reembolso total, parcial o sin reembolso.
- [ ] Crear flujo de disputa cliente-tienda.
- [ ] Crear cola administrativa de disputas.
- [ ] Registrar evidencia, decision, actor y fecha.
- [ ] Notificar a ambas partes.
- [ ] Probar que una disputa no modifica silenciosamente el historial comercial.

## 3.5 Total y calculo comercial

- [ ] Definir subtotal, descuento, impuesto, entrega y total final.
- [ ] Persistir snapshot inmutable de precios al crear el pedido.
- [ ] No recalcular historicos usando el precio actual del catalogo.
- [ ] Definir reglas de redondeo en CRC.
- [ ] Definir cupones y promociones, aunque se dejen fuera del primer release.
- [ ] Validar que el total del frontend y backend coincida.
- [ ] Crear pruebas de propiedad para calculos y redondeos.

---

# 4. Futuro - Pagos enterprise y conciliacion

> Fuera del alcance actual. Los pagos continuan siendo manuales entre cliente y
> tienda. Este bloque solo se conserva como referencia para una futura fase y no
> debe bloquear la implementacion actual de pedidos y catalogo.

## 4.1 Modelo de pago

- [ ] Crear `PaymentIntent` separado del pedido, solo si PawTrack decide operar
      pagos en una fase posterior.
- [ ] Asociar intent de pago a `StoreOrder`, monto esperado, moneda, referencia,
      expiracion y estado.
- [ ] Estados recomendados: `Created`, `AwaitingPayment`, `UserReported`,
      `ProviderReceived`, `Matched`, `Confirmed`, `Rejected`, `Expired`,
      `RefundPending`, `Refunded`, `ManualReview`.
- [ ] No activar ni confirmar solo porque el cliente presiono `Ya pague`.
- [ ] Mantener compatibilidad con SINPE manual durante la migracion.

## 4.2 Ingesta SINPE futura

- [ ] Crear adaptador de proveedor autorizado.
- [ ] Validar firma, autenticidad y replay protection.
- [ ] Persistir `ProviderTransactionId` unico.
- [ ] Normalizar referencia sin perder valor original.
- [ ] Validar cuenta destino, monto, moneda y ventana temporal.
- [ ] Hash o tokenizacion de cuenta origen; guardar solo lo necesario.
- [ ] Guardar hash del payload y no el payload completo por defecto.
- [ ] Procesar webhooks de forma idempotente.
- [ ] Enviar casos ambiguos a revision manual.
- [ ] Emitir eventos por outbox al confirmar pago.
- [ ] Implementar reintentos con backoff y dead-letter operativo.

## 4.3 Conciliacion futura

- [ ] Crear motor de reglas versionado.
- [ ] Registrar resultado: `Exact`, `AmountOnly`, `ReferenceOnly`, `Ambiguous`,
      `Rejected`.
- [ ] Registrar razones, confianza, regla aplicada y actor.
- [ ] Crear conciliacion diaria y reporte de diferencias.
- [ ] Permitir reintento seguro de transacciones fallidas.
- [ ] Permitir reverso administrativo auditado.
- [ ] Crear alertas para pagos sin pedido, pagos duplicados y montos diferentes.
- [ ] Probar duplicados, pagos parciales, pagos tardios y referencias incorrectas.

## 4.4 Comprobantes futuros

- [ ] Generar comprobante de pedido para cliente.
- [ ] Generar comprobante operativo para tienda.
- [ ] Incluir subtotal, impuestos, entrega, total, referencia y estado.
- [ ] Definir PDF, descarga y envio por email.
- [ ] Auditar acceso a comprobantes.
- [ ] Separar comprobante de pago de factura fiscal si legalmente son conceptos
      diferentes.

---

# 5. P1 - Catalogo, perfil y moderacion

## 5.1 Perfil comercial

- [ ] Completar telefono, WhatsApp, sitio web, horario y canales de contacto.
- [ ] Agregar metodos de entrega, retiro y zonas atendidas.
- [ ] Agregar politicas de cancelacion, devolucion y garantia.
- [ ] Agregar datos comerciales y tributarios segun decision legal.
- [ ] Separar informacion publica de informacion privada.
- [ ] Agregar estados: `Pending`, `Active`, `Suspended`, `Closed`, `Rejected`.
- [ ] Definir motivo y auditoria para suspension o rechazo.

## 5.2 Productos

- [ ] Agregar SKU o identificador comercial.
- [ ] Agregar marca, unidad, peso, dimensiones y atributos relevantes.
- [ ] Agregar precio de costo solo si es necesario y siempre privado.
- [ ] Agregar precio de venta versionado.
- [ ] Agregar cantidad, unidad de medida y disponibilidad.
- [ ] Agregar estado `Draft`, `Published`, `Archived`, `Suspended`.
- [ ] Preservar producto historico en pedidos aunque se archive.
- [ ] Agregar validacion de precio positivo y limites razonables.
- [ ] Agregar moderacion de categoria, contenido y enlaces.

## 5.3 Archivos e imagenes

- [ ] Validar MIME real, extension, firma, tamanio y dimensiones.
- [ ] Aplicar antivirus o malware scanning cuando este disponible.
- [ ] Guardar imagenes en contenedor Blob privado si no necesitan acceso publico.
- [ ] Usar URLs firmadas con expiracion para contenido privado.
- [ ] Generar thumbnails optimizados.
- [ ] Eliminar Blob anterior solo despues de confirmar reemplazo correcto.
- [ ] Registrar actor y fecha de cambios de imagen.

## 5.4 Directorio

- [ ] Implementar paginacion visible en frontend.
- [ ] Agregar filtros por categoria, zona, entrega, retiro y estado.
- [ ] Definir reglas transparentes de posicionamiento patrocinado.
- [ ] Evitar abuso de `isFeatured` para alterar ranking sin auditoria.
- [ ] Hacer visible el badge `StorePartner` en directorio, mapa y perfil.
- [ ] Agregar estados de carga, vacio y error.
- [ ] Hacer URLs compartibles para filtros y tienda.
- [ ] Medir vistas, clicks y conversiones con privacidad y retencion definida.

---

# 6. P1 - Sedes y experiencia de checkout

> Sedes, multi-sede, inventario por sede y permisos por sede quedan fuera de la
> fase actual. Se asume una sola sede operativa por tienda.

- [x] Se manejara una sola sede por tienda en esta fase.
- [ ] Consolidar en perfil y checkout la direccion, horario y condiciones de esa
      unica sede.
- [ ] Dejar multi-sede, `locationId`, inventario por sede y permisos por sede
      para una fase posterior.

## 6.1 Carrito

- [ ] Documentar la restriccion actual de una sola tienda por carrito.
- [ ] Mostrar aviso claro al cambiar de tienda y perder/reemplazar carrito.
- [ ] Definir si se conservara un carrito por tienda.
- [ ] Validar producto, precio y disponibilidad nuevamente en checkout.
- [ ] Restaurar estado de error sin perder silenciosamente el carrito.
- [ ] Evitar checkout con cantidades negativas, cero o superiores al limite.
- [ ] Definir comportamiento offline y sincronizacion.

## 6.2 Accesibilidad y UX

- [ ] Cumplir WCAG 2.2 AA en directorio, catalogo, carrito y checkout.
- [ ] Agregar labels, foco visible, teclado y mensajes de error asociados.
- [ ] Revisar responsive en movil para carrito y checkout.
- [ ] Mostrar claramente estado del pedido, pago pendiente y siguiente accion.
- [ ] Evitar revelar PII en URLs, logs o mensajes publicos.
- [ ] Agregar confirmacion antes de acciones destructivas.

---

# 7. P1 - Seguridad, tenancy y autorizacion

## 7.1 Ownership

- [ ] Crear matriz de permisos para cliente, Store Owner, Manager, Staff,
      Analyst, Admin y Support.
- [ ] Verificar ownership en cada lectura y mutacion de tienda, producto, sede,
      pedido, pago, comprobante y analytics.
- [ ] Probar BOLA/IDOR con IDs de otra tienda y otro cliente.
- [ ] Impedir que un Store vea pedidos de otra tienda.
- [ ] Impedir que un operador de sede vea datos fuera de su sede si esa es la
      politica aprobada.
- [ ] Impedir que un cliente modifique pedidos ajenos.

## 7.2 PII y datos financieros

- [ ] Minimizar direccion, telefono y notas del cliente.
- [ ] Ocultar datos de contacto hasta que el flujo comercial lo justifique.
- [ ] No guardar credenciales bancarias.
- [ ] No registrar payloads de pago con datos innecesarios.
- [ ] Enmascarar referencias y datos sensibles en logs.
- [ ] Auditar exportaciones, comprobantes y consultas administrativas.
- [ ] Definir retencion de pedidos, pagos, soporte y auditoria.

## 7.3 Protecciones de API

- [ ] Revisar rate limits por endpoint, usuario, tienda y tenant.
- [ ] Mantener limites de tamano y cantidad de lineas del pedido.
- [ ] Validar strings, URLs, imagenes y notas contra XSS e inyeccion.
- [ ] Aplicar headers de seguridad y CORS minimo.
- [ ] Agregar proteccion contra abuso de catalogo e imagenes.
- [ ] Integrar SAST, dependency scanning, secret scanning y DAST.

---

# 8. P1/P2 - Administracion y operaciones

## 8.1 Panel administrativo

- [ ] Cola paginada de tiendas pendientes.
- [ ] Revision de documentos, datos comerciales y contenido.
- [ ] Aprobar, rechazar, suspender y reactivar con motivo obligatorio.
- [ ] Buscar por tienda, usuario, estado, fecha, plan y zona.
- [ ] Ver pedidos y pagos por ID, tienda, cliente, referencia y periodo.
- [ ] Revisar pagos ambiguos, duplicados y disputas.
- [ ] Ejecutar reembolsos solo con permiso y auditoria.
- [ ] Ver historial completo de cambios.
- [ ] Separar permisos Admin y Support con minimo privilegio.

## 8.2 Soporte y SLA

- [ ] Definir SLA por `StoreBasic`, `StorePlus` y `StorePartner`.
- [ ] Crear categorias de ticket: pago, pedido, entrega, producto, cuenta y fraude.
- [ ] Definir escalamiento y horarios de atencion.
- [ ] Registrar actor, motivo, evidencia y resultado.
- [ ] Permitir comunicacion controlada sin exponer PII innecesaria.
- [ ] Crear runbook de pedido duplicado, pago no conciliado, sobreventa y disputa.

## 8.3 Jobs y resiliencia

- [ ] Job de expiracion de pedidos pendientes.
- [ ] Job de conciliacion de pagos.
- [ ] Job de notificaciones pendientes.
- [ ] Locks distribuidos para jobs en varias instancias.
- [ ] Reintentos con backoff y dead-letter.
- [ ] Alertas por errores, backlog y tiempos de procesamiento.
- [ ] Procedimiento de replay seguro.

---

# 9. P2 - Analytics y observabilidad

## 9.1 Metricas comerciales

- [ ] Definir zona horaria canonica: Costa Rica.
- [ ] Definir ingresos brutos, netos, cancelados y reembolsados.
- [ ] Definir pedidos creados, pagados, confirmados, entregados y cancelados.
- [ ] Definir ticket promedio y formula de calculo.
- [ ] Definir productos mas vendidos por unidades e ingresos.
- [ ] Definir metricas por sede.
- [ ] Definir retencion y granularidad de datos.
- [ ] Evitar incluir datos personales en dashboards.
- [ ] Agregar exportacion CSV/PDF con permisos y auditoria.

## 9.2 Observabilidad

- [ ] Correlation ID desde checkout hasta pago, pedido y notificacion.
- [ ] Application Insights para errores y latencia.
- [ ] Dashboards de checkout, pagos, pedidos, inventario y notificaciones.
- [ ] Alertas por fallas de pago, duplicados, sobreventa y backlog.
- [ ] Health checks de base de datos, Blob, email y proveedor de pagos.
- [ ] Definir SLI/SLO para directorio, checkout, pagos y actualizacion de estado.
- [ ] Medir disponibilidad y tiempos p50/p95/p99.
- [ ] Registrar metricas de negocio anonimizadas.

---

# 10. P2 - Pruebas y calidad

## 10.1 Backend

- [ ] Pruebas de dominio para todas las transiciones y reglas de cancelacion.
- [ ] Pruebas de handlers con ownership, planes, inventario y errores.
- [ ] Pruebas de repositorio con SQL Server real o fixture equivalente.
- [ ] Pruebas de paginacion, filtros, orden y ausencia de N+1.
- [ ] Pruebas de concurrencia para inventario y transiciones.
- [ ] Pruebas de idempotencia de pedidos y pagos.
- [ ] Pruebas de expiracion y jobs con lock distribuido.
- [ ] Dejar pruebas de pagos duplicados, parciales, tardios y ambiguos para una
      futura fase de pagos integrada.
- [ ] Pruebas de BOLA/IDOR y privilege escalation.

## 10.2 Frontend

- [ ] Tests del directorio, detalle, catalogo y filtros.
- [ ] Tests del carrito de una tienda y cambio de tienda.
- [ ] Tests de cantidades, errores y reintentos.
- [ ] Tests del checkout Pickup/Delivery.
- [ ] Tests de referencia SINPE y reporte de pago.
- [ ] Tests del historial de pedidos.
- [ ] Tests del portal de tienda y transiciones.
- [ ] Tests de sedes y analytics.
- [ ] Tests de accesibilidad de rutas criticas.

## 10.3 E2E y release

- [ ] E2E: registro -> aprobacion -> catalogo -> carrito -> pedido -> pago ->
      confirmacion -> entrega.
- [ ] E2E de rechazo, cancelacion, expiracion y reembolso.
- [ ] E2E de StorePartner con dos sedes.
- [ ] E2E de acceso cruzado denegado.
- [ ] Ejecutar Playwright contra backend, SQL y Azurite sembrados.
- [ ] Ejecutar smoke test despues de deploy.
- [ ] Agregar artefactos de trazas, screenshots y logs a CI.
- [ ] Ejecutar regresion completa antes de cambiar estados o contratos.

---

# 11. Plan de implementacion recomendado

## Fase 0 - Alineacion y baseline

**Objetivo:** congelar decisiones que afectan el dominio.

- Aprobar modelo comercial, tiers y responsabilidad. No se contempla comision
  transaccional en esta fase.
- Aprobar politica de cancelacion, entrega, reembolso e impuestos.
- Documentar que la coordinacion de pagos es manual y externa a PawTrack.
- Documentar el modelo simple de cuenta, tienda y una sede.
- Crear feature flags solo para el catalogo, pedidos y comunicacion de estados.
- Levantar baseline de pruebas, rendimiento y seguridad.

**Salida:** decisiones firmadas, contratos de API definidos y criterios de
aceptacion aprobados.

## Fase 1 - Integridad del pedido

**Objetivo:** evitar duplicados, sobreventa y estados inconsistentes.

- Idempotencia de pedido y transiciones.
- Validacion de disponibilidad declarada por la tienda, sin inventario automatico.
- Rechazo, cancelacion y cierre operativo.
- Snapshot comercial inmutable.
- Historial de estados y auditoria.

**Salida:** ningun reintento crea solicitudes duplicadas y cada solicitud deja
claro que la tienda debe confirmar disponibilidad antes de apartar el producto.

## Fase 2 - Operacion manual y experiencia de pedido

**Objetivo:** documentar y hacer visible el flujo manual sin convertir a PawTrack
en operador de pagos.

- Textos claros sobre solicitud no garantizada.
- Estado y motivo de confirmacion o rechazo por parte de la tienda.
- Notificaciones de cambios de estado.
- Runbook de coordinacion manual entre cliente y tienda.
- Comprobantes de solicitud y estado, sin presentarlos como comprobantes de pago.

**Salida:** cliente y tienda entienden que PawTrack comunica la solicitud y que
la tienda controla disponibilidad, entrega y cualquier pago manual.

La conciliacion automatica, los webhooks, reembolsos y `PaymentIntent` quedan
deferidos y no forman parte del release actual.

## Fase 3 - Catalogo y sedes

**Objetivo:** completar la operacion comercial de tiendas.

- Inventario por sede.
- Seleccion de sede en checkout.
- Analytics por sede en frontend.
- Productos archivados y precios versionados.
- Perfil comercial, horarios, zonas y politicas.
- Moderacion de imagenes y contenido.

**Salida:** una tienda multi-sede puede operar catalogos e inventarios sin mezclar
pedidos ni permisos.

## Fase 4 - Operacion enterprise

**Objetivo:** soportar administracion, soporte y escalamiento.

- Panel de pagos, disputas y reembolsos.
- RBAC de owner/manager/staff/analyst/support.
- SLA y runbooks.
- Jobs con lock distribuido.
- Observabilidad, dashboards y alertas.
- Exportaciones auditadas.

**Salida:** operaciones puede resolver incidentes sin acceso excesivo ni cambios
manuales directos en la base de datos.

## Fase 5 - Calidad y rollout

**Objetivo:** lanzar de forma controlada.

- Completar unit, integration, frontend y E2E.
- Ejecutar pruebas de seguridad y accesibilidad.
- UAT con tiendas reales piloto.
- Activar por feature flag.
- Monitorear errores, conversion, pagos y cancelaciones.
- Definir rollback y soporte de lanzamiento.
- Ampliar gradualmente el numero de tiendas.

**Salida:** release reproducible, reversible y con evidencia de UAT.

---

# 12. Checklist de seguimiento de avance

## Gate A - MVP listo para piloto

- [ ] Registro y aprobacion funcionan en ambiente limpio.
- [ ] Catalogo e imagenes funcionan con Blob configurado.
- [ ] Cliente puede crear pedido de una tienda.
- [ ] Tienda recibe y actualiza el pedido.
- [ ] SINPE manual esta documentado y no se confunde con confirmacion bancaria.
- [ ] No existen accesos cruzados entre clientes y tiendas.
- [ ] Logs no exponen PII o secretos.
- [ ] Smoke test automatizado pasa.
- [ ] Runbook de pago manual y pedido atascado esta aprobado.

## Gate B - Integridad comercial

- [ ] Idempotencia implementada y probada.
- [ ] Inventario y concurrencia implementados.
- [ ] Expiracion, cancelacion y rechazo definidos.
- [ ] Snapshot de precios y total persistido.
- [ ] Historial de estados auditado.
- [ ] Comprobantes disponibles.
- [ ] Pruebas HTTP y de concurrencia pasan.

## Gate C - Pagos enterprise

- [ ] Proveedor SINPE autorizado confirmado.
- [ ] Webhook/API autenticado.
- [ ] Replay protection verificada.
- [ ] Conciliacion automatica probada.
- [ ] Cola de revision manual disponible.
- [ ] Reembolsos auditados.
- [ ] Alertas de pagos ambiguos y duplicados activas.
- [ ] Contrato de soporte financiero aprobado.

## Gate D - Multi-sede y portal enterprise

- [ ] Checkout permite seleccionar sede.
- [ ] Inventario por sede funciona.
- [ ] Analytics filtra por sede.
- [ ] RBAC por organizacion y sede pasa pruebas.
- [ ] Exportaciones estan autorizadas y auditadas.
- [ ] Badge y posicionamiento Partner tienen reglas transparentes.

## Gate E - Produccion

- [ ] UAT firmado por tiendas piloto.
- [ ] Unit, integration, frontend, E2E y accesibilidad pasan en CI.
- [ ] SAST, dependency scan, secret scan y DAST pasan.
- [ ] Dashboards, alertas y health checks estan activos.
- [ ] Backup, restore, RPO y RTO fueron probados.
- [ ] Runbooks de incidentes estan disponibles.
- [ ] Rollback fue probado.
- [ ] Feature flags permiten activar y desactivar la operacion.
- [ ] Legal, producto, finanzas y operaciones aprobaron el lanzamiento.

---

# 13. Criterios de salida enterprise

- [ ] Todo pedido es idempotente y auditable.
- [ ] No existe sobreventa bajo concurrencia.
- [ ] Los precios, impuestos, entrega y total quedan congelados en el pedido.
- [ ] Ningun pago se confirma solo por declaracion del usuario.
- [ ] Pagos ambiguos, duplicados o tardios tienen tratamiento definido.
- [ ] Cancelaciones, disputas y reembolsos tienen reglas y responsables.
- [ ] Ningun usuario puede leer o mutar recursos de otro tenant.
- [ ] La informacion privada del cliente se comparte solo cuando corresponde.
- [ ] Productos e imagenes tienen validacion y moderacion.
- [ ] Las sedes respetan inventario, permisos y analytics propios.
- [ ] El frontend refleja los contratos reales del backend.
- [ ] CI cubre los flujos criticos y bloquea regresiones.
- [ ] Operaciones tiene observabilidad, soporte, auditoria y rollback.
- [ ] El lanzamiento puede hacerse gradualmente sin perder pedidos ni auditoria.

---

# 14. Fuentes de codigo y documentacion

- `frontend/src/features/stores/api/storesApi.ts`
- `frontend/src/features/stores/api/storeOrdersApi.ts`
- `frontend/src/features/stores/components/CheckoutModal.tsx`
- `frontend/src/features/stores/components/CartDrawer.tsx`
- `frontend/src/features/stores/pages/StoreDashboardPage.tsx`
- `frontend/src/features/stores/pages/StoreOrdersPage.tsx`
- `frontend/src/features/stores/pages/StoreProductsPage.tsx`
- `frontend/src/features/stores/pages/StoreLocationsPage.tsx`
- `frontend/src/features/stores/pages/StoreAnalyticsPage.tsx`
- `backend/src/PawTrack.API/Controllers/StoresController.cs`
- `backend/src/PawTrack.API/Controllers/StoreOrdersController.cs`
- `backend/src/PawTrack.Application/Stores/StoreCommands.cs`
- `backend/src/PawTrack.Application/Stores/StoreProductCommands.cs`
- `backend/src/PawTrack.Application/Stores/StoreOrderCommands.cs`
- `backend/src/PawTrack.Domain/Stores/StoreOrder.cs`
- `backend/src/PawTrack.Infrastructure/Stores/StoreRepository.cs`
- `backend/src/PawTrack.Infrastructure/Stores/StoreOrderRepository.cs`
- `docs/sinpe.md`
- `docs/todolist-b2b-enterprise.md`
- `docs/SERVICE_PROVIDERS_ENTERPRISE_TODOLIST.md`

---

## Resumen ejecutivo

La base actual permite un piloto de tiendas con catalogo, pedidos y SINPE manual.
El orden recomendado es:

1. decisiones comerciales y legales;
2. idempotencia, inventario y consistencia de pedidos;
3. pagos auditables y conciliacion;
4. sedes, catalogo y experiencia de checkout;
5. soporte, observabilidad, seguridad y pruebas enterprise;
6. rollout gradual con feature flags.

No se recomienda escalar el numero de tiendas ni anunciar pagos automaticos hasta
cerrar los gates de integridad comercial y conciliacion.
