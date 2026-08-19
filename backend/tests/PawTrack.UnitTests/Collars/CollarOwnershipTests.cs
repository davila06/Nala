using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Collars.Queries.GetCollarStatus;
using PawTrack.Application.Collars.Queries.GetLocationHistory;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Collars;

public sealed class CollarOwnershipQueryTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();
    private static readonly Guid PetId = Guid.NewGuid();

    private static Pet MakePet(Guid ownerId)
    {
        var pet = Pet.Create(ownerId, "Fido", PetSpecies.Dog, null, null);
        typeof(Pet).GetProperty("Id")!.SetValue(pet, PetId);
        return pet;
    }

    // ── GetCollarStatusQuery ──────────────────────────────────────────────────

    [Fact]
    public async Task GetCollarStatus_OwnerRequest_ReturnsDtoOrNull()
    {
        var petRepo = Substitute.For<IPetRepository>();
        var collarRepo = Substitute.For<ICollarRepository>();
        petRepo.GetByIdAsync(PetId, Arg.Any<CancellationToken>()).Returns(MakePet(OwnerId));
        collarRepo.GetActiveForPetAsync(PetId, Arg.Any<CancellationToken>()).Returns((Collar?)null);

        var handler = new GetCollarStatusQueryHandler(collarRepo, petRepo);
        var result = await handler.Handle(new GetCollarStatusQuery(PetId, OwnerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull(); // no collar registered
    }

    [Fact]
    public async Task GetCollarStatus_NonOwner_ReturnsFailure()
    {
        var petRepo = Substitute.For<IPetRepository>();
        var collarRepo = Substitute.For<ICollarRepository>();
        petRepo.GetByIdAsync(PetId, Arg.Any<CancellationToken>()).Returns(MakePet(OwnerId));

        var handler = new GetCollarStatusQueryHandler(collarRepo, petRepo);
        var result = await handler.Handle(new GetCollarStatusQuery(PetId, OtherUserId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("denied") || e.Contains("Access"));
        await collarRepo.DidNotReceive().GetActiveForPetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCollarStatus_PetNotFound_ReturnsFailure()
    {
        var petRepo = Substitute.For<IPetRepository>();
        var collarRepo = Substitute.For<ICollarRepository>();
        petRepo.GetByIdAsync(PetId, Arg.Any<CancellationToken>()).Returns((Pet?)null);

        var handler = new GetCollarStatusQueryHandler(collarRepo, petRepo);
        var result = await handler.Handle(new GetCollarStatusQuery(PetId, OwnerId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ── GetLocationHistoryQuery ───────────────────────────────────────────────

    [Fact]
    public async Task GetLocationHistory_NonOwner_ReturnsFailure()
    {
        var petRepo = Substitute.For<IPetRepository>();
        var collarRepo = Substitute.For<ICollarRepository>();
        petRepo.GetByIdAsync(PetId, Arg.Any<CancellationToken>()).Returns(MakePet(OwnerId));

        var handler = new GetLocationHistoryQueryHandler(collarRepo, petRepo);
        var result = await handler.Handle(
            new GetLocationHistoryQuery(PetId, OtherUserId, Hours: 24, MaxPoints: 100),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await collarRepo.DidNotReceive().GetActiveForPetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetLocationHistory_OwnerNoPetCollar_ReturnsEmpty()
    {
        var petRepo = Substitute.For<IPetRepository>();
        var collarRepo = Substitute.For<ICollarRepository>();
        petRepo.GetByIdAsync(PetId, Arg.Any<CancellationToken>()).Returns(MakePet(OwnerId));
        collarRepo.GetActiveForPetAsync(PetId, Arg.Any<CancellationToken>()).Returns((Collar?)null);

        var handler = new GetLocationHistoryQueryHandler(collarRepo, petRepo);
        var result = await handler.Handle(
            new GetLocationHistoryQuery(PetId, OwnerId, Hours: 24, MaxPoints: 100),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
