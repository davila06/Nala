using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Common.Settings;
using PawTrack.Infrastructure.Compliance;

namespace PawTrack.UnitTests.Compliance;

/// <summary>
/// Verifies PersonalDataRetentionJob purges sightings, closed chat threads, and read
/// notifications older than their configured retention windows, and only commits when
/// at least one category had rows deleted.
/// </summary>
public sealed class PersonalDataRetentionJobTests
{
    private readonly ISightingRepository _sightingRepo = Substitute.For<ISightingRepository>();
    private readonly IChatRepository _chatRepo = Substitute.For<IChatRepository>();
    private readonly INotificationRepository _notificationRepo = Substitute.For<INotificationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<PersonalDataRetentionJob> _logger = Substitute.For<ILogger<PersonalDataRetentionJob>>();

    private PersonalDataRetentionJob CreateSut(PersonalDataRetentionSettings settings) =>
        new(_sightingRepo, _chatRepo, _notificationRepo, _unitOfWork, Options.Create(settings), _logger);

    [Fact]
    public async Task ExecuteAsync_UsesConfiguredRetentionWindows()
    {
        var settings = new PersonalDataRetentionSettings
        {
            SightingRetentionDays = 730,
            ClosedChatRetentionDays = 730,
            ReadNotificationRetentionDays = 365,
        };
        _sightingRepo.DeleteReportedBeforeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0);
        _chatRepo.DeleteClosedThreadsOlderThanAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0);
        _notificationRepo.DeleteReadBeforeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0);

        var sut = CreateSut(settings);
        var before = DateTimeOffset.UtcNow;
        await sut.ExecuteAsync(CancellationToken.None);

        await _sightingRepo.Received(1).DeleteReportedBeforeAsync(
            Arg.Is<DateTimeOffset>(d => d >= before.AddDays(-731) && d <= before.AddDays(-729)),
            Arg.Any<CancellationToken>());

        await _chatRepo.Received(1).DeleteClosedThreadsOlderThanAsync(
            Arg.Is<DateTimeOffset>(d => d >= before.AddDays(-731) && d <= before.AddDays(-729)),
            Arg.Any<CancellationToken>());

        await _notificationRepo.Received(1).DeleteReadBeforeAsync(
            Arg.Is<DateTimeOffset>(d => d >= before.AddDays(-366) && d <= before.AddDays(-364)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_CommitsOnce_WhenAnyCategoryDeletedRows()
    {
        var settings = new PersonalDataRetentionSettings();
        _sightingRepo.DeleteReportedBeforeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(2);
        _chatRepo.DeleteClosedThreadsOlderThanAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0);
        _notificationRepo.DeleteReadBeforeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0);

        var sut = CreateSut(settings);
        await sut.ExecuteAsync(CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SkipsCommit_WhenNothingDeleted()
    {
        var settings = new PersonalDataRetentionSettings();
        _sightingRepo.DeleteReportedBeforeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0);
        _chatRepo.DeleteClosedThreadsOlderThanAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0);
        _notificationRepo.DeleteReadBeforeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0);

        var sut = CreateSut(settings);
        await sut.ExecuteAsync(CancellationToken.None);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
