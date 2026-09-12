using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Collars.Commands.CollarHeartbeat;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars.Commands;

public sealed class CollarHeartbeatCommandHandlerTests
{
    private readonly ICollarTagRepository _tagRepo = Substitute.For<ICollarTagRepository>();
    private readonly ICollarRepository _collarRepo = Substitute.For<ICollarRepository>();
    private readonly ICollarAuditRepository _auditRepo = Substitute.For<ICollarAuditRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CollarHeartbeatCommandHandler CreateSut() =>
        new(_tagRepo, _collarRepo, _auditRepo, _unitOfWork);

    [Fact]
    public async Task Handle_WhenSerialMismatch_ReturnsFailureAndLogsAudit()
    {
        var collarId = Guid.NewGuid();
        var tag = CollarTag.CreateFromFactory("PT-ABCD-0000001", "1.0.0");
        tag.Activate(Guid.NewGuid()); // different collar

        _tagRepo.GetBySerialAsync("PT-ABCD-0000001", Arg.Any<CancellationToken>())
            .Returns(tag);

        var sut = CreateSut();
        var result = await sut.Handle(
            new CollarHeartbeatCommand(collarId, "PT-ABCD-0000001", 90), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Serial mismatch — heartbeat rejected.");
        await _auditRepo.Received(1).AddAsync(Arg.Any<CollarAuditEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMatchingSerialAndActiveCollar_UpdatesHeartbeatAndPing()
    {
        var collarId = Guid.NewGuid();
        var collar = Collar.Register(Guid.NewGuid(), Guid.NewGuid(), CollarProvider.Own, "ext-1");
        collar.MarkOffline();

        var tag = CollarTag.CreateFromFactory("PT-ABCD-0000002", "1.0.0");
        tag.Activate(collarId);

        _tagRepo.GetBySerialAsync("PT-ABCD-0000002", Arg.Any<CancellationToken>())
            .Returns(tag);
        _collarRepo.GetByIdAsync(collarId, Arg.Any<CancellationToken>())
            .Returns(collar);

        var sut = CreateSut();
        var result = await sut.Handle(
            new CollarHeartbeatCommand(collarId, "PT-ABCD-0000002", 75, -65, "1.2.0"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        collar.BatteryPercent.Should().Be(75);
        collar.IsOffline.Should().BeFalse();
        tag.FirmwareVersion.Should().Be("1.2.0");
        tag.LastPingAt.Should().NotBeNull();

        _collarRepo.Received(1).Update(collar);
        _tagRepo.Received(1).Update(tag);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
