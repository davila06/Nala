using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Collars.Commands.RegisterCollar;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Collars;

public sealed class RegisterCollarCommandHandlerTests
{
    [Fact]
    public async Task Handle_CollarQuotaExhausted_ReturnsFailure()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Max", PetSpecies.Dog, null, null);
        var collars = Substitute.For<ICollarRepository>();
        var pets = Substitute.For<IPetRepository>();
        var subscriptions = Substitute.For<ISubscriptionService>();
        var entitlements = Substitute.For<IEntitlementService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        entitlements.AuthorizeAsync(ownerId, "MaxGpsCollars", 1m, Arg.Any<EntitlementContext>(), Arg.Any<CancellationToken>())
            .Returns(new EntitlementDecision(false, true, 1m, 1m, 0m, null, SubscriptionTier.UserPlus));
        subscriptions.IsAtLeastPlusAsync(ownerId, Arg.Any<CancellationToken>()).Returns(true);
        collars.CountActiveByOwnerAsync(ownerId, Arg.Any<CancellationToken>()).Returns(1);

        var handler = new RegisterCollarCommandHandler(collars, pets, subscriptions, unitOfWork, entitlements);
        var result = await handler.Handle(new RegisterCollarCommand(
            pet.Id, ownerId, CollarProvider.Generic, "GPS-1"), default);

        result.IsFailure.Should().BeTrue();
        await collars.DidNotReceive().AddAsync(Arg.Any<Collar>(), Arg.Any<CancellationToken>());
    }
}
