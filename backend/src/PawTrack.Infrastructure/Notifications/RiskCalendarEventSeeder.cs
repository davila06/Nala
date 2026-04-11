using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Notifications;

namespace PawTrack.Infrastructure.Notifications;

/// <summary>
/// Seeds the <see cref="RiskCalendarEvent"/> table with the six fixed Costa Rica
/// annual risk events on first startup. Skips seeding if any records already exist.
/// </summary>
public static class RiskCalendarEventSeeder
{
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        await using var scope = services.CreateAsyncScope();

        var repo       = scope.ServiceProvider.GetRequiredService<IRiskCalendarEventRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        if (await repo.AnyAsync(CancellationToken.None))
        {
            logger.LogDebug("RiskCalendarEventSeeder: table already has data, skipping.");
            return;
        }

        logger.LogInformation("RiskCalendarEventSeeder: seeding 6 Costa Rica risk calendar events.");

        var events = BuildEvents();

        foreach (var evt in events)
        {
            await repo.AddAsync(evt, CancellationToken.None);
        }

        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        logger.LogInformation("RiskCalendarEventSeeder: seeding complete.");
    }

    private static IReadOnlyList<RiskCalendarEvent> BuildEvents() =>
    [
        // 1. Tope Nacional — alert one day before (Dec 26)
        RiskCalendarEvent.Create(
            name:             "Tope Nacional",
            month:            12,
            day:              26,
            daysBeforeAlert:  1,
            messageTemplate:  "Mañana es el Tope Nacional. Asegura el patio y verifica que el QR de tu mascota esté vigente."),

        // 2. Año Nuevo — alert on Dec 31 (one day before Jan 1)
        RiskCalendarEvent.Create(
            name:             "Año Nuevo",
            month:            1,
            day:              1,
            daysBeforeAlert:  1,
            messageTemplate:  "Esta noche son los fuegos de Año Nuevo. 8 de cada 10 fugas ocurren esta noche — mantén a tu mascota en un lugar seguro."),

        // 3. Fiestas de Zapote — same-day alert (Dec 26, spans Dec 26 – Jan 2)
        RiskCalendarEvent.Create(
            name:             "Fiestas de Zapote",
            month:            12,
            day:              26,
            daysBeforeAlert:  0,
            messageTemplate:  "Comienzan las Fiestas de Zapote. Habrá pirotecnia toda la semana — revisa que tu mascota esté segura."),

        // 4. 4 de julio — same-day alert, Escazú only
        RiskCalendarEvent.Create(
            name:             "4 de julio",
            month:            7,
            day:              4,
            daysBeforeAlert:  0,
            messageTemplate:  "Noche de fuegos artificiales en tu zona. Mantén a tu mascota en casa.",
            cantonFilter:     "Escazú"),

        // 5. Inicio temporada lluviosa — alert one day before (May 15)
        RiskCalendarEvent.Create(
            name:             "Inicio temporada lluviosa",
            month:            5,
            day:              15,
            daysBeforeAlert:  1,
            messageTemplate:  "La temporada lluviosa inicia mañana. Las tormentas y truenos pueden asustar a las mascotas y provocar fugas."),

        // 6. San José en el Tope — same-day alert (Dec 27)
        RiskCalendarEvent.Create(
            name:             "San José en el Tope",
            month:            12,
            day:              27,
            daysBeforeAlert:  0,
            messageTemplate:  "El Tope Nacional está en curso en San José. Mantén a tu mascota alejada de avenidas concurridas."),
    ];
}
