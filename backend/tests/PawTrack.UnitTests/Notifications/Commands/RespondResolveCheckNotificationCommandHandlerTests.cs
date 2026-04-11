using FluentAssertions;
using MediatR;
using NSubstitute;
using PawTrack.Application.LostPets.Commands.UpdateLostPetStatus;
using PawTrack.Application.Notifications.Commands.RespondResolveCheckNotification;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.LostPets;
using PawTrack.Domain.Notifications;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace PawTrack.UnitTests.Notifications.Commands;

public sealed class RespondResolveCheckNotificationCommandHandlerTests
{
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static TelemetryClient NullTelemetry() =>
        new(new TelemetryConfiguration { TelemetryChannel = new NullChannel() });


    [Fact]
    public async Task Handle_WhenUserConfirmsFound_ResolvesLostEventAndConfirmsNotification()
    {
        var userId = Guid.NewGuid();
        var notification = Notification.Create(
            userId,
            NotificationType.ResolveCheck,
            "Resolve check",
            "Body",
            Guid.NewGuid().ToString());

        _notificationRepository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>())
            .Returns(notification);

        _sender.Send(
                Arg.Any<UpdateLostPetStatusCommand>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        var sut = new RespondResolveCheckNotificationCommandHandler(
            _notificationRepository,
            _sender,
            _unitOfWork,
            NullTelemetry());

        var result = await sut.Handle(
            new RespondResolveCheckNotificationCommand(notification.Id, userId, true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _sender.Received(1).Send(
            Arg.Is<UpdateLostPetStatusCommand>(c => c.NewStatus == LostPetStatus.Reunited),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserSaysStillLost_OnlyConfirmsNotification()
    {
        var userId = Guid.NewGuid();
        var notification = Notification.Create(
            userId,
            NotificationType.ResolveCheck,
            "Resolve check",
            "Body",
            Guid.NewGuid().ToString());

        _notificationRepository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>())
            .Returns(notification);

        var sut = new RespondResolveCheckNotificationCommandHandler(
            _notificationRepository,
            _sender,
            _unitOfWork,
            NullTelemetry());

        var result = await sut.Handle(
            new RespondResolveCheckNotificationCommand(notification.Id, userId, false),

            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _sender.DidNotReceive().Send(Arg.Any<UpdateLostPetStatusCommand>(), Arg.Any<CancellationToken>());
        notification.ActionConfirmedAt.Should().NotBeNull();

    }
}

file sealed class NullChannel : Microsoft.ApplicationInsights.Channel.ITelemetryChannel
{
    public bool? DeveloperMode { get; set; }
    public string? EndpointAddress { get; set; }
    public void Send(Microsoft.ApplicationInsights.Channel.ITelemetry item) { }
    public void Flush() { }
    public void Dispose() { }
}
