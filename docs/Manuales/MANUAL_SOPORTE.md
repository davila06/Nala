# Manual de Soporte - PawTrack CR

**Version:** 1.0  
**Rol:** `Support`  
**Audiencia:** personal de soporte operativo autorizado  
**Ultima actualizacion:** 2026-09-10

## 1. Alcance del rol

`Support` es un rol privilegiado asignado por un `Admin`; no existe registro
publico ni una pantalla `/support`. El acceso se concede a una cuenta existente
mediante la operacion administrativa correspondiente y debe retirarse cuando
termine la necesidad operativa.

El rol puede operar la cola de casos de bienestar animal y los incidentes de
proveedores. No sustituye al administrador para aprobaciones globales, planes,
usuarios, clinicas, tiendas o configuracion de seguridad.

## 2. Casos de bienestar

La cola administrativa esta disponible en la pestana **Bienestar** de `/admin`.
El flujo recomendado es:

1. Filtrar por estado, severidad y canton.
2. Abrir el detalle y revisar solo la evidencia necesaria.
3. Iniciar triage.
4. Asignar severidad con criterios consistentes.
5. Asignar la organizacion responsable o derivar el caso con motivo.
6. Agregar notas factuales, sin especulaciones ni PII innecesaria.
7. Resolver, descartar o cerrar sin accion indicando siempre la razon.

La evidencia sensible se descarga solo cuando existe una necesidad operacional.
No se debe reenviar fuera de PawTrack ni guardar copias locales.

## 3. Incidentes de proveedores

En la cola de incidentes de proveedores, revisa el tipo, descripcion y
referencia de evidencia. Confirma el alcance antes de resolver. Toda resolucion
debe incluir una explicacion breve, accion tomada y, si aplica, una derivacion
al administrador.

El rol no puede modificar el perfil comercial, membresia o verificacion del
proveedor salvo que un flujo administrativo expresamente lo permita.

## 4. Seguridad y escalamiento

- Nunca uses una cuenta de soporte para probar ownership de otro usuario.
- No solicites contraseñas, tokens, API keys ni codigos de handover.
- En accesos indebidos, fraude, riesgo fisico o exposicion de datos, detén la
  operacion y escala a un `Admin`.
- Registra ID de caso, accion, resultado y hora en la bitacora operativa.
- Sigue [RUNBOOK_OPERACIONES.md](../RUNBOOK_OPERACIONES.md) y
  [API_AUTHORIZATION_MATRIX.md](../API_AUTHORIZATION_MATRIX.md).

## 5. Limites conocidos

La autorizacion de Support esta implementada especificamente para bienestar e
incidentes de proveedores. Una respuesta `403` en otra superficie es esperada;
no debe resolverse compartiendo credenciales de Admin.
