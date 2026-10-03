# Plan de implementacion: NALA como sistema diario para tiendas

> Estado: modelo hibrido por etapas aprobado como direccion de producto el 2026-10-02; cada gate legal, fiscal, comercial y de produccion permanece pendiente de evidencia y aprobacion de sus responsables.
> Corte de evidencia del codigo: 2026-10-02. La migracion nueva no se verifico aplicada.
> Alcance: tiendas de mascotas y veterinarias que venden productos al detalle. No describe la operacion clinica.
> Este archivo es el plan canonico de implementacion; el backlog ejecutable vive en [MASTER_TODO.md](MASTER_TODO.md).

## 1. Modelo hibrido aprobado

El objetivo es que NALA se convierta gradualmente en el sistema operativo diario
de una tienda, no reemplazar de golpe su POS, terminal de pago ni proveedor
fiscal. La primera salida es un piloto controlado de una tienda y una sede.

Para el piloto, la autoridad se define por dato:

- NALA administra catalogo publicado, solicitudes/pedidos y el stock operativo
  solo cuando todos los movimientos que cambian existencias quedan registrados
  en NALA.
- El POS o terminal existente conserva el cobro y la caja; el proveedor fiscal
  conserva el comprobante fiscal y la responsabilidad de su emision.
- No hay doble escritura ni sincronizacion bidireccional implicita de precios o
  stock. Para tiendas con POS, se registra por dato si la autoridad es NALA o el
  POS y como se sincroniza.
- La ruta actual de pedido rechaza productos con `StockOnHand` nulo o inferior a
  la cantidad solicitada. Por eso el piloto con pedidos online requiere stock
  declarado en NALA; una tienda debe registrar tambien sus ventas presenciales
  y ajustes si NALA es la autoridad. Si no puede, se requiere conector
  POS-authoritative o cambio de producto a solicitudes sin stock antes de usar
  pedidos; no existe hoy un fallback stockless.
- Sin integracion confiable ni registro de todas las operaciones presenciales,
  NALA no presenta su stock como disponibilidad real ni habilita reservas como
  garantia. Se puede mostrar catalogo, pero el flujo de pedido actual no acepta
  inventario ausente/insuficiente.
- NALA no procesa ni liquida el pago. El codigo actual permite que el cliente
  reporte un pago externo y que la tienda registre manualmente una verificacion
  y la referencia de una devolucion ejecutada fuera de NALA. Esto es una
  declaracion del actor, no una confirmacion bancaria automatica.

El piloto no constituye reemplazo de POS, certificacion fiscal, contrato,
aprobacion legal ni disponibilidad de produccion. Producto aprobo la direccion;
los responsables comerciales, operativos y legales deben aprobar sus propios
gates antes de cobrar, publicar claims o activar comercios.

## 2. Estado del codigo en el workspace

### Flujos implementados en codigo; despliegue no verificado

- Registro y aprobacion administrativa de tiendas, perfil publico, directorio, mapa y catalogo.
- Productos con nombre, descripcion, categoria, precio CRC, imagen, disponibilidad booleana y campo `StockOnHand` opcional.
- Carrito persistente limitado a una tienda y solicitud de pedido para retiro o entrega.
- Pedido con lineas que guardan nombre y precio unitario al crear la solicitud.
- Al aceptar, el backend intenta reservar la cantidad en `StockOnHand` en transaccion relacional `Serializable`; reserva con vencimiento de 15 minutos y job para vencer/liberar reservas.
- Rutas para que el cliente reporte un pago externo, la tienda registre verificacion manual y la tienda registre la referencia de una devolucion externa ya ejecutada.
- Confirmacion, rechazo y avance de estado por el propietario de la tienda; historial paginado para comprador y tienda.
- Entidad `StoreLocation`, CRUD bajo gates de StorePartner y `LocationId` opcional en el pedido.
- Analitica de pedidos entregados/cancelados, filtro tecnico por sede y exportacion StorePartner.
- Importacion CSV/JSON asincrona basica de productos mediante `ImportJob`, con clave de idempotencia del job, limite tecnico y gate de cuota.
- `POST /api/store-orders` requiere `Idempotency-Key`; el backend guarda una huella del payload, devuelve el pedido existente ante replay equivalente y rechaza reutilizacion con payload distinto. Checkout conserva la clave al reintentar. La unicidad esta declarada por cliente en el modelo EF; la migracion esta generada, no aplicada.
- Hay pruebas unitarias de dominio/handlers de tienda; no acreditan por si solas el flujo SQL concurrente ni un E2E de venta/caja.

Las migraciones `20261002193005_AddEnterpriseMarketplaceStockAndPaymentLink`,
`20261002201408_AddStoreOrderManualRefundEvidence` y
`20261002212937_AddStoreOrderIdempotencyAndProviderRefundAccounting`, junto con
las rutas/jobs referidos, son evidencia de codigo/esquema en el workspace, no
de migracion aplicada, despliegue, operacion con una tienda o pago confirmado
por una entidad bancaria. La verificacion de esos estados es `NO_VERIFICADO`.

### Brechas actuales para uso diario

- El stock actual es una cantidad editable, no un kardex inmutable. No hay movimientos de apertura/recepcion/venta/merma/ajuste/devolucion, compras, conteo fisico, SKU/codigo de barras, costo, proveedor, variantes ni stock por sede.
- La ruta `POST /api/store-orders` exige `StockOnHand` declarado y suficiente para cada linea antes de crear el pedido; no existe hoy una modalidad de solicitud sin stock. Esto debe resolverse por la fuente de verdad del piloto o mediante cambio de producto.
- La reserva al aceptar ya existe en codigo, pero requiere pruebas SQL de concurrencia, idempotencia y recuperacion ante fallos antes de usarse como garantia operativa.
- El checkout no elige sede ni envia `LocationId`; por tanto, la atribucion multi-sede no forma parte del flujo frontend actual.
- No hay membresias de empleados de tienda ni permisos por sucursal; el agregado `Store` tiene un `UserId` propietario unico.
- No hay venta presencial/online unificada, caja/cierre, conciliacion automatica ni factura fiscal de tienda en NALA. Reportar/verificar un pago manual o registrar un reembolso externo no procesa esos movimientos.
- Idempotencia de `POST /api/store-orders` implementada en API, handler y checkout; falta aplicar la migracion autorizadamente y verificar colision/concurrencia/rollback en SQL Server. El snapshot de nombre/precio existe al crear la solicitud; falta reconfirmar el monto/condiciones si cambian antes de aceptar.
- Las notificaciones actuales no usan entrega durable/outbox para todo el ciclo ni acreditan recepcion por el comprador.
- El importador no carga SKU ni existencias y requiere UI de vista previa/progreso, errores descargables, lotes/streaming y pruebas de aislamiento y duplicados.
- No se encontro un E2E de la jornada completa de caja/inventario. El runbook B2B existente no cubre ese flujo.

Fuentes de contraste: [B2B_ESTADO_ACTUAL.md](B2B_ESTADO_ACTUAL.md),
[API_REFERENCE.md](API_REFERENCE.md), [GUIA_QA_E2E.md](GUIA_QA_E2E.md),
[manual de tiendas](Manuales/MANUAL_TIENDAS.md),
[StoreOrdersController](../backend/src/PawTrack.API/Controllers/StoreOrdersController.cs),
[StoreOrderRepository](../backend/src/PawTrack.Infrastructure/Stores/StoreOrderRepository.cs),
[StoreProductImportProcessor](../backend/src/PawTrack.Application/Imports/StoreProductImport.cs).

## 3. Brechas confirmadas que requieren correccion

1. **Idempotencia (código implementado; gate relacional pendiente):** `POST /api/store-orders` exige `Idempotency-Key`, almacena hash del payload, reproduce la respuesta solo para el mismo payload y devuelve 409 `IDEMPOTENCY_KEY_CONFLICT` si se reutiliza con otro. Usa índice único cliente/clave. Pruebas unitarias cubren retry, conflicto y colisión simulada; quedan migración aplicada autorizadamente y pruebas de concurrencia/rollback con SQL Server.
2. **Estados y evidencia de pago:** hay estados para disponibilidad, pago reportado, verificacion manual, preparacion y cumplimiento. Documentar que `verify-payment` es una atestacion de la tienda tras revisar su cuenta, no confirmacion bancaria de NALA. Acordar nombre/semantica del estado `Paid` y evitar presentar el pedido como venta liquidada por el sistema.
3. **Transiciones de fulfillment:** probar en dominio/API que retiro y entrega no crucen estados incompatibles, que estados terminales no se muten y que cada cambio registre actor, fecha, motivo y evento inmutable.
4. **Validacion de catalogo:** agregar validador para `UpdateStoreProductCommand` e invariantes de dominio equivalentes al alta, incluyendo nombre, precio, stock no negativo y tienda activa.
5. **Monto acordado:** el pedido ya conserva snapshot de nombre, precio unitario, cantidad y total al crearse. Revalidar disponibilidad y precio al aceptar; si hay cambios, exigir confirmacion del cliente y persistir tambien moneda, impuestos y entrega cuando se definan.
6. **Notificaciones:** reemplazar efectos fire-and-forget por outbox/worker idempotente. Notificar al cliente las transiciones y persistir estado de entrega/error sin perder el pedido.
7. **Reportes:** agregar SQL por periodo con `America/Costa_Rica`, distinguir pedido, venta, pago externo reportado/verificado manualmente, devolucion externa y reembolso integrado (si algun dia se implementa).
8. **Pruebas:** agregar pruebas HTTP, SQL concurrente, reintentos, cambio de precio/stock, flujo de comprador/tienda y aislamiento por cuenta/sede. Las unitarias existentes no cierran estos gates.

## 4. Roadmap por gates

### Fase 0 - Acuerdo y preparacion del piloto

**Direccion aprobada:** piloto hibrido de una tienda y una sede. Aun no equivale a
aprobacion legal/fiscal, seleccion de comercio, contrato ni habilitacion de
produccion.

- Seleccionar comercio piloto, responsables de tienda/PawTrack, canales y semana de observacion.
- Aprobar autoridad por dato: NALA para catalogo, pedidos y stock operativo si todas las ventas/ajustes se registran alli; POS/proveedor para pago, caja y factura fiscal.
- Definir sincronizacion unidireccional por dato. Sin integracion confiable, el piloto registra ventas presenciales en NALA o mantiene el stock como no verificado y pedidos sujetos a confirmacion manual; nunca dos stocks editables.
- Confirmar quien puede usar el portal en esta fase (propietario), privacidad/datos de entrega, politica de rechazo/cancelacion/devolucion, horario y cumplimiento local con asesoria responsable.
- Definir lenguaje de estados: cliente reporta; tienda verifica manualmente; NALA no verifica banco, procesa fondos ni emite factura fiscal de tienda.
- Separar oferta Store tiers y compra de suscripcion de cualquier venta de productos de la tienda.

**Gate:** decision por escrito, autoridad de datos, responsable y limites del piloto; revision legal/fiscal y claims aprobados por sus propietarios. Sin esto no se activa una operacion presentada como POS.

### Fase 1 - Piloto hibrido e integridad minima de pedido

- Ejecutar con una ubicacion, catalogo/importacion inicial y flujo de pedido de una tienda.
- Validar en SQL Server la idempotencia de creacion ante reintentos concurrentes; completar idempotencia de transiciones y correlacion auditable antes de exponer el flujo a reintentos reales.
- Formalizar maquina de estados por retiro/entrega; distinguir solicitud, aceptacion con disponibilidad, pago reportado, verificacion manual, preparacion, entrega y estados terminales.
- Mantener el snapshot actual de lineas; revalidar precio/disponibilidad en aceptacion y requerir confirmacion del comprador si cambia el monto.
- Usar el POS externo para cobro/caja/factura. Registrar cualquier cambio presencial de stock en NALA mientras NALA sea autoridad; si el piloto no puede hacerlo, no exponer stock como real.
- Notificar al comprador y al comercio con entrega observable; no avanzar estado si el pedido no queda persistido.

**Gate del piloto:** reintentos no duplican solicitudes; la tienda y el comprador ven mismo monto/estado; cada salida de stock tiene origen; pago queda rotulado como externo/manual; operacion no depende de una promesa de sincronizacion no implementada.

### Fase 2 - Inventario diario en una sede

- Ampliar el catalogo con SKU, codigo de barras, unidad, costo, precio, estado archivado, proveedor y atributos necesarios por tipo de producto.
- Modelar stock como movimientos inmutables, no solo un numero editable: apertura, recepcion, venta/consumo, ajuste, merma y devolucion.
- Incorporar compras y recepcion de mercaderia, conteo/ajuste con motivo y alertas por stock minimo/vencimiento cuando aplique.
- Evolucionar `StockOnHand` y la reserva al aceptar (ya presentes en codigo) a movimientos atomicos auditables; liberar reserva al cancelar/rechazar/expirar y reconciliarla con el ledger.
- Probar concurrencia SQL Server, restricciones/locking y rollback para impedir stock negativo o doble venta; evaluar `rowversion` o actualizacion condicional segun el diseño final.
- Mostrar kardex, existencias actuales y alertas en el portal de tienda.

**Gate:** bajo compras concurrentes y pedidos simultaneos, el stock queda consistente y cada unidad puede explicarse desde sus movimientos.

### Fase 3 - Caja, pagos, devoluciones y fiscalidad

- Registrar ventas presenciales y online con una sola regla de inventario; prevenir doble conteo si el pago se realiza en terminal externa.
- Separar `Order`, `Sale`, `Payment` y `ElectronicInvoice`; conservar proveedor, identificador externo, monto, moneda, estado, timestamps y claves idempotentes.
- Elegir proveedor de pagos/fiscal con contrato, sandbox, webhooks autenticados, conciliacion, notas de credito y manejo de 429/5xx/timeouts.
- Hasta homologar proveedor, mantener cobro fuera de NALA. El registro existente de verificacion manual solo documenta que la tienda declara haber revisado su cuenta; no es confirmacion bancaria de NALA.
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
- Fortalecer el importador CSV/JSON asincrono de productos que ya existe (`ImportJob`): agregar vista previa/UI, tenant isolation del job/reporte, reporte descargable, metricas, streaming/lotes pequenos y pruebas de duplicados. El import actual no lleva SKU ni existencias. No leer directamente la base de datos de un POS.
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
