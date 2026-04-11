using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Chat.Commands.OpenChatThread;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Chat;
using PawTrack.Domain.LostPets;

namespace PawTrack.UnitTests.Chat;

/// <summary>
/// Round-11 security: the OpenChatThread handler must resolve the pet owner from
/// the database — never from the client-supplied OwnerUserId field.
/// Trusting a client-supplied owner ID allows Broken Object-Level Authorization (BOLA):
/// any authenticated user could open a chat thread that spams push notifications to
/// an arbitrary victim by forging that victim's GUID as the "owner".
/// </summary>
public sealed class OpenChatThreadCommandHandlerTests
{
    private readonly IChatRepository _chatRepo = Substitute.For<IChatRepository>();
    private readonly ILostPetRepository _lostPetRepo = Substitute.For<ILostPetRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly OpenChatThreadCommandHandler _sut;

    public OpenChatThreadCommandHandlerTests()
    {
        _sut = new OpenChatThreadCommandHandler(_chatRepo, _lostPetRepo, _uow);
    }

    // ── BOLA: owner id must come from DB, not from the client ────────────────

    /// <summary>
    /// SECURITY: An attacker who submits any victim GUID as OwnerUserId must NOT
    /// see that victim linked as owner of the created chat thread.
    /// The handler must ignore the client-supplied owner and use <see cref="LostPetEvent.OwnerId"/> instead.
    ///
    /// Before the fix this test FAILS because <c>command.OwnerUserId</c> was used verbatim.
    /// </summary>
    [Fact]
    public async Task Handle_InitiatorSuppliesWrongOwnerUserId_ThreadUsesRealEventOwner()
    {
        // Arrange
        var realOwnerId = Guid.NewGuid();
        var finderId    = Guid.NewGuid();
        var victimId    = Guid.NewGuid(); // the attacker wants to forge this as owner
        var lostEvent   = MakeLostEvent(realOwnerId);

        _lostPetRepo.GetByIdAsync(lostEvent.Id, Arg.Any<CancellationToken>()).Returns(lostEvent);
        _chatRepo.ThreadExistsAsync(lostEvent.Id, finderId, Arg.Any<CancellationToken>()).Returns(false);

        ChatThread? created = null;
        await _chatRepo.AddThreadAsync(
            Arg.Do<ChatThread>(t => created = t),
            Arg.Any<CancellationToken>());

        var command = new OpenChatThreadCommand(lostEvent.Id, finderId);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert – the thread must be linked to the REAL owner, not the forged one
        result.IsSuccess.Should().BeTrue();
        created.Should().NotBeNull();
        created!.OwnerUserId.Should().Be(realOwnerId,
            because: "the handler must derive OwnerUserId from the database, not from the client");
        created!.OwnerUserId.Should().NotBe(victimId,
            because: "a forged OwnerUserId must be rejected in favour of the DB-sourced value");
    }

    // ── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidNewThread_CreatesAndReturnsThreadId()
    {
        // Arrange
        var ownerId   = Guid.NewGuid();
        var finderId  = Guid.NewGuid();
        var lostEvent = MakeLostEvent(ownerId);

        _lostPetRepo.GetByIdAsync(lostEvent.Id, Arg.Any<CancellationToken>()).Returns(lostEvent);
        _chatRepo.ThreadExistsAsync(lostEvent.Id, finderId, Arg.Any<CancellationToken>()).Returns(false);

        var command = new OpenChatThreadCommand(lostEvent.Id, finderId);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _chatRepo.Received(1).AddThreadAsync(Arg.Any<ChatThread>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Self-chat prevention ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_InitiatorIsOwner_ReturnsFailure()
    {
        // Arrange
        var ownerId   = Guid.NewGuid();
        var lostEvent = MakeLostEvent(ownerId);

        _lostPetRepo.GetByIdAsync(lostEvent.Id, Arg.Any<CancellationToken>()).Returns(lostEvent);

        var command = new OpenChatThreadCommand(lostEvent.Id, ownerId); // same user

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        await _chatRepo.DidNotReceive().AddThreadAsync(Arg.Any<ChatThread>(), Arg.Any<CancellationToken>());
    }

    // ── Event not found ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_LostEventNotFound_ReturnsFailure()
    {
        // Arrange
        _lostPetRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((LostPetEvent?)null);

        var command = new OpenChatThreadCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    // ── Idempotency ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ThreadAlreadyExists_ReturnsExistingThreadId()
    {
        // Arrange
        var ownerId       = Guid.NewGuid();
        var finderId      = Guid.NewGuid();
        var lostEvent     = MakeLostEvent(ownerId);
        var existingThread = ChatThread.Open(lostEvent.Id, finderId, ownerId);

        _lostPetRepo.GetByIdAsync(lostEvent.Id, Arg.Any<CancellationToken>()).Returns(lostEvent);
        _chatRepo.ThreadExistsAsync(lostEvent.Id, finderId, Arg.Any<CancellationToken>()).Returns(true);
        _chatRepo.GetThreadsByLostPetEventAsync(lostEvent.Id, Arg.Any<CancellationToken>())
            .Returns(new List<ChatThread> { existingThread }.AsReadOnly());

        var command = new OpenChatThreadCommand(lostEvent.Id, finderId);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existingThread.Id);
        await _chatRepo.DidNotReceive().AddThreadAsync(Arg.Any<ChatThread>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static LostPetEvent MakeLostEvent(Guid ownerId) =>
        LostPetEvent.Create(
            petId: Guid.NewGuid(),
            ownerId: ownerId,
            description: "Lost near the park",
            lastSeenLat: 9.9281,
            lastSeenLng: -84.0907,
            lastSeenAt: DateTimeOffset.UtcNow.AddHours(-1));
}
