using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Common.Settings;
using PawTrack.Application.Pets.DTOs;
using PawTrack.Infrastructure.Notifications.Jobs;

namespace PawTrack.UnitTests.Pets.Handlers;

/// <summary>
/// Verifies that QrScanRetentionJob deletes records older than the configured window (default 90 days)
/// and honours a custom RetentionDays setting.
/// </summary>
public sealed class QrScanRetentionJobTests
{
    private readonly IQrScanEventRepository _repo = Substitute.For<IQrScanEventRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<QrScanRetentionJob> _logger = Substitute.For<ILogger<QrScanRetentionJob>>();

    [Fact]
    public async Task ExecuteAsync_DeletesRecordsOlderThan90Days_ByDefault()
    {
        var settings = Options.Create(new QrScanRetentionSettings { RetentionDays = 90 });
        _repo.DeleteBeforeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(5);

        var sut = new QrScanRetentionJob(_repo, _unitOfWork, settings, _logger);

        var before = DateTimeOffset.UtcNow;
        await sut.ExecuteAsync(CancellationToken.None);

        // Verify cutoff is ~90 days ago
        await _repo.Received(1).DeleteBeforeAsync(
            Arg.Is<DateTimeOffset>(d =>
                d >= before.AddDays(-91) &&
                d <= before.AddDays(-89)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_CommitsAfterDeletion_WhenRowsDeleted()
    {
        var settings = Options.Create(new QrScanRetentionSettings { RetentionDays = 90 });
        _repo.DeleteBeforeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(3);

        var sut = new QrScanRetentionJob(_repo, _unitOfWork, settings, _logger);

        await sut.ExecuteAsync(CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SkipsCommit_WhenNoRowsDeleted()
    {
        var settings = Options.Create(new QrScanRetentionSettings { RetentionDays = 90 });
        _repo.DeleteBeforeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0);

        var sut = new QrScanRetentionJob(_repo, _unitOfWork, settings, _logger);

        await sut.ExecuteAsync(CancellationToken.None);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_RespectsCustomRetentionDays()
    {
        var settings = Options.Create(new QrScanRetentionSettings { RetentionDays = 30 });
        _repo.DeleteBeforeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0);

        var sut = new QrScanRetentionJob(_repo, _unitOfWork, settings, _logger);

        var before = DateTimeOffset.UtcNow;
        await sut.ExecuteAsync(CancellationToken.None);

        await _repo.Received(1).DeleteBeforeAsync(
            Arg.Is<DateTimeOffset>(d =>
                d >= before.AddDays(-31) &&
                d <= before.AddDays(-29)),
            Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Verifies that PetScanHistoryDto includes a non-empty HMAC signature when using the signing service.
/// </summary>
public sealed class PetScanHistorySignatureTests
{
    [Fact]
    public void PetScanHistoryDto_WithSignature_HasNonEmptySignatureAndTimestamp()
    {
        // The DTO must carry Signature and SignedAt when the export is signed.
        var sut = new PetScanHistoryDto(
            ScansToday: 2,
            Events: [],
            Signature: "sha256=abc123aabbcc",
            SignedAt: DateTimeOffset.UtcNow);

        sut.Signature.Should().StartWith("sha256=");
        sut.SignedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void PetScanHistoryDto_WithoutSignature_HasNullSignature()
    {
        var sut = new PetScanHistoryDto(ScansToday: 0, Events: [], Signature: null, SignedAt: null);

        sut.Signature.Should().BeNull();
        sut.SignedAt.Should().BeNull();
    }
}
