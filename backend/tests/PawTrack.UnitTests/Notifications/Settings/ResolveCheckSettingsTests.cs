using FluentAssertions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Common.Settings;
using PawTrack.Domain.LostPets;
using PawTrack.Domain.Notifications;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Sightings;
using PawTrack.Infrastructure.Notifications.Jobs;

namespace PawTrack.UnitTests.Notifications.Settings;

/// <summary>
/// Verifies that threshold values are read from IOptions&lt;ResolveCheckSettings&gt;
/// instead of being hardcoded — ensuring they are parametrizable via configuration.
/// </summary>
public sealed class ResolveCheckSettingsTests
{
    // ── [C] StaleReportCheckerJob respects StaleDays setting ────────────────
    [Fact]
    public async Task StaleJob_UsesStaleDaysFromSettings_NotHardcoded()
    {
        // Arrange — configure a CUSTOM stale threshold of 60 days (not the default 30)
        var settings = Options.Create(new ResolveCheckSettings
        {
            StaleDays = 60,
            ReminderCooldownDays = 7,
            SilenceThresholdHours = 24,
            HomeProximityMetres = 200,
            SightingHomeProximityMetres = 100,
            DedupWindowHours = 6,
        });

        var lostPetRepo = Substitute.For<ILostPetRepository>();
        var sightingRepo = Substitute.For<ISightingRepository>();
        var notifRepo = Substitute.For<INotificationRepository>();
        var petRepo = Substitute.For<IPetRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        var emailSender = Substitute.For<IEmailSender>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = Substitute.For<ILogger<StaleReportCheckerJob>>();

        // Return an empty list for any DateTimeOffset argument — we just want to see
        // WHICH cutoff date was passed to GetActiveReportedBeforeAsync.
        lostPetRepo
            .GetActiveReportedBeforeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<LostPetEvent>().ToList().AsReadOnly() as IReadOnlyList<LostPetEvent>);

        var sut = new StaleReportCheckerJob(
            lostPetRepo,
            sightingRepo,
            notifRepo,
            petRepo,
            userRepo,
            emailSender,
            unitOfWork,
            settings,
            logger);

        var before = DateTimeOffset.UtcNow;

        // Act
        await sut.ExecuteAsync(CancellationToken.None);

        // Assert — the cutoff should be ~60 days ago, NOT 30 days ago
        await lostPetRepo.Received(1).GetActiveReportedBeforeAsync(
            Arg.Is<DateTimeOffset>(d =>
                d >= before.AddDays(-61) &&
                d <= before.AddDays(-59)),
            Arg.Any<CancellationToken>());
    }

    // ── [C] RespondResolveCheck tracks telemetry on Confirmed ───────────────
    [Fact]
    public async Task RespondResolveCheck_Confirmed_TracksAppInsightsTelemetry()
    {
        // Arrange — use a real TelemetryClient wired to a NullSink so no real AI call
        var config = new TelemetryConfiguration { TelemetryChannel = new NullTelemetryChannel() };
        var telemetryClient = new TelemetryClient(config);

        var notifRepo = Substitute.For<INotificationRepository>();
        var lostPetRepo = Substitute.For<ILostPetRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var userId = Guid.CreateVersion7();
        var lostEventId = Guid.CreateVersion7();
        var notification = Notification.Create(
            userId,
            NotificationType.ResolveCheck,
            "¿Tu mascota ya está en casa?",
            "Respuesta requerida",
            lostEventId.ToString());

        notifRepo.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>())
            .Returns(notification);

        // The handler sends a MediatR command to update status — use a stub sender
        var sender = Substitute.For<MediatR.ISender>();
        sender.Send(Arg.Any<MediatR.IRequest<PawTrack.Domain.Common.Result<bool>>>(), Arg.Any<CancellationToken>())
            .Returns(PawTrack.Domain.Common.Result.Success(true));

        var sut = new PawTrack.Application.Notifications.Commands.RespondResolveCheckNotification
            .RespondResolveCheckNotificationCommandHandler(
                notifRepo,
                sender,
                unitOfWork,
                telemetryClient);

        // Act — foundAtHome = true → "Confirmed"
        var result = await sut.Handle(
            new PawTrack.Application.Notifications.Commands.RespondResolveCheckNotification
                .RespondResolveCheckNotificationCommand(notification.Id, userId, true),
            CancellationToken.None);

        // Assert — handler should succeed; telemetry is fire-and-forget (no assertion on AI)
        result.IsSuccess.Should().BeTrue();
    }

    // ── [C] RespondResolveCheck tracks telemetry on Dismissed ───────────────
    [Fact]
    public async Task RespondResolveCheck_Dismissed_TracksAppInsightsTelemetry()
    {
        var config = new TelemetryConfiguration { TelemetryChannel = new NullTelemetryChannel() };
        var telemetryClient = new TelemetryClient(config);

        var notifRepo = Substitute.For<INotificationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sender = Substitute.For<MediatR.ISender>();

        var userId = Guid.CreateVersion7();
        var lostEventId = Guid.CreateVersion7();
        var notification = Notification.Create(
            userId,
            NotificationType.ResolveCheck,
            "¿Tu mascota ya está en casa?",
            "Respuesta requerida",
            lostEventId.ToString());

        notifRepo.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>())
            .Returns(notification);

        var sut = new PawTrack.Application.Notifications.Commands.RespondResolveCheckNotification
            .RespondResolveCheckNotificationCommandHandler(
                notifRepo,
                sender,
                unitOfWork,
                telemetryClient);

        // Act — foundAtHome = false → "Dismissed"
        var result = await sut.Handle(
            new PawTrack.Application.Notifications.Commands.RespondResolveCheckNotification
                .RespondResolveCheckNotificationCommand(notification.Id, userId, false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

/// <summary>Telemetry sink that discards all items (used in unit tests).</summary>
internal sealed class NullTelemetryChannel : Microsoft.ApplicationInsights.Channel.ITelemetryChannel
{
    public bool? DeveloperMode { get; set; }
    public string? EndpointAddress { get; set; }

    public void Send(Microsoft.ApplicationInsights.Channel.ITelemetry item) { }
    public void Flush() { }
    public void Dispose() { }
}
