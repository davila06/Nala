# Gobierno de datos

**Estado:** baseline documental propuesto, no inventario de controles certificado.

## Principios de registro

Toda fuente/dataset debe tener owner, propósito, base/consentimiento registrado, clasificación, personas/mascotas afectadas, procedencia, campos, calidad, retención, región, subprocesadores, permisos, usos secundarios, borrado y evidencias de cambio.

## Límites actuales observables

- El [modelo EF](../DATA_MODEL.md) agrupa entidades de múltiples dominios bajo un contexto compartido; se necesita catálogo campo-a-campo para clasificar PII/sensibles.
- [Matriz de retención](../MATRIZ_RETENCION_DATOS.md), [mapa de datos](../security/PRIVACY_DATA_MAP.md) y auditoría contienen controles declarados, pero ejecución/borrado/restore no están verificados integralmente en el corte.
- Los datos de salud/GPS/fotos no deben alimentar entrenamiento o proveedor AI por inferencia. Cada uso requiere propósito/consentimiento y gate documentado.
- Integración/configuración de proveedor no prueba región, no entrenamiento, retención o borrado; recopilar contrato/evidencia del proveedor.

## Flujo para un caso AI

1. Inventariar fuentes aprobadas y dato mínimo necesario.
2. Identificar base, consentimiento, sujetos, retención, región y acceso.
3. Redactar y probar salida/logs; limitar a herramientas autorizadas.
4. Aprobar por privacidad/seguridad/producto y profesional clínico si afecta salud.
5. Evaluar calidad, sesgo, fuga y reversibilidad antes de piloto; conservar evidencia de aprobación.

Referencias: [PRIVACY](PRIVACY.md), [gobernanza AI](../ai/AI_GOVERNANCE.md), [política de retención](../MATRIZ_RETENCION_DATOS.md). Estos pasos no afirman cumplimiento legal.
