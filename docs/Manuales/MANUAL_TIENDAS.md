# Manual de Tiendas de Mascotas - PawTrack CR

**Version:** 1.0  
**Rol:** `Store`  
**Audiencia:** propietarios y operadores de tiendas aprobadas  
**Ultima actualizacion:** 2026-09-10

## 1. Alcance y registro

Una tienda aprobada aparece en el directorio y puede publicar un catalogo para
recibir solicitudes de pedido. El registro inicial es publico en
`/tienda/registro`; mientras se revisa la solicitud, consulta
`/tienda/pendiente`.

El administrador aprueba o suspende la tienda. Una cuenta de tienda solo puede
administrar sus propios productos, pedidos, perfil y, cuando corresponda, sus
sedes.

## 2. Panel

Despues de iniciar sesion, abre `/tienda/portal`. Las secciones son:

- **Perfil:** nombre, descripcion, direccion, telefono, sitio web y contacto de
  WhatsApp opcional.
- **Productos:** alta, edicion, disponibilidad, categoria, precio en colones e
  imagen JPEG, PNG o WebP de hasta 5 MB.
- **Ordenes:** revisar solicitudes, confirmar disponibilidad, aceptar o
  rechazar, preparar y marcar entrega o retiro.
- **Analitica:** metricas del periodo y export CSV cuando el plan lo habilita.
- **Sedes:** gestion avanzada de ubicaciones cuando el plan y el alcance
  comercial lo permitan.

## 3. Planes y limites

| Tier           | Capacidad documentada                                              |
| -------------- | ------------------------------------------------------------------ |
| `StoreBasic`   | Directorio y catalogo base; no es un plan comercial de pago activo |
| `StorePlus`    | Catalogo, pedidos in-app y operaciones de pedidos                  |
| `StorePartner` | Gates tecnicos para analitica y multiples sedes                    |

Los precios y la disponibilidad comercial se rigen por
[PRICING_AND_PLANS.md](../PRICING_AND_PLANS.md). Aunque existen endpoints
tecnicos de Partner, la decision comercial vigente mantiene multi-sede fuera
del alcance comercial actual hasta nueva aprobacion.

## 4. Gestion de pedidos

1. Abre **Ordenes** y revisa el producto, cantidad y modalidad de
   cumplimiento.
2. Confirma que hay disponibilidad antes de aceptar.
3. Actualiza el estado conforme avanza la preparacion.
4. Coordina directamente con el cliente el retiro o la entrega.
5. Marca como entregado solo cuando la entrega haya ocurrido.

PawTrack comunica la solicitud, pero no vende ni intermedia el producto, no
cobra comision y no garantiza inventario. Cualquier pago se coordina entre
cliente y tienda; la integracion de pagos no forma parte del alcance vigente.

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
