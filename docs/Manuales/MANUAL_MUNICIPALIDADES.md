# Manual de Municipalidades - PawTrack CR

**Version:** 1.0  
**Rol:** `Municipality`  
**Audiencia:** personal municipal autorizado  
**Ultima actualizacion:** 2026-09-09

## 1. Alcance

El portal municipal permite registrar animales capturados, dar seguimiento a su
estado, asociar una mascota PawTrack cuando existe coincidencia y convertir un
caso en reporte de bienestar. El alcance de los datos esta aislado al perfil
municipal y sus cantones autorizados.

El rol se asigna administrativamente. No se obtiene mediante el registro publico
de una cuenta individual.

## 2. Acceso

1. Inicia sesion con la cuenta municipal.
2. Abre `/municipalidad/portal` para el portal operativo o
   `/municipalidad/dashboard` para el tablero.
3. Si la cuenta esta pendiente, vencida o sin perfil municipal, solicita al
   administrador revisar el perfil y la suscripcion.

El backend valida el rol y el tenant municipal; cambiar un `userId`, cantón o
identificador en la URL no concede acceso a otra municipalidad.

## 3. Registrar una captura

En **Registrar captura**, completa cantón, especie y color. Raza, edad estimada,
notas y numero de collar o microchip son opcionales. Guarda el registro tan
pronto como el animal ingrese al proceso municipal.

Los estados disponibles dependen del flujo operativo. Al confirmar una
coincidencia, usa el identificador de mascota que devuelve el sistema; no
copies datos privados de una mascota a otra captura.

## 4. Consultar y actualizar capturas

La bandeja permite filtrar por estado, cantón y pagina. Para una captura propia
puedes actualizar el estado individual. `MuniFull` y `MuniRedRegional` tambien
permiten actualizacion masiva y carga de fotografia JPEG o PNG de hasta 5 MB.

La fotografia y las notas deben contener solo evidencia necesaria para la
operacion. No incluy informacion personal innecesaria en texto libre.

## 5. Estadisticas y red regional

| Tier              | Acceso                                                               |
| ----------------- | -------------------------------------------------------------------- |
| `MuniBasica`      | Portal y capturas de su alcance base                                 |
| `MuniFull`        | Fotos, actualizacion masiva y estadisticas cantonales                |
| `MuniRedRegional` | Todo lo anterior, dashboard regional y transferencias entre cantones |

Las estadisticas se consultan desde el dashboard y no deben interpretarse como
estadisticas nacionales si el alcance seleccionado no lo es.

## 6. Transferir o derivar

Con `MuniRedRegional`, selecciona la captura, indica el cantón destino y agrega
el motivo. Verifica la organización receptora antes de confirmar. Para una
situacion de posible maltrato, usa **Convertir en caso de bienestar** y registra
la severidad y una descripcion factual.

## 7. Privacidad y seguridad

- No compartas capturas, coordenadas o evidencia por canales no autorizados.
- No descargues ni publiques datos de propietarios; el portal no esta diseñado
  para distribuir PII.
- Reporta accesos indebidos al administrador y conserva el identificador del
  caso y la hora del incidente.
- Los reportes institucionales agregados se rigen por
  [RUNBOOK_REPORTES_INSTITUCIONALES.md](../RUNBOOK_REPORTES_INSTITUCIONALES.md).

## 8. Checklist de cierre

- Captura con especie, color, cantón y fecha.
- Coincidencia PawTrack verificada o marcada como no encontrada.
- Estado actualizado.
- Evidencia adjunta solo cuando el tier lo permite y es necesaria.
- Transferencia o caso de bienestar con motivo documentado.
