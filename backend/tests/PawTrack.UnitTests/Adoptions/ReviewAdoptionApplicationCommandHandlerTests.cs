using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.Application.Adoptions;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Adoptions;
using PawTrack.Domain.Allies;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Adoptions;

public sealed class ReviewAdoptionApplicationCommandHandlerTests
{
    private readonly IAdoptionRepository _adoptions = Substitute.For<IAdoptionRepository>();
    private readonly IAllyProfileRepository _allies = Substitute.For<IAllyProfileRepository>();
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private ReviewAdoptionApplicationCommandHandler BuildHandler() => new(
        _adoptions, _allies, _dispatcher, _uow,
        NullLogger<ReviewAdoptionApplicationCommandHandler>.Instance);

    private static (AdoptablePet animal, AdoptionApplication app, AllyProfile shelter) MakeScenario()
    {
        var shelterId = Guid.NewGuid();
        var shelter = AllyProfile.Create(shelterId, "Refugio", AllyType.Shelter,
            "SJ", 9.93, -84.08, 5000);
        shelter.Approve();

        var animal = AdoptablePet.Create(shelterId, "Max", PetSpecies.Dog,
            PetSize.Medium, AgeCategory.Young, "Juguetón", 9.93, -84.08, "SJ");

        var app = AdoptionApplication.Create(animal.Id, Guid.NewGuid(), "Quiero adoptarlo");

        return (animal, app, shelter);
    }

    [Fact]
    public async Task Handle_NotShelter_ReturnsFailure()
    {
        _allies.GetVerifiedByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((AllyProfile?)null);

        var result = await BuildHandler().Handle(
            new ReviewAdoptionApplicationCommand(Guid.NewGuid(), Guid.NewGuid(), true, null), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ApplicationNotFound_ReturnsFailure()
    {
        var (_, _, shelter) = MakeScenario();
        _allies.GetVerifiedByUserIdAsync(shelter.UserId, Arg.Any<CancellationToken>()).Returns(shelter);
        _adoptions.GetApplicationByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((AdoptionApplication?)null);

        var result = await BuildHandler().Handle(
            new ReviewAdoptionApplicationCommand(shelter.UserId, Guid.NewGuid(), true, null), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WrongOrganization_ReturnsAccessDenied()
    {
        var (animal, app, shelter) = MakeScenario();
        var otherShelterId = Guid.NewGuid();
        var otherShelter = AllyProfile.Create(otherShelterId, "Otro", AllyType.Shelter,
            "SJ", 9.93, -84.08, 5000);
        otherShelter.Approve();

        _allies.GetVerifiedByUserIdAsync(otherShelterId, Arg.Any<CancellationToken>()).Returns(otherShelter);
        _adoptions.GetApplicationByIdAsync(app.Id, Arg.Any<CancellationToken>()).Returns(app);
        _adoptions.GetAnimalByIdAsync(animal.Id, Arg.Any<CancellationToken>()).Returns(animal);

        var result = await BuildHandler().Handle(
            new ReviewAdoptionApplicationCommand(otherShelterId, app.Id, true, null), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("access_denied");
    }

    [Fact]
    public async Task Handle_Approve_SetsApprovedAndMarksAnimalInProcess()
    {
        var (animal, app, shelter) = MakeScenario();
        _allies.GetVerifiedByUserIdAsync(shelter.UserId, Arg.Any<CancellationToken>()).Returns(shelter);
        _adoptions.GetApplicationByIdAsync(app.Id, Arg.Any<CancellationToken>()).Returns(app);
        _adoptions.GetAnimalByIdAsync(animal.Id, Arg.Any<CancellationToken>()).Returns(animal);

        var result = await BuildHandler().Handle(
            new ReviewAdoptionApplicationCommand(shelter.UserId, app.Id, true, "Perfecto"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Approved");
        animal.Status.Should().Be(AdoptionStatus.InProcess);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Reject_SetsRejectedStatus()
    {
        var (animal, app, shelter) = MakeScenario();
        _allies.GetVerifiedByUserIdAsync(shelter.UserId, Arg.Any<CancellationToken>()).Returns(shelter);
        _adoptions.GetApplicationByIdAsync(app.Id, Arg.Any<CancellationToken>()).Returns(app);
        _adoptions.GetAnimalByIdAsync(animal.Id, Arg.Any<CancellationToken>()).Returns(animal);

        var result = await BuildHandler().Handle(
            new ReviewAdoptionApplicationCommand(shelter.UserId, app.Id, false, "No cumple requisitos"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Rejected");
        result.Value.ReviewNote.Should().Be("No cumple requisitos");
        animal.Status.Should().Be(AdoptionStatus.Available); // NOT changed on reject
    }
}
