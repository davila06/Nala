using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Collars.Commands.DeactivateCollarTag;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars.Handlers;

public sealed class DeactivateCollarTagCommandHandlerTests
{
    private readonly ICollarTagRepository _tagRepo = Substitute.For<ICollarTagRepository>();
    private readonly ICollarDeviceCredentialRepository _credRepo = Substitute.For<ICollarDeviceCredentialRepository>();
    private readonly ICollarRepository _collarRepo = Substitute.For<ICollarRepository>();
    private readonly ICollarAuditRepository _auditRepo = Substitute.For<ICollarAuditRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly DeactivateCollarTagCommandHandler _sut;
    private static readonly Guid OwnerId = Guid.NewGuid();
    private const string Serial = "PT-A3F9-0001234";

    public DeactivateCollarTagCommandHandlerTests()
    {
        _sut = new DeactivateCollarTagCommandHandler(_tagRepo, _credRepo, _collarRepo, _auditRepo, _uow);
    }

    [Fact]
    public async Task Handle_HappyPath_DeactivatesTagAndRevokesCredentials()
    {
        var collarId = Guid.NewGuid();
        var collar = Collar.Register(Guid.NewGuid(), OwnerId, CollarProvider.Own, null);
        typeof(Collar).GetProperty("Id")!.SetValue(collar, collarId);
        typeof(Collar).GetProperty("OwnerId")!.SetValue(collar, OwnerId);

        var tag = CollarTag.CreateFromFactory(Serial, "1.0.0");
        tag.Activate(collarId);
        typeof(CollarTag).GetProperty("CollarId")!.SetValue(tag, collarId);

        var cred = CollarDeviceCredential.Create(collarId, "somehash");

        _tagRepo.GetBySerialAsync(Serial, Arg.Any<CancellationToken>()).Returns(tag);
        _collarRepo.GetByIdAsync(collarId, Arg.Any<CancellationToken>()).Returns(collar);
        _credRepo.GetForCollarAsync(collarId, Arg.Any<CancellationToken>())
            .Returns(new[] { cred } as IReadOnlyList<CollarDeviceCredential>);

        var result = await _sut.Handle(new DeactivateCollarTagCommand(Serial, OwnerId), default);

        result.IsSuccess.Should().BeTrue();
        tag.Status.Should().Be(CollarTagStatus.Unactivated);
        collar.IsActive.Should().BeFalse();
        cred.IsRevoked.Should().BeTrue();
        await _uow.Received(1).SaveChangesAsync(default);
        await _auditRepo.Received(1).AddAsync(
            Arg.Is<CollarAuditEntry>(e => e.Event == CollarAuditEvent.Deactivated),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SerialNotFound_ReturnsFailure()
    {
        _tagRepo.GetBySerialAsync(Serial, Arg.Any<CancellationToken>()).Returns((CollarTag?)null);

        var result = await _sut.Handle(new DeactivateCollarTagCommand(Serial, OwnerId), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WrongOwner_ReturnsAccessDenied()
    {
        var collarId = Guid.NewGuid();
        var collar = Collar.Register(Guid.NewGuid(), Guid.NewGuid(), CollarProvider.Own, null);
        typeof(Collar).GetProperty("Id")!.SetValue(collar, collarId);

        var tag = CollarTag.CreateFromFactory(Serial, "1.0.0");
        tag.Activate(collarId);
        typeof(CollarTag).GetProperty("CollarId")!.SetValue(tag, collarId);

        _tagRepo.GetBySerialAsync(Serial, Arg.Any<CancellationToken>()).Returns(tag);
        _collarRepo.GetByIdAsync(collarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await _sut.Handle(new DeactivateCollarTagCommand(Serial, OwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Access denied.");
    }
}
