using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Collars.Commands.CancelCollarHandoverCode;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars.Handlers;

public sealed class CancelCollarHandoverCodeCommandHandlerTests
{
    private readonly ICollarHandoverCodeRepository _handoverRepo = Substitute.For<ICollarHandoverCodeRepository>();
    private readonly ICollarAuditRepository _auditRepo = Substitute.For<ICollarAuditRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly CancelCollarHandoverCodeCommandHandler _sut;
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CollarId = Guid.NewGuid();
    private static readonly Guid HandoverCodeId = Guid.NewGuid();

    public CancelCollarHandoverCodeCommandHandlerTests()
    {
        _sut = new CancelCollarHandoverCodeCommandHandler(_handoverRepo, _auditRepo, _uow);
    }

    private static CollarHandoverCode MakeCode(Guid ownerId)
    {
        var code = CollarHandoverCode.Create(CollarId, ownerId, "hash");
        typeof(CollarHandoverCode).GetProperty("Id")!.SetValue(code, HandoverCodeId);
        return code;
    }

    [Fact]
    public async Task Handle_HappyPath_CancelsCode()
    {
        var code = MakeCode(OwnerId);
        _handoverRepo.GetByIdAsync(HandoverCodeId, Arg.Any<CancellationToken>()).Returns(code);

        var result = await _sut.Handle(new CancelCollarHandoverCodeCommand(HandoverCodeId, OwnerId), default);

        result.IsSuccess.Should().BeTrue();
        code.IsCancelled.Should().BeTrue();
        await _uow.Received(1).SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WrongOwner_ReturnsAccessDenied()
    {
        var code = MakeCode(Guid.NewGuid());
        _handoverRepo.GetByIdAsync(HandoverCodeId, Arg.Any<CancellationToken>()).Returns(code);

        var result = await _sut.Handle(new CancelCollarHandoverCodeCommand(HandoverCodeId, OwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Access denied.");
    }

    [Fact]
    public async Task Handle_AlreadyRedeemed_ReturnsFailure()
    {
        var code = MakeCode(OwnerId);
        code.Redeem(Guid.NewGuid());
        _handoverRepo.GetByIdAsync(HandoverCodeId, Arg.Any<CancellationToken>()).Returns(code);

        var result = await _sut.Handle(new CancelCollarHandoverCodeCommand(HandoverCodeId, OwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*ya canjeado*");
    }
}
