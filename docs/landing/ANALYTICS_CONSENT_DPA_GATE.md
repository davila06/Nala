# Gate de analytics externo

**Estado:** BLOQUEADO hasta aprobación de privacidad y proveedor.

## Requisitos

- Proveedor seleccionado y región de procesamiento.
- DPA y subprocesadores revisados.
- Consentimiento previo cuando aplique.
- Retención y borrado definidos.
- Eventos sin PII ni datos clínicos/ubicación.
- Opt-out y documentación de cookies/storage.
- Prueba de no envío antes del consentimiento.
- Owner de seguridad y privacidad.

La implementación actual es un bridge local de eventos DOM; no se debe conectar transporte externo sin cerrar este gate.
