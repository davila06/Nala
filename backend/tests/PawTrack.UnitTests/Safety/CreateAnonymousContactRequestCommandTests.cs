using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Safety.Commands.CreateAnonymousContactRequest;
using PawTrack.Domain.Auth;
using PawTrack.Domain.LostPets;

namespace PawTrack.UnitTests.Safety;

public sealed class CreateAnonymousContactRequestCommandTests
{
    [Fact]
    public async Task Handle_ActiveLostEvent_PersistsRequestAndNotifiesOwner()
    {
        var lostRepo = Substitute.For<ILostPetRepository>();
        var users = Substitute.For<IUserRepository>();
        var contacts = Substitute.For<IAnonymousContactRequestRepository>();
        var notifications = Substitute.For<INotificationRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var (owner, _) = User.Create("owner@example.com", "hash", "Owner");
        var ownerId = owner.Id;
        var lost = LostPetEvent.Create(Guid.NewGuid(), ownerId, "lost", 9.9, -84.0, DateTimeOffset.UtcNow);
        lostRepo.GetByIdAsync(lost.Id, Arg.Any<CancellationToken>()).Returns(lost);
        users.GetByIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(owner);

        var handler = new CreateAnonymousContactRequestCommandHandler(
            lostRepo, users, contacts, notifications, uow);

        var result = await handler.Handle(
            new CreateAnonymousContactRequestCommand(lost.Id, "Finder", "I saw your pet near the park."),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await contacts.Received(1).AddAsync(Arg.Any<Domain.Safety.AnonymousContactRequest>(), Arg.Any<CancellationToken>());
        await notifications.Received(1).AddAsync(Arg.Is<Domain.Notifications.Notification>(n => n.UserId == ownerId), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
