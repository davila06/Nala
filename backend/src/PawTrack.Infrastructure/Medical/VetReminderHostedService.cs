using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Medical;

/// <summary>
/// Runs at 08:00 CR time daily and sends push reminders for vet appointments due within 3 days.
/// </summary>
public sealed class VetReminderNotificationJob(IMedicalRepository medicalRepository,
    INotificationDispatcher notificationDispatcher,
    IPetRepository petRepository,
    ILogger<VetReminderNotificationJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reminders = await medicalRepository.GetRemindersDueSoonAsync(today, daysAhead: 3, ct);

        logger.LogInformation("VetReminderJob: checking {Count} upcoming reminders for {Date}", reminders.Count, today);

        foreach (var reminder in reminders)
        {
            try
            {
                var pet = await petRepository.GetByIdAsync(reminder.PetId, ct);
                if (pet is null) continue;

                var daysUntil = reminder.DueDate.DayNumber - today.DayNumber;
                var message = daysUntil == 0
                    ? $"¡Hoy es el día! {reminder.Title} para {pet.Name}."
                    : $"En {daysUntil} día(s): {reminder.Title} para {pet.Name}.";

                await notificationDispatcher.DispatchVetReminderAsync(
                    pet.OwnerId, pet.Name, reminder.Title, reminder.DueDate, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to dispatch vet reminder {ReminderId}", reminder.Id);
            }
        }
    }
}

public sealed class VetReminderHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<VetReminderHostedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            // Fire at 08:00 CR time (UTC-6) = 14:00 UTC
            var nextRun = now.Date.AddHours(14);
            if (now.Hour >= 14) nextRun = nextRun.AddDays(1);
            var delay = nextRun - now;

            logger.LogInformation("VetReminderHostedService: next run in {Delay}", delay);
            await Task.Delay(delay, stoppingToken);

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var job = scope.ServiceProvider.GetRequiredService<VetReminderNotificationJob>();
                await job.ExecuteAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "VetReminderHostedService failed");
            }
        }
    }
}
