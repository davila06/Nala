using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Collars;
using PawTrack.Application.Collars.Commands.RedeemCollarHandoverCode;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars.Handlers;

public sealed class RedeemCollarHandoverCodeCommandHandlerTests
{
    private readonly ICollarHandoverCodeRepository _handoverRepo = Substitute.For<ICollarHandoverCodeRepository>();
    private readonly ICollarRepository _collarRepo = Substitute.For<ICollarRepository>();
    private readonly ICollarTagRepository _tagRepo = Substitute.For<ICollarTagRepository>();
    private readonly ICollarDeviceCredentialRepository _credRepo = Substitute.For<ICollarDeviceCredentialRepository>();
    private readonly ICollarAuditRepository _auditRepo = Substitute.For<ICollarAuditRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly RedeemCollarHandoverCodeCommandHandler _sut;
    private static readonly Guid OldOwnerId = Guid.NewGuid();
    private static readonly Guid NewOwnerId = Guid.NewGuid();
    private static readonly Guid CollarId = Guid.NewGuid();
    private static readonly Guid HandoverCodeId = Guid.NewGuid();
    private const string Serial = "PT-A3F9-0001234";
    private const string RawPin = "483920";

    public RedeemCollarHandoverCodeCommandHandlerTests()
    {
        _sut = new RedeemCollarHandoverCodeCommandHandler(
            _handoverRepo, _collarRepo, _tagRepo, _credRepo, _auditRepo, _uow);
    }

    private static CollarHandoverCode MakeCode()
    {
        var code = CollarHandoverCode.Create(CollarId, OldOwnerId, CollarDeviceKeyHasher.Compute(RawPin));
        typeof(CollarHandoverCode).GetProperty("Id")!.SetValue(code, HandoverCodeId);
        return code;
    }

    private static Collar MakeCollarWithSerial()
    {
        var collar = Collar.Register(Guid.NewGuid(), OldOwnerId, CollarProvider.Own, null);
        typeof(Collar).GetProperty("Id")!.SetValue(collar, CollarId);
        collar.SetTagSerial(Serial);
        return collar;
    }

    private static CollarTag MakeActivatedTag()
    {
        var tag = CollarTag.CreateFromFactory(Serial, "1.0.0");
        tag.Activate(CollarId);
        typeof(CollarTag).GetProperty("CollarId")!.SetValue(tag, CollarId);
        return tag;
    }

    [Fact]
    public async Task Handle_HappyPath_ReleasesSerialAndRedeemsCode()
    {
        var code = MakeCode();
        var collar = MakeCollarWithSerial();
        var tag = MakeActivatedTag();
        var cred = CollarDeviceCredential.Create(CollarId, "credhash");

        _handoverRepo.GetByIdAsync(HandoverCodeId, Arg.Any<CancellationToken>()).Returns(code);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);
        _tagRepo.GetBySerialAsync(Serial, Arg.Any<CancellationToken>()).Returns(tag);
        _credRepo.GetForCollarAsync(CollarId, Arg.Any<CancellationToken>())
            .Returns(new[] { cred } as IReadOnlyList<CollarDeviceCredential>);

        var result = await _sut.Handle(
            new RedeemCollarHandoverCodeCommand(HandoverCodeId, RawPin, NewOwnerId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Serial.Should().Be(Serial);
        code.IsRedeemed.Should().BeTrue();
        collar.IsActive.Should().BeFalse();
        tag.Status.Should().Be(CollarTagStatus.Unactivated);
        cred.IsRevoked.Should().BeTrue();
        await _auditRepo.Received(1).AddAsync(
            Arg.Is<CollarAuditEntry>(e => e.Event == CollarAuditEvent.HandoverCompleted),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WrongPin_RecordsFailedAttemptAndReturnsFailure()
    {
        var code = MakeCode();
        _handoverRepo.GetByIdAsync(HandoverCodeId, Arg.Any<CancellationToken>()).Returns(code);

        var result = await _sut.Handle(
            new RedeemCollarHandoverCodeCommand(HandoverCodeId, "000000", NewOwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*PIN incorrecto*");
        code.AttemptCount.Should().Be(1);
        await _uow.Received(1).SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_CodeNotFound_ReturnsFailure()
    {
        _handoverRepo.GetByIdAsync(HandoverCodeId, Arg.Any<CancellationToken>()).Returns((CollarHandoverCode?)null);

        var result = await _sut.Handle(
            new RedeemCollarHandoverCodeCommand(HandoverCodeId, RawPin, NewOwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*no encontrado*");
    }

    [Fact]
    public async Task Handle_AlreadyRedeemed_ReturnsFailure()
    {
        var code = MakeCode();
        code.Redeem(Guid.NewGuid());
        _handoverRepo.GetByIdAsync(HandoverCodeId, Arg.Any<CancellationToken>()).Returns(code);

        var result = await _sut.Handle(
            new RedeemCollarHandoverCodeCommand(HandoverCodeId, RawPin, NewOwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*ya fue utilizado*");
    }

    [Fact]
    public async Task Handle_Locked_ReturnsFailure()
    {
        var code = MakeCode();
        for (var i = 0; i < CollarHandoverCode.MaxAttempts; i++) code.RecordFailedAttempt();
        _handoverRepo.GetByIdAsync(HandoverCodeId, Arg.Any<CancellationToken>()).Returns(code);

        var result = await _sut.Handle(
            new RedeemCollarHandoverCodeCommand(HandoverCodeId, RawPin, NewOwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*Demasiados intentos*");
    }
}
