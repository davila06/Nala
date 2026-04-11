using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Notifications.DTOs;
using PawTrack.Application.Notifications.Queries.GetMyNotifications;
using PawTrack.Domain.Common;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-49 security regression tests.
///
/// Gap: <c>GET /api/notifications?pageSize=N</c> accepts an unbounded <c>pageSize</c>
/// query parameter.  If the handler did not clamp it, a request with
/// <c>pageSize=100000</c> would return up to 100,000 rows per call — two DB queries
/// per call — causing a memory and CPU spike on every invocation.
///
/// Status: The handler already clamps to [1, 50]:
///
///   <code>
///   private const int MaxPageSize = 50;
///   var pageSize   = Math.Clamp(request.PageSize, 1, MaxPageSize);
///   var pageNumber = Math.Max(request.PageNumber, 1);
///   </code>
///
/// These regression tests document and lock in the existing clamping behaviour,
/// preventing a future refactor from accidentally removing it.
/// </summary>
public sealed class Round49SecurityRegressionTests
{
    private readonly INotificationRepository _repo = Substitute.For<INotificationRepository>();
    private readonly GetMyNotificationsQueryHandler _sut;

    public Round49SecurityRegressionTests()
    {
        _sut = new GetMyNotificationsQueryHandler(_repo);
    }

    [Theory]
    [InlineData(999)]
    [InlineData(int.MaxValue)]
    [InlineData(100_000)]
    public async Task Handler_ClampsPageSize_ToMaximumOfFifty(int requestedPageSize)
    {
        // Arrange
        var userId = Guid.NewGuid();
        _repo.GetByUserIdAsync(userId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
             .Returns([]);
        _repo.CountUnreadAsync(userId, Arg.Any<CancellationToken>())
             .Returns(0);

        // Act
        var result = await _sut.Handle(
            new GetMyNotificationsQuery(userId, PageNumber: 1, PageSize: requestedPageSize),
            CancellationToken.None);

        // Assert — the handler never queries more than MaxPageSize rows
        result.IsSuccess.Should().BeTrue();
        result.Value.PageSize.Should().BeLessThanOrEqualTo(50,
            $"pageSize={requestedPageSize} must be clamped to ≤50 to prevent " +
            "unbounded DB reads on a single request");

        // Verify GetByUserIdAsync was called with a clamped take value
        await _repo.Received(1).GetByUserIdAsync(
            userId,
            Arg.Any<int>(),
            Arg.Is<int>(take => take <= 50),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task Handler_ClampsPageSize_ToMinimumOfOne(int requestedPageSize)
    {
        var userId = Guid.NewGuid();
        _repo.GetByUserIdAsync(userId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
             .Returns([]);
        _repo.CountUnreadAsync(userId, Arg.Any<CancellationToken>())
             .Returns(0);

        var result = await _sut.Handle(
            new GetMyNotificationsQuery(userId, PageNumber: 1, PageSize: requestedPageSize),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PageSize.Should().BeGreaterThanOrEqualTo(1,
            "negative or zero pageSize must be clamped to at least 1");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Handler_ClampsPageNumber_ToMinimumOfOne(int requestedPage)
    {
        var userId = Guid.NewGuid();
        _repo.GetByUserIdAsync(userId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
             .Returns([]);
        _repo.CountUnreadAsync(userId, Arg.Any<CancellationToken>())
             .Returns(0);

        var result = await _sut.Handle(
            new GetMyNotificationsQuery(userId, PageNumber: requestedPage, PageSize: 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // skip (pageNumber - 1) * pageSize — with pageNumber clamped to 1, skip = 0
        await _repo.Received(1).GetByUserIdAsync(
            userId,
            Arg.Is<int>(skip => skip >= 0),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }
}
