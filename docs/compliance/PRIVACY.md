# Privacidad: inventario de evidencia

**Aviso:** documento de gobernanza técnica, no asesoría ni certificación legal. La [política publicada/propuesta](../POLITICA_DE_PRIVACIDAD.md) contiene campos sin completar y requiere revisión jurídica local; la [auditoría de protección de datos](../CUMPLIMIENTO_PROTECCION_DATOS.md) registra pendientes organizacionales.

| Categoría                 | Ejemplos observados/documentados                               | Riesgo y control a validar                                                                                    |
| ------------------------- | -------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------- |
| Identidad/contacto        | Cuenta, correo/teléfono, roles, login y relay de contacto      | Minimización, consentimiento/propósito, acceso por ownership, retención y redacción en logs.                  |
| Ubicación GPS             | Collar, avistamientos, casos y zonas                           | Precisión, exposición pública, historial, retención y acceso según propósito.                                 |
| Salud veterinaria         | Vacunas, medicación, citas, documentos y grants clínicos       | Consentimiento diferenciado, permisos de lectura/escritura/exportación, auditoría y ventanas de conservación. |
| Imágenes/evidencia        | Fotos de mascota, sightings, bienestar y documentos            | Metadatos EXIF, contenedor público/privado, derechos, acceso y borrado.                                       |
| Institucional/comunitario | Capturas, incidentes, denuncias, campañas y datos de partner   | Tenant/organización, finalidad, acuerdo, acceso, divulgación y transferencias.                                |
| Pago/actividad            | Referencias SINPE, pedidos, suscripciones, auditoría y eventos | No inferir datos de tarjeta ni estado cobrado sin evidencia; limitar exposición.                              |

## Pendientes a confirmar

La documentación existente menciona registro de bases de datos ante PRODHAB y DPA/transferencia internacional Azure como pendientes de confirmación. La política tiene placeholders de responsable/contactos y aprobación. Ver [LEGAL_REVIEW_REGISTER](../LEGAL_REVIEW_REGISTER.md), [mapa de datos](../security/PRIVACY_DATA_MAP.md) y [DATA GOVERNANCE](DATA_GOVERNANCE.md). No afirmar cumplimiento Ley 8968 ni GDPR.
