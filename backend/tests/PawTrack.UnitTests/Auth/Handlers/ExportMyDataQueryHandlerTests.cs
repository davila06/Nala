using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Auth.Queries.ExportMyData;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Chat;
using PawTrack.Domain.LostPets;
using PawTrack.Domain.Medical;
using PawTrack.Domain.Notifications;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Auth.Handlers;

public sealed class ExportMyDataQueryHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IPetRepository _petRepo = Substitute.For<IPetRepository>();
    private readonly ILostPetRepository _lostPetRepo = Substitute.For<ILostPetRepository>();
    private readonly IMedicalRepository _medicalRepo = Substitute.For<IMedicalRepository>();
    private readonly IChatRepository _chatRepo = Substitute.For<IChatRepository>();
    private readonly INotificationRepository _notificationRepo = Substitute.For<INotificationRepository>();

    private readonly ExportMyDataQueryHandler _sut;

    public ExportMyDataQueryHandlerTests()
    {
        _sut = new ExportMyDataQueryHandler(
            _userRepo, _petRepo, _lostPetRepo, _medicalRepo, _chatRepo, _notificationRepo);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.Handle(new ExportMyDataQuery(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AggregatesAllOwnedData()
    {
        var (user, _) = User.Create("owner@test.com", "hash", "Owner");
        var pet = Pet.Create(user.Id, "Max", PetSpecies.Dog, null, null);
        var lostEvent = LostPetEvent.Create(pet.Id, user.Id, "lost near park", 9.9, -84.1, DateTimeOffset.UtcNow);
        var record = MedicalRecord.Create(
            pet.Id, user.Id, MedicalRecordType.Checkup, new DateOnly(2026, 1, 1), "Checkup", null, null, null);
        var thread = ChatThread.Open(Guid.NewGuid(), user.Id, Guid.NewGuid());
        var ownMessage = ChatMessage.Create(thread.Id, user.Id, "hola, la encontre");
        var otherMessage = ChatMessage.Create(thread.Id, Guid.NewGuid(), "gracias");
        var notification = Notification.Create(user.Id, NotificationType.PetReunited, "Reunido", "Tu mascota fue encontrada");

        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _petRepo.GetByOwnerIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(new List<Pet> { pet });
        _lostPetRepo.GetByOwnerIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(new List<LostPetEvent> { lostEvent });
        _medicalRepo.GetByPetIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(new List<MedicalRecord> { record });
        _chatRepo.GetThreadsByUserAsync(user.Id, Arg.Any<CancellationToken>()).Returns(new List<ChatThread> { thread });
        _chatRepo.GetMessagesByThreadAsync(thread.Id, null, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ChatMessage> { ownMessage, otherMessage });
        _notificationRepo.GetByUserIdAsync(user.Id, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Notification> { notification });

        var result = await _sut.Handle(new ExportMyDataQuery(user.Id), default);

        result.IsSuccess.Should().BeTrue();
        var export = result.Value!;
        export.Pets.Should().ContainSingle(p => p.Id == pet.Id.ToString());
        export.LostPetReports.Should().ContainSingle(e => e.Id == lostEvent.Id.ToString());
        export.MedicalRecords.Should().ContainSingle(r => r.Id == record.Id);
        export.Notifications.Should().ContainSingle(n => n.Id == notification.Id);

        // Only messages authored by the exporting user are included — not the other party's.
        export.ChatMessages.Should().ContainSingle(m => m.Body == ownMessage.Body);
    }
}
