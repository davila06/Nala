# Manual de Usuario - PawTrack CR

**Version:** 4.0
**Rol:** `Owner`
**Plataforma:** PWA web
**Ultima actualizacion:** 2026-09-09

## 1. Que puedes hacer

PawTrack ayuda a registrar mascotas, generar su QR, reportar perdidas,
recibir avistamientos y coordinar una reunificacion segura. Tambien incluye
salud, familia, collares GPS, adopciones, tiendas y servicios.

Las rutas publicas no requieren cuenta para consultar perfiles, mapa, directorio
o reportar un avistamiento. Las acciones sobre tus datos requieren sesion y
ownership validado por el servidor.

## 2. Crear y proteger tu cuenta

1. Abre `/register`, completa nombre, correo, contrasena y confirmacion de
   mayoria de edad.
2. Verifica el correo recibido.
3. Inicia sesion en `/login`.
4. Usa `/perfil` para cambiar contrasena, preferencias, ubicacion de alertas y
   suscripcion.

Cinco intentos incorrectos pueden activar un bloqueo temporal. No compartas
contrasena, tokens, codigos de recuperacion ni codigos de entrega.

## 3. Registrar una mascota y su QR

Desde `/dashboard` elige **Registrar mascota**. Completa nombre, especie, raza,
fecha de nacimiento, foto y, si aplica, microchip. Cada mascota obtiene un QR
unico; descargalo desde `/pets/:id` y colocalo en collar o placa.

El perfil publico `/p/:id` muestra solo datos minimos. Nunca expone tu correo,
direccion ni telefono. Los escaneos se registran con hora y ubicacion
aproximada según la configuracion disponible.

## 4. Reportar una perdida

Desde el perfil de la mascota abre **Reportar como perdida** y registra ultimo
punto visto, fecha/hora, foto, descripcion, mensaje publico y contacto relay.
No publiques tu telefono ni direccion en el texto.

La perdida activa `/lost/:id/case`, notificaciones, avistamientos y, según tu
plan, difusion multicanal. La sala permite revisar avistamientos, chat seguro,
recompensa, coordinacion y acciones del caso.

## 5. Avistamientos y mascota encontrada

- En `/p/:id/report-sighting` reporta un avistamiento asociado a un QR.
- En `/encontre-mascota` registra una mascota encontrada con el formulario
  completo.
- En `/encontre` usa el flujo rapido cuando solo tienes datos basicos.
- En `/map/match` puedes probar coincidencia visual cuando el plan lo permite.

El reporte de avistamiento protege la identidad del reportante. No contactes a
un propietario fuera del relay anonimo ni publiques coordenadas exactas de una
persona.

## 6. Busqueda, difusion y reunificacion

En `/lost/:id/busqueda` puedes activar una cuadricula de busqueda, reclamar
zonas y marcarlas como revisadas. La coordinacion se actualiza en tiempo real.

Desde la Case Room, **Difundir** puede usar los canales habilitados de correo,
WhatsApp, Telegram y Facebook. Existe un limite operativo para evitar spam.

Cuando la mascota este contigo, usa el codigo de entrega de cuatro digitos si
la entrega fue coordinada por PawTrack y luego marca el caso como reunificado.
Nunca compartas el codigo antes de verificar a la persona y al animal.

## 7. Chat y seguridad

El chat entre dueño y persona que ayuda es enmascarado. Puedes reportar fraude
desde el hilo. PawTrack no muestra automaticamente telefonos o correos de las
partes. Si hay riesgo fisico, prioriza autoridades y protocolos locales.

## 8. Historial medico y familia

El modulo de salud requiere consentimiento especifico para datos sanitarios.
Puedes registrar consultas, vacunas, peso, medicacion y recordatorios según tu
tier. Un grant permite que una clinica consulte o escriba solo los permisos
concedidos; el historial de accesos queda visible.

El plan `UserFamilia` permite invitar hasta cuatro miembros adicionales y
compartir el cuidado de las mascotas. Revoca invitaciones o accesos que ya no
correspondan.

## 9. Collar GPS

En `/pets/:id`, la pestaña GPS requiere plan Plus o superior. Desde alli puedes:

- activar un tag con serial `PT-XXXX-NNNNNNN`;
- consultar ubicacion e historial;
- configurar zonas seguras y alertas de bateria/conectividad;
- activar o desactivar modo perdido;
- generar una transferencia segura del collar.

Una transferencia no comparte tus credenciales. Entrega el PIN o enlace solo al
nuevo responsable y verifica que el collar quede asociado a la mascota correcta.

## 10. Planes del dueño

| Plan tecnico  |   Mascotas | Caracteristicas principales           |
| ------------- | ---------: | ------------------------------------- |
| `Free`        |          1 | funciones base, limites de QR e IA    |
| `UserPlus`    |          3 | funciones ampliadas, GPS y difusion   |
| `UserFamilia` | ilimitadas | familia, salud completa y exportacion |

Los precios y gates vigentes se mantienen en
[PRICING_AND_PLANS.md](../PRICING_AND_PLANS.md) y
[FEATURES.md](../FEATURES.md). La interfaz puede ocultar una funcion, pero el
backend es la autoridad final del plan activo.

## 11. Directorios y adopciones

Puedes consultar `/clinicas`, `/tiendas`, `/servicios`, `/adopciones` y
`/adopciones/ferias` sin ser propietario de un negocio. Las solicitudes de
adopcion y reservas se gestionan con la cuenta autenticada y no conceden acceso
a datos de otros usuarios.

## 12. Privacidad, exportacion y eliminacion

Desde tu perfil solicita **Descargar mis datos** para obtener un JSON con la
informacion que te corresponde. Incluye tus mascotas, reportes, notificaciones
y tus mensajes propios; no incluye mensajes escritos por otras personas.

Para agregar registros medicos debes otorgar consentimiento de datos de salud.
Los grants de clinica se revocan desde el panel de acceso medico.

La eliminacion de cuenta requiere confirmar tu contrasena y es permanente.
Exporta tus datos antes de confirmar. Las retenciones legales, de seguridad y
de auditoria se rigen por [CUMPLIMIENTO_PROTECCION_DATOS.md](../CUMPLIMIENTO_PROTECCION_DATOS.md)
y la [Politica de privacidad](../POLITICA_DE_PRIVACIDAD.md).

## 13. Soporte y reporte de incidentes

Para un problema tecnico conserva la ruta, hora y mensaje visible, sin enviar
secretos. Para fraude usa el control del chat. Para exposicion de datos, acceso
indebido o riesgo urgente, escala al equipo PawTrack y sigue
[RUNBOOK_OPERACIONES.md](../RUNBOOK_OPERACIONES.md).
