using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Collars.Queries.GetCollarAuditLog;
using PawTrack.Application.Collars.Queries.GetCollarAuditLogBySerial;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars.Handlers;

public sealed class GetCollarAuditLogQueryHandlerTests
{
    private readonly ICollarRepository _collarRepo = Substitute.For<ICollarRepository>();
    private readonly ICollarAuditRepository _auditRepo = Substitute.For<ICollarAuditRepository>();
    private readonly GetCollarAuditLogQueryHandler _sut;

    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CollarId = Guid.NewGuid();

    public GetCollarAuditLogQueryHandlerTests()
    {
        _sut = new GetCollarAuditLogQueryHandler(_collarRepo, _auditRepo);
    }

    private static Collar MakeCollar(Guid ownerId)
    {
        var collar = Collar.Register(Guid.NewGuid(), ownerId, CollarProvider.Own, null);
        typeof(Collar).GetProperty("Id")!.SetValue(collar, CollarId);
        return collar;
    }

    [Fact]
    public async Task Handle_Owner_ReturnsEntries()
    {
        var collar = MakeCollar(OwnerId);
        var entry = CollarAuditEntry.Create(CollarAuditEvent.Activated, "test", collarId: CollarId);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);
        _auditRepo.GetByCollarIdAsync(CollarId, 0, 50, Arg.Any<CancellationToken>())
            .Returns(new[] { entry });

        var result = await _sut.Handle(new GetCollarAuditLogQuery(CollarId, OwnerId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(e => e.Event == "Activated");
    }

    [Fact]
    public async Task Handle_WrongOwner_ReturnsAccessDenied()
    {
        var collar = MakeCollar(Guid.NewGuid());
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await _sut.Handle(new GetCollarAuditLogQuery(CollarId, OwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Access denied.");
    }

    [Fact]
    public async Task Handle_CollarNotFound_ReturnsFailure()
    {
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns((Collar?)null);

        var result = await _sut.Handle(new GetCollarAuditLogQuery(CollarId, OwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*no encontrado*");
    }
}

public sealed class GetCollarAuditLogBySerialQueryHandlerTests
{
    private readonly ICollarAuditRepository _auditRepo = Substitute.For<ICollarAuditRepository>();
    private readonly GetCollarAuditLogBySerialQueryHandler _sut;
    private const string Serial = "PT-A3F9-0001234";

    public GetCollarAuditLogBySerialQueryHandlerTests()
    {
        _sut = new GetCollarAuditLogBySerialQueryHandler(_auditRepo);
    }

    [Fact]
    public async Task Handle_ReturnsEntriesForSerial()
    {
        var entry = CollarAuditEntry.Create(CollarAuditEvent.SerialRegistered, "test", serial: Serial);
        _auditRepo.GetBySerialAsync(Serial, 0, 50, Arg.Any<CancellationToken>())
            .Returns(new[] { entry });

        var result = await _sut.Handle(new GetCollarAuditLogBySerialQuery(Serial), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(e => e.Event == "SerialRegistered");
    }
}
