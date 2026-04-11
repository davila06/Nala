using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Fosters.Commands.UpsertMyFosterProfile;
using PawTrack.Application.Fosters.Queries.GetFosterSuggestions;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Sightings;

namespace PawTrack.UnitTests.Sightings.Fosters;

public sealed class FosterProfileAndSuggestionsTests
{
    private readonly IFosterVolunteerRepository _fosterRepository = Substitute.For<IFosterVolunteerRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IFoundPetRepository _foundPetRepository = Substitute.For<IFoundPetRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task UpsertMyFosterProfile_CreatesProfile_WhenMissing()
    {
        var (user, _) = User.Create("foster@test.com", "hash", "Daniela");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _fosterRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns((Domain.Fosters.FosterVolunteer?)null);

        var sut = new UpsertMyFosterProfileCommandHandler(
            _fosterRepository,
            _userRepository,
            _unitOfWork);

        var result = await sut.Handle(
            new UpsertMyFosterProfileCommand(
                user.Id,
                9.9347,
                -84.0875,
                [PetSpecies.Dog, PetSpecies.Cat],
                "Medium",
                5,
                true,
                DateTimeOffset.UtcNow.AddDays(7)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await _fosterRepository.Received(1)
            .AddAsync(Arg.Is<Domain.Fosters.FosterVolunteer>(f =>
                f.UserId == user.Id
                && f.FullName == "Daniela"
                && f.AcceptedSpecies.Contains(PetSpecies.Dog)
                && f.AcceptedSpecies.Contains(PetSpecies.Cat)
                && f.MaxDays == 5
                && f.IsAvailable), Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFosterSuggestions_ReturnsTop3OrderedByDistanceAndSpeciesMatch()
    {
        var report = FoundPetReport.Create(
            PetSpecies.Dog,
            null,
            "Negro",
            "Medium",
            9.9300,
            -84.0800,
            "Mario",
            "8888-1111",
            null);

        _foundPetRepository.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _fosterRepository.GetNearbyAvailableAsync(9.93, -84.08, PetSpecies.Dog, 3_000, Arg.Any<CancellationToken>())
            .Returns(new List<FosterVolunteerSuggestion>
            {
                new(Guid.NewGuid(), "Carlos", 1200, true, "Large", 3),
                new(Guid.NewGuid(), "Daniela", 400, true, "Medium", 7),
                new(Guid.NewGuid(), "Ana", 900, false, null, 2),
                new(Guid.NewGuid(), "Luis", 600, true, "Small", 1),
            });

        var sut = new GetFosterSuggestionsQueryHandler(_fosterRepository, _foundPetRepository);

        var result = await sut.Handle(
            new GetFosterSuggestionsQuery(report.Id, 3),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value![0].VolunteerName.Should().Be("Daniela");
    }
}
