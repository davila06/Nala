# Reglas de negocio con enforcement identificado

- Reporte de pérdida: debe pertenecer al responsable, no duplicar caso activo y respetar `MaxActiveLostCases`; [ReportLostPetCommandHandler](../../backend/src/PawTrack.Application/LostPets/Commands/ReportLostPet/ReportLostPetCommandHandler.cs).
- Contacto de caso activo: solo cuentas autenticadas, pero **no** solo el propietario; [GetLostPetContactQuery](../../backend/src/PawTrack.Application/LostPets/Queries/GetLostPetContact/GetLostPetContactQuery.cs) y [ruta](../../backend/src/PawTrack.API/Controllers/LostPetsController.cs). Es decisión de privacidad que requiere revisión.
- Avistamiento con archivo: tipos JPEG/PNG/WebP, magic bytes y límite 5 MB en [SightingsController](../../backend/src/PawTrack.API/Controllers/SightingsController.cs).
- Cuotas de plan: [EntitlementService](../../backend/src/PawTrack.Infrastructure/Subscriptions/EntitlementService.cs) resuelve definición, consumo y fallback; cada beneficio de [FEATURES](../FEATURES.md) exige verificación independiente.
- Reportes externos: el [gateway NoOp](../../backend/src/PawTrack.Application/Regulatory/Gateways/NoOpRegulatorySubmissionGateway.cs) no transmite; no ofrecer envío regulatorio.
