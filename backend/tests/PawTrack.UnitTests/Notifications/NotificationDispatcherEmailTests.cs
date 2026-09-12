using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Auth;
using PawTrack.Infrastructure.Notifications;

namespace PawTrack.UnitTests.Notifications;

public sealed class NotificationDispatcherEmailTests
{
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IPushNotificationService _pushService = Substitute.For<IPushNotificationService>();
    private readonly IUserLocationRepository _userLocationRepo = Substitute.For<IUserLocationRepository>();
    private readonly IAllyProfileRepository _allyProfileRepo = Substitute.For<IAllyProfileRepository>();
    private readonly INotificationRateLimitService _rateLimitService = Substitute.For<INotificationRateLimitService>();
    private readonly IGeofencedAlertLogRepository _alertLogRepo = Substitute.For<IGeofencedAlertLogRepository>();
    private readonly IFamilyRepository _familyRepo = Substitute.For<IFamilyRepository>();
    private readonly ISubscriptionService _subService = Substitute.For<ISubscriptionService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<NotificationDispatcher> _logger = Substitute.For<ILogger<NotificationDispatcher>>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    private NotificationDispatcher CreateSut() => new(
        _notificationRepository,
        _emailSender,
        _pushService,
        _userLocationRepo,
        _allyProfileRepo,
        _rateLimitService,
        _alertLogRepo,
        _familyRepo,
        _subService,
        _unitOfWork,
        _logger,
        _userRepository);

    [Fact]
    public async Task DispatchCustodyStartedAsync_SendsEmailsToBothFosterAndOwner()
    {
        var sut = CreateSut();
        var custodyId = Guid.NewGuid();
        var fosterUserId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();

        await sut.DispatchCustodyStartedAsync(
            custodyId,
            fosterUserId, "foster@test.cr", "Foster Name",
            ownerUserId, "owner@test.cr", "Owner Name",
            "Nala", 5, CancellationToken.None);

        await _emailSender.Received(1).SendCustodyStartedAsync(
            "foster@test.cr", "Foster Name", "Nala", "Owner Name", 5, Arg.Any<CancellationToken>());
        await _emailSender.Received(1).SendCustodyStartedAsync(
            "owner@test.cr", "Owner Name", "Nala", "Foster Name", 5, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchCustodyClosedAsync_SendsEmailsToBothFosterAndOwner()
    {
        var sut = CreateSut();
        var custodyId = Guid.NewGuid();
        var fosterUserId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();

        await sut.DispatchCustodyClosedAsync(
            custodyId,
            fosterUserId, "foster@test.cr", "Foster Name",
            ownerUserId, "owner@test.cr", "Owner Name",
            "Nala", "Mascota devuelta a su dueño", CancellationToken.None);

        await _emailSender.Received(1).SendCustodyClosedAsync(
            "foster@test.cr", "Foster Name", "Nala", "Owner Name", "Mascota devuelta a su dueño", Arg.Any<CancellationToken>());
        await _emailSender.Received(1).SendCustodyClosedAsync(
            "owner@test.cr", "Owner Name", "Nala", "Foster Name", "Mascota devuelta a su dueño", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAdoptionInterestAsync_WhenShelterFound_SendsEmail()
    {
        var sut = CreateSut();
        var shelterId = Guid.NewGuid();
        var (shelterUser, _) = User.Create("shelter@test.cr", "hash", "Refugio Esperanza");
        _userRepository.GetByIdAsync(shelterId, Arg.Any<CancellationToken>()).Returns(shelterUser);

        var appId = Guid.NewGuid();
        await sut.DispatchAdoptionInterestAsync(shelterId, "Max", appId, CancellationToken.None);

        await _emailSender.Received(1).SendAdoptionInterestAsync(
            "shelter@test.cr", "Refugio Esperanza", "Max", "Un interesado", appId.ToString(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAdoptionApprovedAsync_WhenApplicantFound_SendsEmail()
    {
        var sut = CreateSut();
        var applicantId = Guid.NewGuid();
        var (applicantUser, _) = User.Create("applicant@test.cr", "hash", "Carlos");
        _userRepository.GetByIdAsync(applicantId, Arg.Any<CancellationToken>()).Returns(applicantUser);

        var appId = Guid.NewGuid();
        await sut.DispatchAdoptionApprovedAsync(applicantId, "Max", appId, CancellationToken.None);

        await _emailSender.Received(1).SendAdoptionApprovedAsync(
            "applicant@test.cr", "Carlos", "Max", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAdoptionRejectedAsync_WhenApplicantFound_SendsEmail()
    {
        var sut = CreateSut();
        var applicantId = Guid.NewGuid();
        var (applicantUser, _) = User.Create("applicant@test.cr", "hash", "Carlos");
        _userRepository.GetByIdAsync(applicantId, Arg.Any<CancellationToken>()).Returns(applicantUser);

        var appId = Guid.NewGuid();
        await sut.DispatchAdoptionRejectedAsync(applicantId, "Max", appId, CancellationToken.None);

        await _emailSender.Received(1).SendAdoptionRejectedAsync(
            "applicant@test.cr", "Carlos", "Max", Arg.Any<CancellationToken>());
    }
}
