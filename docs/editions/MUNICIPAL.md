# Edición municipal e institucional

La [API municipal](../../backend/src/PawTrack.API/Controllers/MunicipalController.cs) exige roles Municipality/Admin y expone capturas, estados, transferencias y estadísticas. [InstitutionalReportsController](../../backend/src/PawTrack.API/Controllers/InstitutionalReportsController.cs) tiene catálogo, previews y exportaciones bajo rol y feature flag. Estado de los flujos técnicos: `PARCIALMENTE_IMPLEMENTADO` para interoperabilidad institucional porque el [gateway de envío](../../backend/src/PawTrack.Application/Regulatory/Gateways/NoOpRegulatorySubmissionGateway.cs) no transmite externamente. No afirmar convenio, aprobación SENASA ni despliegue municipal sin evidencia formal.

Ver [capacidades](MUNICIPAL_CAPABILITIES.md), [piloto](MUNICIPAL_PILOT.md) y [gobierno de datos](MUNICIPAL_DATA_GOVERNANCE.md).
