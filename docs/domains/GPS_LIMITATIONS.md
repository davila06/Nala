# Limitaciones GPS

- [TrackSolidService](../../backend/src/PawTrack.Infrastructure/Collars/TrackSolidService.cs) omite consultas si faltan credenciales y captura errores HTTP con respuesta vacía: no prometer ubicación continua ni SLA sin pruebas de proveedor.
- [Jobs de polling, purga y alerta](../../backend/src/PawTrack.Infrastructure/InfrastructureServiceCollectionExtensions.cs) están registrados; frecuencia efectiva, retención aplicada y entrega de alertas en producción son `NO_VERIFICADO`.
- [CollarsController](../../backend/src/PawTrack.API/Controllers/CollarsController.cs) ofrece ubicación e historial bajo JWT; la protección cruzada por dueño y dispositivo requiere pruebas específicas no ejecutadas en este corte.
- [IOT_ARCHITECTURE](IOT_ARCHITECTURE.md) no acredita firmware propio, certificados, OTA ni Azure IoT Hub; [COLLAR_CURRENT_STATE](../COLLAR_CURRENT_STATE.md) conserva antecedentes.
