# Roadmap: NALA como software diario para tiendas veterinarias

> Estado: propuesta tecnica y operativa, pendiente de decisiones de producto y aprobacion legal/comercial.
> Corte de analisis: 2026-09-26.
> Alcance: tiendas de mascotas y veterinarias que venden productos al detalle. No describe la operacion clinica.
> Fuente del estado actual: codigo en `backend/src/PawTrack.Domain/Stores`, `backend/src/PawTrack.Application/Stores`, `backend/src/PawTrack.API/Controllers` y `frontend/src/features/stores`.

## 1. Decision de producto

El modulo actual es un canal de catalogo y solicitudes de pedido. No es un POS/ERP: no lleva existencias, compras, caja diaria, pagos procesados ni facturacion fiscal de la tienda.

Este roadmap asume que NALA evolucionara a un sistema diario para una tienda individual, primero en una sola ubicacion, y que despues soportara sucursales y personal. Durante la transicion, la tienda conserva su terminal de pago y proveedor fiscal. NALA no debe afirmar que procesa SINPE, confirma depositos o garantiza inventario hasta que exista un flujo implementado y homologado.

La decision P0 es definir quien tiene autoridad sobre cada dato. Para la primera version diaria propuesta:

- NALA es autoridad de productos, precios publicados, existencias, pedidos y movimientos de inventario que registre en NALA.
- El proveedor fiscal y el procesador de pago siguen siendo autoridad de sus respectivos comprobantes y transacciones.
- Si la tienda ya opera un POS, se define una sola fuente de verdad para inventario y precios; no se habilita sincronizacion bidireccional implicita.

## 2. Estado verificado de la aplicacion

### Implementado

- Registro y aprobacion administrativa de tiendas, perfil publico, directorio, mapa y catalogo.
- Productos con nombre, descripcion, categoria, precio CRC, imagen y disponibilidad booleana.
- Carrito persistente limitado a una tienda y solicitud de pedido para retiro o entrega.
- Pedido con lineas que guardan nombre y precio unitario al crear la solicitud.
- Confirmacion, rechazo y avance de estado por el propietario de la tienda; historial paginado para comprador y tienda.
- Entidad `StoreLocation`, CRUD bajo gates de StorePartner y `LocationId` opcional en el pedido.
- Analitica de pedidos entregados/cancelados, filtro tecnico por sede y exportacion StorePartner.
- Importacion CSV/JSON asincrona basica de productos mediante `ImportJob`, con clave de idempotencia, limite tecnico de 5 MB/10.000 filas y gate de cuota.
- Tests unitarios de dominio/handlers y un smoke B2B de aislamiento; el runbook marca el escenario enterprise opt-in como dependiente de datos sembrados.

### No implementado o no verificable en este alcance

- `StoreProduct` no tiene SKU/codigo de barras, costo, impuesto, proveedor, variantes ni stock.
- No existen movimientos de inventario, compras/recepciones, reservas de unidades, transferencias ni conteos fisicos de tienda.
- El checkout no elige sede ni envia `LocationId`; por tanto, la atribucion multi-sede no forma parte del flujo frontend actual.
- No hay membresias de empleados de tienda ni permisos por sucursal; el agregado `Store` tiene un `UserId` propietario unico.
- Las solicitudes no son ventas confirmadas ni reservas de producto. Pago, entrega, impuesto y disponibilidad se coordinan fuera de NALA.
- Hay campos y estados heredados de referencia/SINPE, pero el controlador de pedidos no expone una ruta para reportar pago. No tratar la referencia como transaccion ni `PaymentReported` como confirmacion bancaria.
- La notificacion de nuevo pedido se dispara fuera de una cola durable; el cambio de estado no envia una notificacion al comprador desde el handler inspeccionado.
- No aparece una suite especifica de UI/E2E para el ciclo de compra de tienda en los paths de tests Store. La suite B2B existente cubre principalmente aislamiento y exportacion.
- El importador de productos carga todo el archivo en memoria; su aislamiento de reportes/jobs y UX de progreso/errores siguen siendo gates pendientes.

Fuentes: [B2B_ESTADO_ACTUAL.md](B2B_ESTADO_ACTUAL.md), [pendientesTiendas.md](pendientesTiendas.md), [API_REFERENCE.md](API_REFERENCE.md), [GUIA_QA_E2E.md](GUIA_QA_E2E.md).

## 3. Brechas confirmadas que requieren correccion

1. **Consistencia del pedido:** `POST /api/store-orders` no usa una clave de idempotencia. Reintentos por timeout pueden crear pedidos duplicados. Agregar `Idempotency-Key` con unicidad por cliente/operacion y una respuesta repetible.
2. **Estados y pago:** aclarar separadamente solicitud, disponibilidad aceptada, pago externo pendiente/verificado, preparacion y entrega. El dominio permite confirmar desde `PendingPayment`; no existe evidencia de pago asociada al pedido. Definir si confirmar significa aceptar disponibilidad o aceptar venta, y cambiar nombres/estados en consecuencia.
3. **Transiciones de fulfillment:** validar en el dominio que pedidos `Pickup` no puedan pasar a `OutForDelivery` y pedidos `Delivery` no puedan pasar a `ReadyForPickup`. Toda cancelacion debe guardar timestamp, actor, motivo y transicion; no permitir mutaciones desde estados terminales.
4. **Validacion de catalogo:** incorporar un validador para `UpdateStoreProductCommand` y invariantes de dominio equivalentes a la creacion, incluidos nombre no vacio, precio valido y pertenencia/estado de la tienda.
5. **Total mostrado:** recalcular precio/disponibilidad en el servidor al confirmar el pedido y mostrar al comprador cualquier cambio antes de aceptar; mantener snapshot inmutable de cantidad, nombre, precio, moneda, impuesto y entrega cuando esos conceptos se habiliten.
6. **Notificaciones:** reemplazar efectos fire-and-forget por outbox/worker idempotente. Notificar al cliente las transiciones relevantes y registrar aceptacion/fallo del proveedor sin perder el pedido.
7. **Reportes:** ejecutar agregaciones en SQL; usar explicitamente `America/Costa_Rica` en filtros y agrupaciones. Excluir o clasificar pedidos no entregados, cancelados, devueltos y aun no cobrados.
8. **Evidencia de pruebas:** agregar pruebas HTTP positivas/negativas, concurrencia, reintentos, flujo frontend, cambio de precio, stock y aislamiento por tienda/sucursal.

## 4. Roadmap por gates

### Fase 0 - Alcance, ownership y operacion

**Prioridad P0. No iniciar transacciones de inventario antes de aprobar esto.**

- Aprobar el modelo operativo: NALA reemplaza POS para una tienda o integra un POS existente.
- Definir la autoridad por entidad: producto, precio, stock, venta, pago y factura.
- Aprobar roles iniciales, limites por plan, politicas de cancelacion/devolucion, horarios, entrega/retiro y datos del comprador.
- Revisar Ley 8968, normativa de consumidor, fiscalidad y obligaciones de productos regulados con asesor local.
- Definir moneda, impuestos, redondeo y calendario de Costa Rica; separar factura de la tienda de la factura de suscripcion PawTrack.

**Gate:** decisiones registradas, responsable asignado y claims comerciales alineados.

### Fase 1 - Integridad del catalogo y pedidos

- Corregir validacion de alta/edicion y eliminar estados ambiguos de pago.
- Implementar idempotencia para crear pedido y cambiar estados, con correlacion y auditoria.
- Formalizar state machine por fulfillment, actor y motivo; incluir cancelacion solicitada por comprador y respuesta de tienda.
- Congelar snapshot comercial del pedido y pedir confirmacion del cliente si cambia el precio.
- Definir expiracion de solicitudes, tratamiento de pedidos no atendidos y notificaciones por transicion.
- Registrar eventos y efectos externos mediante outbox; no descartar silenciosamente fallos de correo/push.

**Gate:** reintentos no duplican pedidos, no existen transiciones imposibles y el cliente ve el estado/monto aceptado por ambas partes.

### Fase 2 - Inventario diario en una sede

- Ampliar el catalogo con SKU, codigo de barras, unidad, costo, precio, estado archivado, proveedor y atributos necesarios por tipo de producto.
- Modelar stock como movimientos inmutables, no solo un numero editable: apertura, recepcion, venta/consumo, ajuste, merma y devolucion.
- Incorporar compras y recepcion de mercaderia, conteo/ajuste con motivo y alertas por stock minimo/vencimiento cuando aplique.
- Reservar stock de forma atomica al aceptar una orden; liberar reserva al cancelar/rechazar/expirar.
- Usar concurrencia optimista SQL Server (`rowversion`) y restricciones para impedir stock negativo o doble venta.
- Mostrar kardex, existencias actuales y alertas en el portal de tienda.

**Gate:** bajo compras concurrentes y pedidos simultaneos, el stock queda consistente y cada unidad puede explicarse desde sus movimientos.

### Fase 3 - Caja, pagos, devoluciones y fiscalidad

- Registrar ventas presenciales y online con una sola regla de inventario; prevenir doble conteo si el pago se realiza en terminal externa.
- Separar `Order`, `Sale`, `Payment` y `ElectronicInvoice`; conservar proveedor, identificador externo, monto, moneda, estado, timestamps y claves idempotentes.
- Elegir proveedor de pagos/fiscal con contrato, sandbox, webhooks autenticados, conciliacion, notas de credito y manejo de 429/5xx/timeouts.
- Hasta homologar proveedor, ofrecer registro de pago externo como **declarado/pendiente de verificacion**, nunca como verificado por presion del boton.
- Definir devolucion total/parcial, anulacion, nota de credito, diferencias de caja y auditoria con MFA para acciones sensibles.

**Gate:** conciliacion entre venta, pago, caja e invoice comprobada en sandbox y UAT; reconciliacion manual disponible ante caida del proveedor.

### Fase 4 - Personal y sucursales

- Crear membresias de tienda con invitacion/revocacion y permisos minimos (propietario, administrador, cajero, inventario, solo lectura).
- Hacer de `StoreLocation` un limite operativo: producto/stock/precio si difiere, caja, horarios, retiro, pedidos, asignacion de personal y auditoria.
- Requerir o resolver una sede activa al hacer checkout; enviar `LocationId` desde UI y API, no inferirla por texto.
- Implementar transferencias entre sedes con salida/recepcion y estados para evitar disponibilidad ficticia durante el traslado.
- Probar BOLA entre tiendas y entre sedes para todo endpoint que acepte `storeId`, `locationId` o resource ID.

**Gate:** empleado de sede A no puede consultar ni mutar caja, inventario o pedidos de sede B sin permiso explicito; reporte consolidado concuerda con reportes por sede.

### Fase 5 - Integraciones y adopcion

- Publicar contratos `/api/v1` de catalogo, disponibilidad, stock, pedidos, ventas y eventos; scopes por tienda/sede, rotacion, sandbox y webhooks firmados.
- Fortalecer el importador CSV/JSON asincrono de productos que ya existe (`ImportJob`): agregar vista previa/UI, tenant isolation del job/reporte, reporte descargable, metricas, streaming/lotes pequenos y pruebas de duplicados. El formato actual no tiene SKU porque el catalogo aun no lo modela. No leer directamente la base de datos de un POS.
- Priorizar conectores segun uso real medido en tiendas piloto. Preferir API/webhook; ofrecer exportacion/importacion supervisada para software local sin API.
- Implementar sincronizacion en una sola direccion por dato cuando la autoridad sea un POS externo; guardar `ExternalId` y cursor por proveedor/sede.
- Medir fallos de sincronizacion, diferencias de stock, latencia y ultima sincronizacion visible.

**Gate:** dos tiendas piloto pueden importar el catalogo y sostener una semana de operacion sin duplicados, descuadres ni carga manual repetida.

### Fase 6 - Produccion y resiliencia

- Ejecutar migraciones en staging y produccion con backup restaurable, verificacion de conteos y rollback documentado.
- Configurar alertas, health checks, dashboards, retencion, soporte y runbooks para pedidos atascados, stock negativo, pago ambiguo y proveedor caido.
- Probar carga, restauracion, red lenta y reintentos. Agregar modo offline solo si NALA sera el POS de caja; definir cola local cifrada, idempotencia y resolucion de conflictos.
- Completar accesibilidad movil/tablet, lector de codigo de barras/escanner compatible, estados de carga/error y flujos de caja por teclado.
- Ejecutar UAT firmado por propietario y cajero antes de activar gradualmente cada tienda.

**Gate:** migraciones, observabilidad, backup/restore, seguridad y UAT aprobados; feature flag permite rollback sin perder transacciones.

## 5. Arquitectura recomendada

Mantener el monolito modular Clean Architecture/CQRS actual. Ampliar el modulo `Stores` y aislar adaptadores externos en infraestructura; no crear microservicios hasta que volumen/equipos lo justifiquen.

- Agregados sugeridos: `StoreProduct`, `StoreLocation`, `InventoryMovement`, `StockReservation`, `PurchaseReceipt`, `StoreOrder`, `StoreSale` y, solo al aprobar integracion, `StorePayment`/`ElectronicInvoice`.
- `Store` es tenant; `LocationId` es boundary de autorizacion en las funciones por sucursal.
- Los comandos validan tenant, permiso, estado, idempotencia y concurrencia en servidor; el frontend nunca es autoridad sobre precio o stock.
- Los eventos transaccionales salen por outbox. Los webhooks entrantes se persisten en inbox y se procesan de forma idempotente.
- Mantener secretos de proveedores en Key Vault; no registrar PAN, credenciales bancarias, API tokens ni PII innecesaria.

## 6. Pruebas requeridas

- Unitarias: invariantes de inventario, state machine, calculos CRC y redondeo, reserva/liberacion y transiciones terminales.
- Integracion SQL: dos ventas simultaneas del ultimo producto, doble recepcion, transferencia incompleta, reintento idempotente y rollback.
- API/security: actor de otra tienda/sede, permisos revocados, precio/stock manipulados por cliente, export fuera de tenant y replay de webhook.
- Frontend/E2E: alta de producto, recepcion, venta/carro, confirmacion, pago pendiente/conciliado, preparacion, entrega/retiro, cancelacion y devolucion.
- Resiliencia: outbox atrasado, proveedor 429/5xx, timeout ambiguo, reinicio durante una transaccion, backup/restore y red intermitente.

## 7. Metricas de salida

- Exactitud de inventario por sede y cantidad de discrepancias por semana.
- Pedidos duplicados, pedidos vencidos sin respuesta y porcentaje atendido dentro del SLA.
- Tiempo de alta de producto y tiempo de caja por venta.
- Diferencias entre pedidos, ventas, pagos, cierres e invoices.
- Tasa de cancelacion, devolucion y sustitucion por falta de stock.
- Errores y edad del backlog de outbox/inbox/sincronizaciones.
- Adopcion semanal por usuario operativo, no solo por cuenta propietaria.

## 8. Criterio de lanzamiento

NALA puede llamarse **catalogo y pedidos para tiendas** mientras siga el alcance actual. Solo se debe llamar **software diario de tienda** cuando inventario, roles, pedidos, caja, conciliacion, fiscalidad aplicable, auditoria, soporte y recuperacion tengan pruebas/UAT de extremo a extremo en staging. La disponibilidad de una pantalla o entidad de sede no cierra por si sola ese gate.
