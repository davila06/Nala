using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Allies.Commands.ConfirmAllyAlertAction;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Allies;
using PawTrack.Domain.Notifications;

namespace PawTrack.UnitTests.Allies.Commands;

public sealed class ConfirmAllyAlertActionCommandHandlerTests
{
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();
    private readonly IAllyProfileRepository _allyProfileRepository = Substitute.For<IAllyProfileRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ConfirmAllyAlertActionCommandHandler _sut;

    public ConfirmAllyAlertActionCommandHandlerTests()
    {
        _sut = new ConfirmAllyAlertActionCommandHandler(_notificationRepository, _allyProfileRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_VerifiedAlly_ConfirmsAlertAction()
    {
        var userId = Guid.NewGuid();
        var profile = AllyProfile.Create(userId, "Vet Escazu", AllyType.VeterinaryClinic, "Escazu", 9.9187, -84.1394, 2000);
        profile.Approve();

        var notification = Notification.Create(
            userId,
            NotificationType.VerifiedAllyAlert,
            "Nueva alerta",
            "Busca a Nala en tu zona",
            Guid.NewGuid().ToString());

        _allyProfileRepository.GetVerifiedByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);
        _notificationRepository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);

        var result = await _sut.Handle(
            new ConfirmAllyAlertActionCommand(notification.Id, userId, "Ya buscamos en nuestra area"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.ActionSummary.Should().Be("Ya buscamos en nuestra area");
        notification.ActionConfirmedAt.Should().NotBeNull();
        _notificationRepository.Received(1).Update(notification);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}