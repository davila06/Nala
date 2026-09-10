# Manual de Proveedores de Servicios - PawTrack CR

**Version:** 1.0  
**Rol:** `ServiceProvider`  
**Audiencia:** profesionales y negocios de servicios para mascotas  
**Ultima actualizacion:** 2026-09-10

## 1. Registro y estados

Registra tu servicio en `/servicio/registro` con categoria, descripcion,
ubicacion y contacto. Las categorias actuales incluyen entrenador, grooming,
hotel, daycare, paseador, fotografo y otros.

El directorio publico se consulta en `/servicios` y cada perfil en
`/servicios/:id`; ambos pueden mostrar vallas publicitarias activas del
placement correspondiente.

Consulta `/servicio/pendiente` durante la revision. Cuando el perfil esta
activo aparece en `/servicios` y tiene portal en `/servicio/portal`. Un perfil
rechazado o suspendido no debe presentarse como verificado.

## 2. Prueba de membresia

La aprobacion inicial puede activar una prueba unica de 30 dias de membresia
`Verified`. La prueba no se reinicia al reactivar un perfil suspendido. Al
vencer, el perfil vuelve a `Free` salvo una membresia manual administrada por
PawTrack.

`Free` conserva el directorio y contacto basico. Catalogo, disponibilidad y
reservas requieren membresia distinta de `Free`. No existen aun comisiones,
payouts, reembolsos o precios recurrentes aprobados para este rol.

## 3. Configurar el perfil y el catalogo

En `/servicio/portal/perfil` actualiza nombre, categoria, descripcion,
direccion, telefono, sitio web y WhatsApp profesional.

En `/servicio/portal/servicios` crea cada servicio con modalidad, duracion,
precio en colones y capacidad. Publica solo servicios que realmente puedas
prestar. Puedes activar o desactivar servicios sin eliminar el historial.

Define reglas de disponibilidad por dia y hora local de Costa Rica. Usa bloqueos
para vacaciones, mantenimiento o ausencia temporal; elimina solo los bloqueos
que ya no apliquen.

## 4. Reservas

En `/servicio/portal/reservas` revisa las solicitudes entrantes y actualiza su
estado conforme confirmes, inicies y completes el servicio. Revisa fecha,
capacidad y modalidad antes de aceptar. El cliente consulta sus reservas desde
`/mis-reservas`.

La cancelacion o reprogramacion debe llevar una razon clara y respetar las
politicas operativas mostradas por la plataforma. Los pagos de reserva son un
flujo reportado y no deben describirse como liquidacion automatica de PawTrack.

## 5. Verificacion e incidentes

En `/servicio/portal/verificacion` carga evidencia privada en PDF, JPEG, PNG o
WebP de hasta 5 MB. El sello **Verificado por PawTrack CR** no equivale a una
licencia estatal.

Consulta incidentes propios en el portal y aporta evidencia factual. No
incluyas datos de salud ni documentos de clientes salvo que el proceso de
soporte lo solicite expresamente.

## 6. Seguridad

- Administra solo recursos de tu propio proveedor.
- No compartas credenciales ni enlaces privados de verificacion.
- Reporta una reserva sospechosa con su identificador.
- Revisa [SERVICE_PROVIDERS_THREAT_MODEL.md](../SERVICE_PROVIDERS_THREAT_MODEL.md)
  para las reglas de seguridad del dominio.
