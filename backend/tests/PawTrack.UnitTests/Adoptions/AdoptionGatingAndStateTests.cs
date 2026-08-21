using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.Application.Adoptions;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Adoptions;
using PawTrack.Domain.Allies;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Adoptions;

public sealed class MarkAdoptedCommandHandlerTests
{
    private readonly IAdoptionRepository _adoptions = Substitute.For<IAdoptionRepository>();
    private readonly IAllyProfileRepository _allies = Substitute.For<IAllyProfileRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private MarkAdoptedCommandHandler BuildHandler() => new(_adoptions, _allies, _uow);

    private static (AdoptablePet animal, AllyProfile shelter) MakeScenario()
    {
        var userId = Guid.NewGuid();
        var shelter = AllyProfile.Create(userId, "Refugio", AllyType.Shelter, "SJ", 9.93, -84.08, 5000);
        shelter.Approve();
        var animal = AdoptablePet.Create(userId, "Max", PetSpecies.Dog,
            PetSize.Medium, AgeCategory.Young, "Juguetón", 9.93, -84.08, "SJ");
        animal.MarkInProcess();
        return (animal, shelter);
    }

    [Fact]
    public async Task Handle_NotShelter_ReturnsFailure()
    {
        _allies.GetVerifiedByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((AllyProfile?)null);

        var result = await BuildHandler().Handle(
            new MarkAdoptedCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AnimalNotOwned_ReturnsFailure()
    {
        var (animal, shelter) = MakeScenario();
        var otherId = Guid.NewGuid();
        var otherShelter = AllyProfile.Create(otherId, "Otro", AllyType.Shelter, "SJ", 9.93, -84.08, 5000);
        otherShelter.Approve();
        _allies.GetVerifiedByUserIdAsync(otherId, Arg.Any<CancellationToken>()).Returns(otherShelter);
        _adoptions.GetAnimalByIdAsync(animal.Id, Arg.Any<CancellationToken>()).Returns(animal);

        var result = await BuildHandler().Handle(
            new MarkAdoptedCommand(otherId, animal.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("access_denied");
    }

    [Fact]
    public async Task Handle_Valid_SetsAdoptedAndSaves()
    {
        var (animal, shelter) = MakeScenario();
        _allies.GetVerifiedByUserIdAsync(shelter.UserId, Arg.Any<CancellationToken>()).Returns(shelter);
        _adoptions.GetAnimalByIdAsync(animal.Id, Arg.Any<CancellationToken>()).Returns(animal);

        var result = await BuildHandler().Handle(
            new MarkAdoptedCommand(shelter.UserId, animal.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Adopted");
        animal.Status.Should().Be(AdoptionStatus.Adopted);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public sealed class WithdrawApplicationCommandHandlerTests
{
    private readonly IAdoptionRepository _adoptions = Substitute.For<IAdoptionRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private WithdrawApplicationCommandHandler BuildHandler() => new(_adoptions, _uow);

    [Fact]
    public async Task Handle_ApplicationNotFound_ReturnsFailure()
    {
        _adoptions.GetApplicationByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((AdoptionApplication?)null);

        var result = await BuildHandler().Handle(
            new WithdrawApplicationCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("application_not_found");
    }

    [Fact]
    public async Task Handle_WrongApplicant_ReturnsFailure()
    {
        var app = AdoptionApplication.Create(Guid.NewGuid(), Guid.NewGuid(), "Quiero adoptarlo");
        _adoptions.GetApplicationByIdAsync(app.Id, Arg.Any<CancellationToken>()).Returns(app);

        var result = await BuildHandler().Handle(
            new WithdrawApplicationCommand(Guid.NewGuid(), app.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(WithdrawApplicationCommandHandler.NotOwnApplicationError);
    }

    [Fact]
    public async Task Handle_AlreadyApproved_ReturnsFailure()
    {
        var applicantId = Guid.NewGuid();
        var app = AdoptionApplication.Create(Guid.NewGuid(), applicantId, "Quiero adoptarlo");
        app.Approve("Aprobado");
        _adoptions.GetApplicationByIdAsync(app.Id, Arg.Any<CancellationToken>()).Returns(app);

        var result = await BuildHandler().Handle(
            new WithdrawApplicationCommand(applicantId, app.Id), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidPending_SetsWithdrawnAndSaves()
    {
        var applicantId = Guid.NewGuid();
        var app = AdoptionApplication.Create(Guid.NewGuid(), applicantId, "Quiero adoptarlo");
        _adoptions.GetApplicationByIdAsync(app.Id, Arg.Any<CancellationToken>()).Returns(app);

        var result = await BuildHandler().Handle(
            new WithdrawApplicationCommand(applicantId, app.Id), default);

        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(ApplicationStatus.Withdrawn);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public sealed class PublishGatingTests
{
    private readonly IAllyProfileRepository _allies = Substitute.For<IAllyProfileRepository>();
    private readonly IAdoptionRepository _adoptions = Substitute.For<IAdoptionRepository>();
    private readonly ISubscriptionService _subscriptions = Substitute.For<ISubscriptionService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private PublishAdoptablePetCommandHandler BuildHandler() => new(
        _allies, _adoptions, _subscriptions, _uow,
        NullLogger<PublishAdoptablePetCommandHandler>.Instance);

    private static PublishAdoptablePetCommand Cmd(Guid orgId) => new(
        orgId, "Max", PetSpecies.Dog, PetSize.Medium, AgeCategory.Young,
        "Juguetón", 9.93, -84.08, "SJ", null, null, null, null,
        false, false, false, false, false, false, false);

    private static AllyProfile MakeShelter(Guid userId)
    {
        var p = AllyProfile.Create(userId, "Refugio", AllyType.Shelter, "SJ", 9.93, -84.08, 5000);
        p.Approve();
        return p;
    }

    [Fact]
    public async Task Handle_ShelterBasicUnder5_Allows()
    {
        var userId = Guid.NewGuid();
        _allies.GetVerifiedByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(MakeShelter(userId));
        _subscriptions.GetActiveUserTierAsync(userId, Arg.Any<CancellationToken>()).Returns(SubscriptionTier.Free);
        _adoptions.CountByOrganizationAsync(userId, Arg.Any<CancellationToken>()).Returns(4);

        var result = await BuildHandler().Handle(Cmd(userId), default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShelterBasicAt5_Blocks()
    {
        var userId = Guid.NewGuid();
        _allies.GetVerifiedByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(MakeShelter(userId));
        _subscriptions.GetActiveUserTierAsync(userId, Arg.Any<CancellationToken>()).Returns(SubscriptionTier.Free);
        _adoptions.CountByOrganizationAsync(userId, Arg.Any<CancellationToken>()).Returns(5);

        var result = await BuildHandler().Handle(Cmd(userId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(PublishAdoptablePetCommandHandler.ShelterBasicLimitError);
    }

    [Fact]
    public async Task Handle_ShelterPlusAt10_Allows()
    {
        var userId = Guid.NewGuid();
        _allies.GetVerifiedByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(MakeShelter(userId));
        _subscriptions.GetActiveUserTierAsync(userId, Arg.Any<CancellationToken>()).Returns(SubscriptionTier.ShelterPlus);
        _adoptions.CountByOrganizationAsync(userId, Arg.Any<CancellationToken>()).Returns(10);

        var result = await BuildHandler().Handle(Cmd(userId), default);

        result.IsSuccess.Should().BeTrue();
    }
}
