using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PawTrack.Application.Adoptions;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Adoptions;
using PawTrack.Domain.Auth;

namespace PawTrack.UnitTests.Adoptions;

public sealed class CreateAdoptionFairAuthorizationTests
{
    private readonly IAdoptionRepository _adoptions = Substitute.For<IAdoptionRepository>();
    private readonly IAllyProfileRepository _allies = Substitute.For<IAllyProfileRepository>();
    private readonly ISubscriptionService _subscriptions = Substitute.For<ISubscriptionService>();
    private readonly INotificationDispatcher _notifications = Substitute.For<INotificationDispatcher>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<CreateAdoptionFairCommandHandler> _logger = Substitute.For<ILogger<CreateAdoptionFairCommandHandler>>();

    private CreateAdoptionFairCommandHandler CreateSut() =>
        new(_adoptions, _allies, _subscriptions, _notifications, _users, _unitOfWork, _logger);

    private static CreateAdoptionFairCommand Command(Guid actorId) => new(
        actorId, "Feria responsable", "Parque Central", 9.93, -84.08,
        DateTimeOffset.UtcNow.AddDays(10), DateTimeOffset.UtcNow.AddDays(10).AddHours(6),
        "Adopcion responsable", []);

    [Fact]
    public async Task Handle_WhenActorIsAdmin_CreatesFairWithoutShelterSubscription()
    {
        var (admin, _) = User.Create("admin@pawtrack.cr", "hash", "Admin");
        admin.PromoteToAdmin();
        _users.GetByIdAsync(admin.Id, Arg.Any<CancellationToken>()).Returns(admin);

        var result = await CreateSut().Handle(Command(admin.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _adoptions.Received(1).AddFairAsync(
            Arg.Is<AdoptionFair>(x => x.OrganizationUserId == admin.Id), Arg.Any<CancellationToken>());
        await _subscriptions.DidNotReceive().GetActiveUserTierAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenActorIsOwner_ReturnsNotVerifiedShelter()
    {
        var (owner, _) = User.Create("owner@pawtrack.cr", "hash", "Owner");
        _users.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(owner);

        var result = await CreateSut().Handle(Command(owner.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("not_verified_shelter");
        await _adoptions.DidNotReceive().AddFairAsync(Arg.Any<AdoptionFair>(), Arg.Any<CancellationToken>());
    }
}
