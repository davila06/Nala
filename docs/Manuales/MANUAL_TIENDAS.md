# Manual de Tiendas de Mascotas - PawTrack CR

**Version:** 1.0  
**Rol:** `Store`  
**Audiencia:** propietarios y operadores de tiendas aprobadas  
**Ultima actualizacion:** 2026-10-02

## 1. Alcance y registro

Una tienda aprobada aparece en el directorio y puede publicar un catalogo para
recibir solicitudes de pedido. El registro inicial es publico en
`/tienda/registro`; mientras se revisa la solicitud, consulta
`/tienda/pendiente`.

**Alcance actual:** el portal no es un POS/ERP. El codigo incluye una cantidad
`StockOnHand` por producto y reserva temporal cuando la tienda acepta una
solicitud, pero no kardex, compras, ventas presenciales, caja ni stock por sede.
El cliente puede reportar un pago externo; la tienda puede registrar que lo
verifico manualmente y guardar la referencia de una devolucion ejecutada fuera
de NALA. La app no procesa fondos, consulta al banco ni emite factura fiscal.
La migracion, el piloto y la disponibilidad de estas rutas en cada entorno no
estan verificados; usalas solo cuando el responsable del piloto confirme que el
entorno esta habilitado. El alcance futuro esta en
[ROADMAP_TIENDAS_USO_DIARIO.md](../ROADMAP_TIENDAS_USO_DIARIO.md).

El administrador aprueba o suspende la tienda. Una cuenta de tienda solo puede
administrar sus propios productos, pedidos, perfil y, cuando corresponda, sus
sedes.

## 2. Panel

Despues de iniciar sesion, abre `/tienda/portal`. Las secciones son:

- **Perfil:** nombre, descripcion, direccion, telefono, sitio web y contacto de
  WhatsApp opcional.
- **Productos:** alta, edicion, disponibilidad, categoria, precio en colones,
  existencias simples e imagen JPEG, PNG o WebP de hasta 5 MB. La cantidad no
  tiene historial de movimientos.
- **Ordenes:** revisar solicitudes, confirmar disponibilidad, aceptar o
  rechazar, preparar y marcar entrega o retiro.
- **Analitica:** metricas del periodo y export CSV cuando el plan lo habilita.
- **Sedes:** gestion avanzada de ubicaciones cuando el plan y el alcance
  comercial lo permitan.

## 3. Planes y limites

| Tier           | Capacidad documentada                                              |
| -------------- | ------------------------------------------------------------------ |
| `StoreBasic`   | Directorio y catalogo base; no es un plan comercial de pago activo |
| `StorePlus`    | Catalogo y solicitudes de pedido in-app                            |
| `StorePartner` | Analitica avanzada/export y CRUD tecnico de sedes                  |

Los tiers son gates tecnicos; no implican venta publica aprobada. Los precios
y la disponibilidad comercial se rigen por
[PRICING_AND_PLANS.md](../PRICING_AND_PLANS.md). Aunque existen endpoints
tecnicos de Partner, la decision comercial vigente mantiene multi-sede fuera
del alcance comercial actual hasta nueva aprobacion.

## 4. Gestion de pedidos

1. Abre **Ordenes** y revisa productos, cantidades y modalidad.
2. Antes de aceptar, confirma que el inventario y precio mostrados siguen
  vigentes. En codigo, aceptar reserva las cantidades disponibles por 15
  minutos; no cubre ventas presenciales que no se registren en NALA.
3. Si el cliente reporta SINPE, comprueba el abono en el canal bancario de la
  tienda. La accion manual de verificar pago solo registra la declaracion y
  referencia de la tienda; no consulta ni recibe confirmacion del banco.
4. Actualiza el estado conforme avanza la preparacion y coordina retiro/entrega
  directamente hasta que haya integracion y reglas aprobadas.
5. Marca como entregado solo cuando la entrega haya ocurrido. Una devolucion se
  ejecuta fuera de PawTrack; la ruta actual solo permite registrar su referencia.

PawTrack comunica y administra estados tecnicos del pedido; no vende ni
intermedia el producto, no cobra comision ni procesa el pago. El stock escalar
y la reserva temporal no equivalen a un inventario auditable ni garantizan
existencias en POS/produccion. La confirmacion de pago es manual por la tienda,
no automatica por NALA. La oferta comercial y disponibilidad se rigen por
[PRICING_AND_PLANS.md](../PRICING_AND_PLANS.md).

## 5. Buenas practicas

- Mantén precios y disponibilidad actualizados.
- No publiques telefonos personales en la descripcion.
- No marques una orden como entregada para acelerar metricas.
- Para un incidente de pedido, conserva el ID de orden y abre el canal de
  soporte definido por PawTrack.

## 6. Rutas publicas relacionadas

Los clientes consultan `/tiendas` y los perfiles publicos. El directorio puede
mostrar el placement publicitario `Directory`. La tienda no debe
intentar usar endpoints de otra tienda: el backend valida ownership por cuenta.
