using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Medical;

namespace PawTrack.Infrastructure.Medical;

/// <summary>
/// Daily job at 09:00 CR time (UTC-6 = 15:00 UTC). For each pet with medical records,
/// detects overdue protocols and creates VetReminder + sends push if no pending reminder exists.
/// Only creates reminders for Familia plan pets to avoid noise for free users.
/// </summary>
public sealed class HealthAlertJob(
    IMedicalRepository medicalRepository,
    IPetRepository petRepository,
    ISubscriptionService subscriptionService,
    INotificationDispatcher notificationDispatcher,
    IUnitOfWork unitOfWork,
    ILogger<HealthAlertJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var petIds = await medicalRepository.GetPetIdsWithRecordsAsync(ct);
        logger.LogInformation("HealthAlertJob: scanning {Count} pets for protocol gaps", petIds.Count);

        int created = 0;
        int notified = 0;

        foreach (var petId in petIds)
        {
            try
            {
                await ProcessPetAsync(petId, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "HealthAlertJob: error processing pet {PetId}", petId);
            }
        }

        if (created > 0) await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("HealthAlertJob: created {Created} reminders, sent {Notified} notifications", created, notified);

        // Local method — processes one pet
        async Task ProcessPetAsync(Guid id, CancellationToken token)
        {
            var pet = await petRepository.GetByIdAsync(id, token);
            if (pet is null) return;

            var protocols = await medicalRepository.GetHealthProtocolsBySpeciesAsync(pet.Species.ToString(), token);
            if (protocols.Count == 0) return;

            var records = await medicalRepository.GetByPetIdAsync(id, token);

            // Latest record date per type
            var latestByType = records
                .GroupBy(r => r.Type)
                .ToDictionary(g => g.Key, g => g.Max(r => r.Date));

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            foreach (var protocol in protocols)
            {
                if (!latestByType.TryGetValue(protocol.RecordType, out var lastDate)) continue;

                // Only act on overdue or within 14 days
                var days = protocol.DaysUntilDue(lastDate);
                if (days > 14) continue;

                // Skip if already has a pending reminder for this type
                var existing = await medicalRepository.GetLatestReminderByTypeAsync(id, protocol.RecordType, token);
                if (existing is not null) continue;

                // Crear VetReminder solo para dueños con plan Familia
                var isFamilia = await subscriptionService.IsFamiliaAsync(pet.OwnerId, token);
                if (isFamilia)
                {
                    var dueDate = days < 0 ? today.AddDays(7) : protocol.DueDate(lastDate);
                    var reminder = VetReminder.Create(
                        id, pet.OwnerId, protocol.RecordType, dueDate,
                        $"{protocol.ProtocolName} de {pet.Name}",
                        days < 0 ? $"Atrasado {Math.Abs(days)} días" : $"Vence en {days} días");
                    await medicalRepository.AddReminderAsync(reminder, token);
                    created++;
                }

                // Push notification para todos los planes
                try
                {
                    var alertText = days < 0
                        ? $"{pet.Name} tiene atrasado: {protocol.ProtocolName}"
                        : $"{pet.Name}: {protocol.ProtocolName} vence en {days} días";

                    var dummyDue = days < 0 ? today.AddDays(7) : protocol.DueDate(lastDate);
                    await notificationDispatcher.DispatchVetReminderAsync(
                        pet.OwnerId, pet.Name, alertText, dummyDue, token);
                    notified++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "HealthAlertJob: failed to notify owner {OwnerId} for pet {PetId}", pet.OwnerId, id);
                }
            }
        }
    }
}

public sealed class HealthAlertHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<HealthAlertHostedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            // 09:00 CR time = UTC-6 → 15:00 UTC
            var nextRun = now.Date.AddHours(15);
            if (now.Hour >= 15) nextRun = nextRun.AddDays(1);

            logger.LogInformation("HealthAlertHostedService: next run at {NextRun}", nextRun);
            await Task.Delay(nextRun - now, stoppingToken);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<HealthAlertJob>();
                await job.ExecuteAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "HealthAlertHostedService: unhandled error");
            }
        }
    }
}
