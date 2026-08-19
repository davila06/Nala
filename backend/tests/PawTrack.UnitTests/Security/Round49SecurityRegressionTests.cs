using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Notifications.DTOs;
using PawTrack.Application.Notifications.Queries.GetMyNotifications;
using PawTrack.Domain.Notifications;

namespace PawTrack.UnitTests.Security;

public sealed class Round49SecurityRegressionTests
{
    private readonly INotificationRepository _repo = Substitute.For<INotificationRepository>();
    private readonly GetMyNotificationsQueryHandler _sut;
    private static readonly (IReadOnlyList<Notification> Items, int Total, int Unread) Empty =
        (Array.Empty<Notification>(), 0, 0);

    public Round49SecurityRegressionTests() => _sut = new GetMyNotificationsQueryHandler(_repo);

    [Theory]
    [InlineData(999)]
    [InlineData(int.MaxValue)]
    [InlineData(100_000)]
    public async Task Handler_ClampsPageSize_ToMaximumOfFifty(int requestedPageSize)
    {
        var userId = Guid.NewGuid();
        _repo.GetPagedWithCountsAsync(userId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Empty);
        var result = await _sut.Handle(new GetMyNotificationsQuery(userId, 1, requestedPageSize), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.PageSize.Should().BeLessThanOrEqualTo(50);
        await _repo.Received(1).GetPagedWithCountsAsync(userId, Arg.Any<int>(), Arg.Is<int>(t => t <= 50), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task Handler_ClampsPageSize_ToMinimumOfOne(int requestedPageSize)
    {
        var userId = Guid.NewGuid();
        _repo.GetPagedWithCountsAsync(userId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Empty);
        var result = await _sut.Handle(new GetMyNotificationsQuery(userId, 1, requestedPageSize), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.PageSize.Should().BeGreaterThanOrEqualTo(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Handler_ClampsPageNumber_ToMinimumOfOne(int requestedPage)
    {
        var userId = Guid.NewGuid();
        _repo.GetPagedWithCountsAsync(userId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Empty);
        var result = await _sut.Handle(new GetMyNotificationsQuery(userId, requestedPage, 20), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.PageNumber.Should().BeGreaterThanOrEqualTo(1);
        await _repo.Received(1).GetPagedWithCountsAsync(userId, Arg.Is<int>(s => s >= 0), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}