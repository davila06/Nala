using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Collars.Commands.GenerateCollarDeviceKey;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars.Handlers;

public sealed class GenerateCollarDeviceKeyCommandHandlerTests
{
    private readonly ICollarRepository _collarRepo = Substitute.For<ICollarRepository>();
    private readonly ICollarDeviceCredentialRepository _credRepo = Substitute.For<ICollarDeviceCredentialRepository>();
    private readonly ICollarAuditRepository _auditRepo = Substitute.For<ICollarAuditRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly GenerateCollarDeviceKeyCommandHandler _sut;
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CollarId = Guid.NewGuid();

    public GenerateCollarDeviceKeyCommandHandlerTests()
    {
        _sut = new GenerateCollarDeviceKeyCommandHandler(_collarRepo, _credRepo, _auditRepo, _uow);
    }

    private Collar MakeCollar(Guid ownerId, bool active = true)
    {
        var collar = Collar.Register(Guid.NewGuid(), ownerId, CollarProvider.Generic, "IMEI-12345");
        typeof(Collar).GetProperty("Id")!.SetValue(collar, CollarId);
        if (!active) collar.Deactivate();
        return collar;
    }

    [Fact]
    public async Task Handle_HappyPath_ReturnsRawKeyAndRevokesExisting()
    {
        var collar = MakeCollar(OwnerId);
        var existingCred = CollarDeviceCredential.Create(CollarId, "oldhash");
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);
        _credRepo.GetForCollarAsync(CollarId, Arg.Any<CancellationToken>())
            .Returns(new[] { existingCred } as IReadOnlyList<CollarDeviceCredential>);

        var result = await _sut.Handle(new GenerateCollarDeviceKeyCommand(CollarId, OwnerId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.CollarDeviceKey.Should().StartWith("ptwk_collar_");
        existingCred.IsRevoked.Should().BeTrue("existing credential must be revoked before issuing a new one");
        await _credRepo.Received(1).AddAsync(Arg.Any<CollarDeviceCredential>(), default);
        await _uow.Received(1).SaveChangesAsync(default);
        await _auditRepo.Received(1).AddAsync(
            Arg.Is<CollarAuditEntry>(e => e.Event == CollarAuditEvent.DeviceKeyRegenerated),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CollarNotFound_ReturnsFailure()
    {
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns((Collar?)null);

        var result = await _sut.Handle(new GenerateCollarDeviceKeyCommand(CollarId, OwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*no encontrado*");
    }

    [Fact]
    public async Task Handle_WrongOwner_ReturnsAccessDenied()
    {
        var collar = MakeCollar(Guid.NewGuid()); // different owner
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await _sut.Handle(new GenerateCollarDeviceKeyCommand(CollarId, OwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Access denied.");
    }

    [Fact]
    public async Task Handle_InactiveCollar_ReturnsFailure()
    {
        var collar = MakeCollar(OwnerId, active: false);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await _sut.Handle(new GenerateCollarDeviceKeyCommand(CollarId, OwnerId), default);

        result.IsFailure.Should().BeTrue();
    }
}
