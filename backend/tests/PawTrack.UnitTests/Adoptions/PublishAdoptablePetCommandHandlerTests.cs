using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.Application.Adoptions;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Adoptions;
using PawTrack.Domain.Allies;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Adoptions;

public sealed class PublishAdoptablePetCommandHandlerTests
{
    private readonly IAllyProfileRepository _allies = Substitute.For<IAllyProfileRepository>();
    private readonly IAdoptionRepository _adoptions = Substitute.For<IAdoptionRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private PublishAdoptablePetCommandHandler BuildHandler() => new(
        _allies, _adoptions, _uow, NullLogger<PublishAdoptablePetCommandHandler>.Instance);

    private static PublishAdoptablePetCommand Cmd(Guid orgUserId) => new(
        orgUserId, "Max", PetSpecies.Dog, PetSize.Medium, AgeCategory.Young,
        "Muy juguetón", 9.93, -84.08, "San José",
        null, null, null, null, false, false, false, false, false, false, false);

    private static AllyProfile MakeShelter(Guid userId)
    {
        var p = AllyProfile.Create(userId, "Refugio Esperanza",
            AllyType.Shelter, "San José", 9.93, -84.08, 5000);
        p.Approve();
        return p;
    }

    [Fact]
    public async Task Handle_NullAllyProfile_ReturnsFailure()
    {
        _allies.GetVerifiedByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((AllyProfile?)null);

        var result = await BuildHandler().Handle(Cmd(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(PublishAdoptablePetCommandHandler.NotVerifiedShelterError);
    }

    [Fact]
    public async Task Handle_WrongAllyType_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var profile = AllyProfile.Create(userId, "Clínica", AllyType.VeterinaryClinic,
            "San José", 9.93, -84.08, 5000);
        profile.Approve();
        _allies.GetVerifiedByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);

        var result = await BuildHandler().Handle(Cmd(userId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(PublishAdoptablePetCommandHandler.NotVerifiedShelterError);
    }

    [Fact]
    public async Task Handle_ValidShelter_PublishesAndSaves()
    {
        var userId = Guid.NewGuid();
        var shelter = MakeShelter(userId);
        _allies.GetVerifiedByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(shelter);

        var result = await BuildHandler().Handle(Cmd(userId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OrganizationName.Should().Be("Refugio Esperanza");
        result.Value.Status.Should().Be("Available");
        result.Value.Name.Should().Be("Max");
        await _adoptions.Received(1).AddAnimalAsync(
            Arg.Any<AdoptablePet>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
