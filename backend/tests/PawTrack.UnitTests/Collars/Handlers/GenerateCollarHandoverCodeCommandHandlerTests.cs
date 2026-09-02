using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Collars.Commands.GenerateCollarHandoverCode;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars.Handlers;

public sealed class GenerateCollarHandoverCodeCommandHandlerTests
{
    private readonly ICollarRepository _collarRepo = Substitute.For<ICollarRepository>();
    private readonly ICollarHandoverCodeRepository _handoverRepo = Substitute.For<ICollarHandoverCodeRepository>();
    private readonly ICollarAuditRepository _auditRepo = Substitute.For<ICollarAuditRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly GenerateCollarHandoverCodeCommandHandler _sut;
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CollarId = Guid.NewGuid();

    public GenerateCollarHandoverCodeCommandHandlerTests()
    {
        _sut = new GenerateCollarHandoverCodeCommandHandler(_collarRepo, _handoverRepo, _auditRepo, _uow);
    }

    private static Collar MakeCollarWithSerial(Guid ownerId, string serial = "PT-A3F9-0001234")
    {
        var collar = Collar.Register(Guid.NewGuid(), ownerId, CollarProvider.Own, null);
        typeof(Collar).GetProperty("Id")!.SetValue(collar, CollarId);
        collar.SetTagSerial(serial);
        return collar;
    }

    [Fact]
    public async Task Handle_HappyPath_ReturnsPinAndLogsAudit()
    {
        var collar = MakeCollarWithSerial(OwnerId);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);
        _handoverRepo.GetActiveForCollarAsync(CollarId, Arg.Any<CancellationToken>()).Returns((CollarHandoverCode?)null);

        var result = await _sut.Handle(new GenerateCollarHandoverCodeCommand(CollarId, OwnerId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Pin.Should().MatchRegex("^[0-9]{6}$");
        await _handoverRepo.Received(1).AddAsync(Arg.Any<CollarHandoverCode>(), Arg.Any<CancellationToken>());
        await _auditRepo.Received(1).AddAsync(
            Arg.Is<CollarAuditEntry>(e => e.Event == CollarAuditEvent.HandoverCodeGenerated),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_ExistingActiveCode_CancelsPreviousOne()
    {
        var collar = MakeCollarWithSerial(OwnerId);
        var existing = CollarHandoverCode.Create(CollarId, OwnerId, "oldhash");
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);
        _handoverRepo.GetActiveForCollarAsync(CollarId, Arg.Any<CancellationToken>()).Returns(existing);

        await _sut.Handle(new GenerateCollarHandoverCodeCommand(CollarId, OwnerId), default);

        existing.IsCancelled.Should().BeTrue();
        _handoverRepo.Received(1).Update(existing);
    }

    [Fact]
    public async Task Handle_CollarWithoutSerial_ReturnsFailure()
    {
        var collar = Collar.Register(Guid.NewGuid(), OwnerId, CollarProvider.Tractive, "TRC-1");
        typeof(Collar).GetProperty("Id")!.SetValue(collar, CollarId);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await _sut.Handle(new GenerateCollarHandoverCodeCommand(CollarId, OwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*serial físico*");
    }

    [Fact]
    public async Task Handle_WrongOwner_ReturnsAccessDenied()
    {
        var collar = MakeCollarWithSerial(Guid.NewGuid());
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await _sut.Handle(new GenerateCollarHandoverCodeCommand(CollarId, OwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Access denied.");
    }
}
