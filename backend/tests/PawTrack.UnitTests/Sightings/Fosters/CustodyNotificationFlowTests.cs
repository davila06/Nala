using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Fosters.Commands.CloseCustody;
using PawTrack.Application.Fosters.Commands.StartCustody;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Fosters;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Sightings;

namespace PawTrack.UnitTests.Sightings.Fosters;

public sealed class CustodyNotificationFlowTests
{
    private readonly IFosterVolunteerRepository _fosterRepository = Substitute.For<IFosterVolunteerRepository>();
    private readonly ICustodyRecordRepository _custodyRepository = Substitute.For<ICustodyRecordRepository>();
    private readonly IFoundPetRepository _foundPetRepository = Substitute.For<IFoundPetRepository>();
    private readonly ILostPetRepository _lostPetRepository = Substitute.For<ILostPetRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPetRepository _petRepository = Substitute.For<IPetRepository>();
    private readonly INotificationDispatcher _notificationDispatcher = Substitute.For<INotificationDispatcher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task StartCustody_WhenFoundReportIsMatched_DispatchesBidirectionalNotifications()
    {
        var (fosterUser, _) = User.Create("foster@test.com", "hash", "Daniela");
        var (ownerUser, _) = User.Create("owner@test.com", "hash", "Mario");
        var pet = Pet.Create(ownerUser.Id, "Nala", PetSpecies.Dog, "Labrador", null);

        var found = FoundPetReport.Create(
            PetSpecies.Dog,
            null,
            "Negra",
            "Medium",
            9.93,
            -84.08,
            "Laura",
            "8888-1111",
            null);

        var lostEvent = Domain.LostPets.LostPetEvent.Create(
            pet.Id,
            ownerUser.Id,
            "Se escapó",
            9.93,
            -84.08,
            DateTimeOffset.UtcNow.AddHours(-4));

        found.Match(lostEvent.Id, 90);

        var fosterProfile = FosterVolunteer.Create(
            fosterUser.Id,
            fosterUser.Name,
            9.94,
            -84.09,
            [PetSpecies.Dog],
            "Medium",
            5,
            true,
            null);

        _fosterRepository.GetByUserIdAsync(fosterUser.Id, Arg.Any<CancellationToken>()).Returns(fosterProfile);
        _foundPetRepository.GetByIdAsync(found.Id, Arg.Any<CancellationToken>()).Returns(found);
        _lostPetRepository.GetByIdAsync(lostEvent.Id, Arg.Any<CancellationToken>()).Returns(lostEvent);
        _userRepository.GetByIdAsync(fosterUser.Id, Arg.Any<CancellationToken>()).Returns(fosterUser);
        _userRepository.GetByIdAsync(ownerUser.Id, Arg.Any<CancellationToken>()).Returns(ownerUser);
        _petRepository.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);

        var sut = new StartCustodyCommandHandler(
            _fosterRepository,
            _custodyRepository,
            _foundPetRepository,
            _lostPetRepository,
            _userRepository,
            _petRepository,
            _notificationDispatcher,
            _unitOfWork);

        var result = await sut.Handle(
            new StartCustodyCommand(fosterUser.Id, found.Id, 4, "Cuido temporal"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await _notificationDispatcher.Received(1).DispatchCustodyStartedAsync(
            Arg.Any<Guid>(),
            fosterUser.Id,
            fosterUser.Email,
            fosterUser.Name,
            ownerUser.Id,
            ownerUser.Email,
            ownerUser.Name,
            pet.Name,
            4,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CloseCustody_WhenRecordActive_DispatchesBidirectionalNotifications()
    {
        var (fosterUser, _) = User.Create("foster2@test.com", "hash", "Carlos");
        var (ownerUser, _) = User.Create("owner2@test.com", "hash", "Ana");
        var pet = Pet.Create(ownerUser.Id, "Milo", PetSpecies.Cat, null, null);

        var found = FoundPetReport.Create(
            PetSpecies.Cat,
            null,
            null,
            "Small",
            9.95,
            -84.10,
            "Pedro",
            "8777-2222",
            null);

        var lostEvent = Domain.LostPets.LostPetEvent.Create(
            pet.Id,
            ownerUser.Id,
            null,
            9.95,
            -84.10,
            DateTimeOffset.UtcNow.AddHours(-8));

        found.Match(lostEvent.Id, 84);

        var record = CustodyRecord.Start(fosterUser.Id, found.Id, 3, "Temporal");

        var fosterProfile = FosterVolunteer.Create(
            fosterUser.Id,
            fosterUser.Name,
            9.95,
            -84.10,
            [PetSpecies.Cat],
            "Small",
            4,
            false,
            DateTimeOffset.UtcNow.AddDays(1));

        _custodyRepository.GetByIdAsync(record.Id, Arg.Any<CancellationToken>()).Returns(record);
        _fosterRepository.GetByUserIdAsync(fosterUser.Id, Arg.Any<CancellationToken>()).Returns(fosterProfile);
        _foundPetRepository.GetByIdAsync(found.Id, Arg.Any<CancellationToken>()).Returns(found);
        _lostPetRepository.GetByIdAsync(lostEvent.Id, Arg.Any<CancellationToken>()).Returns(lostEvent);
        _userRepository.GetByIdAsync(fosterUser.Id, Arg.Any<CancellationToken>()).Returns(fosterUser);
        _userRepository.GetByIdAsync(ownerUser.Id, Arg.Any<CancellationToken>()).Returns(ownerUser);
        _petRepository.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);

        var sut = new CloseCustodyCommandHandler(
            _custodyRepository,
            _fosterRepository,
            _foundPetRepository,
            _lostPetRepository,
            _userRepository,
            _petRepository,
            _notificationDispatcher,
            _unitOfWork);

        var result = await sut.Handle(
            new CloseCustodyCommand(record.Id, fosterUser.Id, "Entregado al dueño"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await _notificationDispatcher.Received(1).DispatchCustodyClosedAsync(
            record.Id,
            fosterUser.Id,
            fosterUser.Email,
            fosterUser.Name,
            ownerUser.Id,
            ownerUser.Email,
            ownerUser.Name,
            pet.Name,
            "Entregado al dueño",
            Arg.Any<CancellationToken>());
    }
}