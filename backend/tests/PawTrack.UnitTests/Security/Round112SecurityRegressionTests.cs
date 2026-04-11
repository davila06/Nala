using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Chat.Queries.GetChatThreads;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Chat;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// SEC-01 (R112) — Broken Object-Level Authorization (BOLA) timing leak in GetChatThreadsQuery.
///
/// Vulnerability (before fix): The handler called the unfiltered
/// <c>GetThreadsByLostPetEventAsync(lostPetEventId)</c> which loaded ALL threads for an event
/// before filtering in memory. Any authenticated user could supply an arbitrary
/// <c>LostPetEventId</c> to trigger DB work and receive a timing signal confirming
/// the event exists — even when they have no threads associated with it.
///
/// Fix: The handler must call <c>GetThreadsByLostPetEventAndParticipantAsync</c> which pushes
/// the participant predicate to the database, ensuring the query touches only rows the
/// requesting user is authorised to see and leaks no timing information about events
/// they are not a participant of.
/// </summary>
public sealed class Round112SecurityRegressionTests
{
    private readonly IChatRepository    _chatRepo = Substitute.For<IChatRepository>();
    private readonly IUserRepository    _userRepo = Substitute.For<IUserRepository>();

    private readonly GetChatThreadsQueryHandler _sut;

    public Round112SecurityRegressionTests()
    {
        _sut = new GetChatThreadsQueryHandler(_chatRepo, _userRepo);
    }

    // ── R112-A: participant-filtered method is called ────────────────────────

    [Fact]
    public async Task R112_GetChatThreads_UsesParticipantScopedQuery_NeverUnfilteredBulkFetch()
    {
        // Arrange
        var lostEventId      = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();

        _chatRepo
            .GetThreadsByLostPetEventAndParticipantAsync(
                lostEventId, requestingUserId, Arg.Any<CancellationToken>())
            .Returns(new List<ChatThread>().AsReadOnly() as IReadOnlyList<ChatThread>);

        // Act
        var result = await _sut.Handle(
            new GetChatThreadsQuery(lostEventId, requestingUserId),
            CancellationToken.None);

        // Assert — unfiltered bulk method MUST NOT be called (timing BOLA vector)
        await _chatRepo.DidNotReceive()
            .GetThreadsByLostPetEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        // Assert — participant-scoped method MUST be called with the correct identifiers
        await _chatRepo.Received(1)
            .GetThreadsByLostPetEventAndParticipantAsync(
                lostEventId, requestingUserId, Arg.Any<CancellationToken>());

        result.IsSuccess.Should().BeTrue();
    }

    // ── R112-B: non-participant gets empty result, not an error ──────────────

    [Fact]
    public async Task R112_GetChatThreads_NonParticipant_ReceivesEmptyList_NotError()
    {
        // Arrange — DB returns empty list because the user has no threads for this event
        var lostEventId      = Guid.NewGuid();
        var nonParticipantId = Guid.NewGuid();

        _chatRepo
            .GetThreadsByLostPetEventAndParticipantAsync(
                lostEventId, nonParticipantId, Arg.Any<CancellationToken>())
            .Returns(new List<ChatThread>().AsReadOnly() as IReadOnlyList<ChatThread>);

        // Act
        var result = await _sut.Handle(
            new GetChatThreadsQuery(lostEventId, nonParticipantId),
            CancellationToken.None);

        // Assert — a non-participant silently receives an empty list; no error is surfaced
        // (surfacing a 404 would leak whether the event exists)
        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().BeEmpty();
    }
}
