# Recordatorios y alertas

La [API](../../backend/src/PawTrack.API/Controllers/MedicalController.cs) permite consultar, crear, completar y eliminar recordatorios; [VetReminderHostedService y HealthAlertHostedService](../../backend/src/PawTrack.Infrastructure/InfrastructureServiceCollectionExtensions.cs) están registrados. La persistencia de [VetReminders](../../backend/src/PawTrack.Infrastructure/Persistence/PawTrackDbContext.cs) acredita modelo, pero la entrega de notificaciones y calendario en producción es `NO_VERIFICADO`. No afirmar que un recordatorio sustituye consulta veterinaria.

Pruebas médicas existentes: [MedicalCommandHandlerTests](../../backend/tests/PawTrack.UnitTests/Medical/Handlers/MedicalCommandHandlerTests.cs) y [MedicalEndpointsTests](../../backend/tests/PawTrack.IntegrationTests/Medical/MedicalEndpointsTests.cs); no se ejecutaron en esta revisión.
