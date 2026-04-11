using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.LostPets;
using PawTrack.Domain.Notifications;
using PawTrack.Domain.Pets;
using PawTrack.Infrastructure.Notifications.Jobs;
using Microsoft.Extensions.Options;
using PawTrack.Application.Common.Settings;

namespace PawTrack.UnitTests.Notifications.Jobs;

public sealed class StaleReportCheckerJobTests
{
    private readonly ILostPetRepository _lostPetRepository = Substitute.For<ILostPetRepository>();
    private readonly ISightingRepository _sightingRepository = Substitute.For<ISightingRepository>();
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();
    private readonly IPetRepository _petRepository = Substitute.For<IPetRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<StaleReportCheckerJob> _logger = Substitute.For<ILogger<StaleReportCheckerJob>>();

    private static IOptions<ResolveCheckSettings> DefaultSettings() =>
        Options.Create(new ResolveCheckSettings());


    [Fact]
    public async Task ExecuteAsync_WhenStaleReportWithoutRecentReminder_CreatesNotificationAndSendsEmail()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Nala", PetSpecies.Cat, null, null);
        pet.MarkAsLost();

        var lostEvent = LostPetEvent.Create(
            pet.Id,
            ownerId,
            "still missing",
            9.93,
            -84.08,
            DateTimeOffset.UtcNow.AddDays(-40));

        var (owner, _) = User.Create("owner@test.com", "password-hash", "Nala Owner");

        _lostPetRepository.GetActiveReportedBeforeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([lostEvent]);

        _sightingRepository.HasSightingsForLostEventSinceAsync(lostEvent.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _notificationRepository.HasRecentByUserTypeAndEntityAsync(
                ownerId,
                NotificationType.StaleReportReminder,
                lostEvent.Id.ToString(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        _petRepository.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _userRepository.GetByIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(owner);

        var sut = new StaleReportCheckerJob(
            _lostPetRepository,
            _sightingRepository,
            _notificationRepository,
            _petRepository,
            _userRepository,
            _emailSender,
            _unitOfWork,
            DefaultSettings(),
            _logger);

        await sut.ExecuteAsync(CancellationToken.None);

        await _notificationRepository.Received(1).AddAsync(
            Arg.Is<Notification>(n =>
                n.UserId == owner.Id
                && n.Type == NotificationType.StaleReportReminder
                && n.RelatedEntityId == lostEvent.Id.ToString()),
            Arg.Any<CancellationToken>());

        await _emailSender.Received(1).SendStaleReportReminderAsync(
            owner.Email,
            owner.Name,
            pet.Name,
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenRecentReminderAlreadyExists_SkipsNotification()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Rocky", PetSpecies.Dog, null, null);
        pet.MarkAsLost();

        var lostEvent = LostPetEvent.Create(
            pet.Id,
            ownerId,
            "still missing",
            9.93,
            -84.08,
            DateTimeOffset.UtcNow.AddDays(-35));

        _lostPetRepository.GetActiveReportedBeforeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([lostEvent]);

        _sightingRepository.HasSightingsForLostEventSinceAsync(lostEvent.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _notificationRepository.HasRecentByUserTypeAndEntityAsync(
                ownerId,
                NotificationType.StaleReportReminder,
                lostEvent.Id.ToString(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = new StaleReportCheckerJob(
            _lostPetRepository,
            _sightingRepository,
            _notificationRepository,
            _petRepository,
            _userRepository,
            _emailSender,
            _unitOfWork,
            DefaultSettings(),
            _logger);

        await sut.ExecuteAsync(CancellationToken.None);

        await _notificationRepository.DidNotReceive().AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendStaleReportReminderAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
