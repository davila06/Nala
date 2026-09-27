# Gobierno de datos institucionales

Las [rutas municipales](../../backend/src/PawTrack.API/Controllers/MunicipalController.cs) exigen roles y admiten capturas/transferencias; las [rutas de reportes](../../backend/src/PawTrack.API/Controllers/InstitutionalReportsController.cs) combinan roles, scope y flag. Deben comprobarse aislamiento entre municipios, agregación, minimización y acceso a fotos en pruebas dedicadas; no se ejecutaron aquí. [DbContext](../../backend/src/PawTrack.Infrastructure/Persistence/PawTrackDbContext.cs) incluye capturas y exportaciones; el modelo no acredita retención efectiva.

Un envío a regulador no está habilitado por el [NoOpRegulatorySubmissionGateway](../../backend/src/PawTrack.Application/Regulatory/Gateways/NoOpRegulatorySubmissionGateway.cs). No presentar exportaciones descargables como cumplimiento institucional o interoperabilidad aprobada.
