# Estado de pilotos institucionales

`NO_VERIFICADO`: no se revisaron convenios firmados, operadores municipales, aceptación institucional ni métricas reales. Los [endpoints municipales](../../backend/src/PawTrack.API/Controllers/MunicipalController.cs) y el [Bicep con flags parametrizados](../../infra/main.bicep) son capacidad técnica/declarativa, no evidencia de piloto contratado o aprobado. Las [exportaciones](../../backend/src/PawTrack.API/Controllers/InstitutionalReportsController.cs) no constituyen recepción oficial; el [gateway NoOp](../../backend/src/PawTrack.Application/Regulatory/Gateways/NoOpRegulatorySubmissionGateway.cs) no envía.

Antes de un piloto: obtener constancias verificables, delimitar canton/tenant, consentimiento y retención, pruebas con usuarios autorizados y soporte operativo. No registrar datos de convenios o personas sin aprobación.
