# PawTrack CR - Borrador legal y operativo para tiendas

> **Estado:** borrador para revision de producto, operaciones y asesoria legal.
> **No constituye asesoramiento juridico ni reemplaza la revision de un abogado
> en Costa Rica.**
>
> **Version:** 0.1  
> **Fecha:** 2026-09-08  
> **Alcance:** tiendas de mascotas, catalogo, solicitudes de pedido, entrega,
> retiro y comunicaciones entre cliente y tienda.

---

## 1. Decisiones de alcance aprobadas para revision

1. PawTrack funciona como plataforma de comunicacion de pedidos.
2. PawTrack no compra, vende, revende ni intermedia los productos de la tienda.
3. PawTrack no cobra comision por pedido en esta fase.
4. PawTrack no custodia fondos, no procesa pagos y no confirma transferencias.
5. Una cuenta de usuario puede administrar una sola tienda.
6. Cada tienda opera una sola sede en esta fase.
7. La entrega y el retiro dependen completamente de la tienda.
8. La tienda es responsable de aceptar, rechazar, preparar, entregar, cancelar
   y cerrar sus pedidos.
9. PawTrack no maneja inventario ni garantiza la existencia de un producto.
10. Crear un pedido representa una solicitud y no una reserva automatica de
    unidades, precio, entrega o retiro.
11. Los pagos, si existen, se acuerdan y gestionan manualmente entre cliente y
    tienda fuera de PawTrack.
12. No se implementan por ahora comisiones, payouts, conciliacion bancaria,
    reembolsos, multi-tienda, multi-sede ni permisos por sede.

Estas decisiones deben quedar aprobadas por escrito antes del lanzamiento
comercial.

---

## 2. Naturaleza de la relacion

### 2.1 PawTrack

PawTrack proporciona software para:

- publicar informacion comercial suministrada por la tienda;
- recibir una solicitud de pedido del cliente;
- transmitir la solicitud a la tienda;
- mostrar el estado operativo comunicado por la tienda;
- facilitar notificaciones y trazabilidad tecnica del pedido.

PawTrack no es vendedor, distribuidor, fabricante, transportista, repartidor,
agente de cobro, custodio de dinero ni garante de la operacion comercial entre
las partes.

### 2.2 Tienda

La tienda es el proveedor y vendedor del producto. La tienda debe:

- suministrar informacion exacta, vigente y no engañosa;
- definir precios, disponibilidad real, condiciones, horarios y zonas;
- revisar cada solicitud antes de aceptarla;
- confirmar o rechazar pedidos oportunamente;
- preparar y entregar o poner a disposicion el pedido;
- gestionar consultas, reclamos, cambios, devoluciones y garantias;
- cumplir las obligaciones legales aplicables a sus productos y actividad;
- mantener actualizados sus datos y los de su sede.

### 2.3 Cliente

El cliente debe:

- proporcionar datos correctos para contacto, entrega o retiro;
- revisar las condiciones informadas por la tienda;
- atender las instrucciones y horarios de la tienda;
- pagar directamente a la tienda mediante el mecanismo que ambas partes
  acuerden;
- verificar el pedido al recibirlo o retirarlo;
- comunicar reclamos primero a la tienda y conservar la evidencia relevante.

---

## 3. Pedido, confirmacion y reserva

### 3.1 Pedido como solicitud

La accion de crear un pedido en PawTrack no constituye por si sola una venta
confirmada. La solicitud queda sujeta a la revision de la tienda, incluyendo:

- disponibilidad real del producto;
- precio vigente;
- zona y capacidad de entrega;
- horario de preparacion o retiro;
- restricciones del producto;
- datos suficientes del cliente.

La interfaz debe mostrar esta condicion de forma visible antes y despues de
crear el pedido.

### 3.2 Recomendacion sobre la reserva sin inventario

Como PawTrack no manejara inventario, se recomienda el siguiente criterio:

1. El cliente crea una **solicitud de pedido**.
2. La tienda revisa manualmente disponibilidad y condiciones.
3. La tienda cambia el pedido a **confirmado** solo si puede atenderlo.
4. La tienda puede apartar fisicamente el producto bajo su propia operacion.
5. La tienda informa al cliente el plazo de retiro o entrega y cualquier
   condicion de vencimiento del apartado.
6. Si no puede atenderlo, la tienda lo rechaza o cancela con motivo.

El apartado fisico, su duracion, perdida, sustitucion o liberacion son
responsabilidad exclusiva de la tienda. PawTrack no debe denominar el pedido
como "reserva garantizada" mientras no exista inventario transaccional.

### 3.3 Estados recomendados

La nomenclatura debe revisarse contra el codigo actual. Para esta fase se
recomienda una maquina operativa similar a:

```text
Solicitado
Confirmado
Preparando
ListoParaRetiro
EnEntrega
Entregado
Rechazado
Cancelado
```

Solo la tienda puede cambiar el estado operativo. El cliente puede solicitar
una cancelacion o reportar un problema, pero no cambia el estado directamente.
Cada rechazo o cancelacion debe registrar motivo y fecha.

Los estados heredados relacionados con `PendingPayment` o `PaymentReported`
no deben interpretarse como confirmacion de un pago por PawTrack. Si se
mantienen temporalmente por compatibilidad, la UI y la documentacion deben
explicarlo claramente.

---

## 4. Entrega y retiro

### 4.1 Responsabilidad de la tienda

La tienda decide y comunica:

- si ofrece entrega, retiro o ambas modalidades;
- zonas atendidas y zonas excluidas;
- tarifas, minimos y tiempos estimados;
- horarios de preparacion, entrega y retiro;
- identidad o condiciones para retirar;
- procedimiento ante ausencia del cliente;
- evidencia de entrega cuando sea necesaria.

PawTrack no garantiza tiempos, cobertura, calidad, integridad o disponibilidad
del transporte.

### 4.2 Datos de direccion

La direccion de entrega se debe tratar como dato privado del cliente. Solo debe
compartirse con la tienda y, cuando corresponda, con la persona que efectue la
entrega. No debe publicarse en el directorio, perfil publico, mapa, analytics,
URLs ni logs.

Debe definirse:

- cuales campos son obligatorios;
- cuanto tiempo se conservan;
- quien puede consultarlos;
- cuando se eliminan o anonimizan;
- que ocurre si el cliente retira su consentimiento antes de completar el pedido.

### 4.3 Incumplimientos

La tienda debe atender directamente reclamos por:

- entrega tardia o fallida;
- producto equivocado o incompleto;
- producto dañado;
- falta de disponibilidad despues de aceptar;
- problemas de retiro;
- devolucion, garantia o sustitucion.

PawTrack puede registrar la incidencia y facilitar la comunicacion, pero no
promete resolverla ni asumir la obligacion comercial de la tienda.

---

## 5. Productos y contenido comercial

La tienda garantiza que tiene derecho a publicar textos, fotografias, marcas,
precios y demas material del catalogo.

La tienda no debe publicar:

- productos prohibidos o de origen ilicito;
- medicamentos o productos regulados sin las autorizaciones correspondientes;
- productos vencidos, falsificados o inseguros;
- afirmaciones medicas o sanitarias engañosas;
- contenido que infrinja derechos de terceros;
- datos personales de clientes u otras personas.

PawTrack debe reservarse el derecho de ocultar, suspender o retirar contenido
que sea ilegal, peligroso, fraudulento, engañoso o contrario a sus politicas.
La tienda debe recibir un canal para corregir o apelar una decision, salvo
casos urgentes de seguridad o cumplimiento.

---

## 6. Pagos y ausencia de intermediacion

En esta fase:

- PawTrack no solicita ni almacena credenciales bancarias.
- PawTrack no procesa transferencias ni tarjetas.
- PawTrack no verifica que una transferencia haya ocurrido.
- PawTrack no confirma pagos en nombre de la tienda.
- PawTrack no administra saldos, comisiones, liquidaciones ni payouts.
- PawTrack no promete reembolsos.
- Cualquier disputa de pago se resuelve entre cliente y tienda.

Los textos de la aplicacion deben evitar frases como "PawTrack recibio tu pago",
"pago garantizado" o "reembolso de PawTrack". Se recomienda utilizar:

> "PawTrack envio tu solicitud a la tienda. Cualquier pago, confirmacion,
> entrega o reembolso se coordina directamente con la tienda."

Si el codigo conserva una referencia o paso de reporte de pago por compatibilidad,
debe identificarse como comunicacion manual y no como prueba de pago.

---

## 7. Datos personales y privacidad

La funcionalidad de tiendas debe complementar la Política de Privacidad de
PawTrack CR, sin contradecirla.

### 7.1 Datos tratados

Podrian tratarse:

- nombre y correo del cliente;
- telefono, si se requiere para coordinar;
- direccion o instrucciones de entrega;
- detalle del pedido y notas;
- identificador de tienda, fechas, estados y trazas tecnicas;
- datos de contacto y comerciales de la tienda;
- registros de soporte, seguridad y auditoria.

No se deben recopilar mas datos de los necesarios para cumplir el pedido y
atender soporte.

### 7.2 Comparticion

El cliente debe saber que los datos necesarios para completar el pedido se
comparten con la tienda. La tienda solo puede usar esos datos para:

- preparar y entregar el pedido;
- comunicarse sobre ese pedido;
- atender reclamos, garantia o devolucion;
- cumplir una obligacion legal.

La tienda no debe usar la informacion del cliente para publicidad no autorizada,
crear bases de datos externas o compartirla con terceros sin una base legal y
la informacion correspondiente.

### 7.3 Seguridad y retencion

- [ ] Definir periodo de conservacion de pedidos y direcciones.
- [ ] Definir proceso de acceso, rectificacion, exportacion y eliminacion.
- [ ] Auditar accesos administrativos a PII.
- [ ] Enmascarar PII en logs y analytics.
- [ ] Aplicar control de acceso por propiedad de recurso.
- [ ] Documentar incidentes y notificacion de brechas cuando corresponda.

---

## 8. Cuenta de tienda y suspension

Una cuenta administra una sola tienda en esta fase. La tienda debe proteger sus
credenciales y es responsable de las acciones realizadas desde su cuenta.

PawTrack puede suspender temporalmente una cuenta o catalogo cuando exista:

- fraude o intento de fraude;
- contenido ilegal o peligroso;
- riesgo para clientes o mascotas;
- incumplimiento de los terminos;
- datos falsos o suplantacion;
- abuso de la plataforma;
- requerimiento de autoridad competente.

Debe existir un registro de la razon, fecha, actor y alcance de la suspension.
Cuando no exista riesgo urgente, se debe ofrecer un mecanismo de revision o
apelacion.

---

## 9. Limitacion de responsabilidad para revision

La redaccion final debe ser revisada por asesoria legal. Como punto de partida,
los terminos deberian distinguir entre:

- fallas tecnicas atribuibles a PawTrack;
- decisiones comerciales de la tienda;
- calidad, legalidad y seguridad del producto;
- entrega, retiro y transporte;
- exactitud de precios y disponibilidad;
- pagos y reembolsos entre las partes;
- conducta del cliente o de la tienda;
- eventos fuera del control razonable de cada parte.

No se debe usar una limitacion de responsabilidad para excluir obligaciones que
la ley no permita excluir ni para ocultar derechos del consumidor.

---

## 10. Checklist de aprobacion legal y operativa

### Producto

- [ ] Se aprobaron los textos de pedido no garantizado.
- [ ] Se aprobaron los estados y la regla de que solo la tienda los cambia.
- [ ] Se aprobaron las reglas de entrega, retiro y cancelacion.
- [ ] La interfaz no presenta a PawTrack como vendedor o procesador de pagos.
- [ ] Se confirmo el alcance de una tienda y una sede por cuenta.

### Operaciones

- [ ] Cada tienda publica sus zonas, horarios, tarifas y condiciones.
- [ ] Existe procedimiento para rechazos, cancelaciones y reclamos.
- [ ] Existe canal de soporte y escalamiento.
- [ ] Se definio como se documenta un apartado manual de producto.
- [ ] Se definio que ocurre ante falta de disponibilidad.

### Privacidad y seguridad

- [ ] Se aprobo el uso de direccion, telefono y notas del pedido.
- [ ] Se definio quien puede ver datos de entrega.
- [ ] Se probaron accesos cruzados entre clientes y tiendas.
- [ ] Se definio retencion y eliminacion de datos.
- [ ] Se revisaron logs, analytics y exportaciones para evitar PII.

### Legal

- [ ] Asesoria legal reviso terminos y politica de privacidad.
- [ ] Se confirmaron obligaciones de consumidor y comercio aplicables.
- [ ] Se definieron responsabilidades fiscales de la tienda.
- [ ] Se revisaron productos regulados y publicidad comercial.
- [ ] Producto, operaciones y soporte aprobaron la version de lanzamiento.

---

## 11. Decisiones pendientes despues de este borrador

- [ ] Tiers, precios y limites comerciales.
- [ ] Reglas exactas de cancelacion y no-show.
- [ ] Tiempo maximo para que la tienda responda una solicitud.
- [ ] Duracion de un apartado manual, si la tienda lo ofrece.
- [ ] Datos obligatorios de tienda y cliente.
- [ ] Retencion de pedidos, direcciones y comunicaciones.
- [ ] Texto final de terminos, privacidad y consentimiento.
- [ ] Procedimiento formal de quejas y escalamiento.

---

## Revision y aprobacion

| Area           | Responsable | Estado    | Fecha | Observaciones |
| -------------- | ----------- | --------- | ----- | ------------- |
| Producto       |             | Pendiente |       |               |
| Operaciones    |             | Pendiente |       |               |
| Soporte        |             | Pendiente |       |               |
| Privacidad     |             | Pendiente |       |               |
| Asesoria legal |             | Pendiente |       |               |
| Direccion      |             | Pendiente |       |               |
