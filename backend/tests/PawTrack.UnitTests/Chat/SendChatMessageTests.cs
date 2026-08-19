using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.Application.Chat.Commands.SendChatMessage;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Chat;

namespace PawTrack.UnitTests.Chat;

public sealed class SendChatMessageCommandHandlerTests
{
    private readonly IChatRepository _chatRepo = Substitute.For<IChatRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly INotificationDispatcher _notifications = Substitute.For<INotificationDispatcher>();
    private readonly ILostPetRepository _lostPetRepo = Substitute.For<ILostPetRepository>();
    private readonly IPetRepository _petRepo = Substitute.For<IPetRepository>();
    private readonly IPiiScrubber _pii = Substitute.For<IPiiScrubber>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly SendChatMessageCommandHandler _sut;

    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid FinderId = Guid.NewGuid();
    private static readonly Guid EventId = Guid.NewGuid();

    public SendChatMessageCommandHandlerTests()
    {
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        // PiiScrubber returns input unchanged by default
        _pii.Scrub(Arg.Any<string?>()).Returns(x => x.Arg<string?>());
        _notifications.DispatchNewChatMessageAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _sut = new SendChatMessageCommandHandler(
            _chatRepo, _userRepo, _notifications, _lostPetRepo, _petRepo,
            _pii, _uow, NullLogger<SendChatMessageCommandHandler>.Instance);
    }

    private ChatThread MakeOpenThread() =>
        ChatThread.Open(EventId, FinderId, OwnerId);

    [Fact]
    public async Task Handle_ValidMessage_PersistsAndReturnsId()
    {
        var thread = MakeOpenThread();
        _chatRepo.GetThreadByIdAsync(thread.Id, Arg.Any<CancellationToken>()).Returns(thread);

        var cmd = new SendChatMessageCommand(thread.Id, FinderId, "¿Está bien el perro?");
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _chatRepo.Received(1).AddMessageAsync(
            Arg.Is<ChatMessage>(m => m.ThreadId == thread.Id && m.SenderUserId == FinderId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MessageBodyIsScrubbed()
    {
        var thread = MakeOpenThread();
        _chatRepo.GetThreadByIdAsync(thread.Id, Arg.Any<CancellationToken>()).Returns(thread);
        // Simulate scrubber adding [REDACTED] even on content that passed the guard
        const string raw = "Vi al perro cerca del norte";
        const string scrubbed = "Vi al perro cerca del [REDACTED]";
        _pii.Scrub(raw).Returns(scrubbed);

        var cmd = new SendChatMessageCommand(thread.Id, FinderId, raw);
        await _sut.Handle(cmd, CancellationToken.None);

        await _chatRepo.Received(1).AddMessageAsync(
            Arg.Is<ChatMessage>(m => m.Body == scrubbed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ContainsEmail_RejectsBeforePersisting()
    {
        var thread = MakeOpenThread();
        _chatRepo.GetThreadByIdAsync(thread.Id, Arg.Any<CancellationToken>()).Returns(thread);

        var cmd = new SendChatMessageCommand(thread.Id, FinderId, "write me at user@mail.com");
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _chatRepo.DidNotReceive().AddMessageAsync(Arg.Any<ChatMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyBody_ReturnsFailure()
    {
        var thread = MakeOpenThread();
        _chatRepo.GetThreadByIdAsync(thread.Id, Arg.Any<CancellationToken>()).Returns(thread);

        var cmd = new SendChatMessageCommand(thread.Id, FinderId, "   ");
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_BodyExceedsMaxLength_ReturnsFailure()
    {
        var thread = MakeOpenThread();
        _chatRepo.GetThreadByIdAsync(thread.Id, Arg.Any<CancellationToken>()).Returns(thread);

        var cmd = new SendChatMessageCommand(thread.Id, FinderId, new string('x', 801));
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonParticipant_ReturnsFailure()
    {
        var thread = MakeOpenThread();
        _chatRepo.GetThreadByIdAsync(thread.Id, Arg.Any<CancellationToken>()).Returns(thread);

        var outsider = Guid.NewGuid();
        var cmd = new SendChatMessageCommand(thread.Id, outsider, "Hello");
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("acceso"));
    }

    [Fact]
    public async Task Handle_ClosedThread_ReturnsFailure()
    {
        var thread = MakeOpenThread();
        thread.Close();
        _chatRepo.GetThreadByIdAsync(thread.Id, Arg.Any<CancellationToken>()).Returns(thread);

        var cmd = new SendChatMessageCommand(thread.Id, FinderId, "Still here");
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ThreadNotFound_ReturnsFailure()
    {
        _chatRepo.GetThreadByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                 .Returns((ChatThread?)null);

        var cmd = new SendChatMessageCommand(Guid.NewGuid(), FinderId, "Hello");
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}

// ── Contact-guard behaviour via handler integration ──────────────────────────

public sealed class ChatContactGuardTests
{
    private readonly IChatRepository _chatRepo = Substitute.For<IChatRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly INotificationDispatcher _notifications = Substitute.For<INotificationDispatcher>();
    private readonly ILostPetRepository _lostPetRepo = Substitute.For<ILostPetRepository>();
    private readonly IPetRepository _petRepo = Substitute.For<IPetRepository>();
    private readonly IPiiScrubber _pii = Substitute.For<IPiiScrubber>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private static readonly Guid FinderId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();

    private SendChatMessageCommandHandler MakeSut() =>
        new(_chatRepo, _userRepo, _notifications, _lostPetRepo, _petRepo,
            _pii, _uow, NullLogger<SendChatMessageCommandHandler>.Instance);

    [Theory]
    [InlineData("Escríbeme a user@example.com")]
    [InlineData("llama al 8888-1234")]
    [InlineData("mi tel: +506 8881 2345")]
    public async Task Handle_BodyWithContactDetail_ReturnsFailure(string body)
    {
        var thread = ChatThread.Open(Guid.NewGuid(), FinderId, OwnerId);
        _chatRepo.GetThreadByIdAsync(thread.Id, Arg.Any<CancellationToken>()).Returns(thread);
        _pii.Scrub(Arg.Any<string?>()).Returns(x => x.Arg<string?>());
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await MakeSut().Handle(
            new SendChatMessageCommand(thread.Id, FinderId, body), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _chatRepo.DidNotReceive().AddMessageAsync(Arg.Any<ChatMessage>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("El perro es de color café")]
    [InlineData("Lo vi en el parque central cerca de la fuente")]
    public async Task Handle_CleanBody_Persists(string body)
    {
        var thread = ChatThread.Open(Guid.NewGuid(), FinderId, OwnerId);
        _chatRepo.GetThreadByIdAsync(thread.Id, Arg.Any<CancellationToken>()).Returns(thread);
        _pii.Scrub(Arg.Any<string?>()).Returns(x => x.Arg<string?>());
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _notifications.DispatchNewChatMessageAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await MakeSut().Handle(
            new SendChatMessageCommand(thread.Id, FinderId, body), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
