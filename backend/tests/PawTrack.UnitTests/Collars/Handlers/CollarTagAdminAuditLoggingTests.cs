using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Collars.Commands.Admin;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars.Handlers;

public sealed class CollarTagAdminAuditLoggingTests
{
    private readonly ICollarTagRepository _tagRepo = Substitute.For<ICollarTagRepository>();
    private readonly ICollarDeviceCredentialRepository _credRepo = Substitute.For<ICollarDeviceCredentialRepository>();
    private readonly ICollarAuditRepository _auditRepo = Substitute.For<ICollarAuditRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private const string Serial = "PT-A3F9-0001234";

    [Fact]
    public async Task RegisterCollarTag_HappyPath_LogsSerialRegistered()
    {
        _tagRepo.GetBySerialAsync(Serial, Arg.Any<CancellationToken>()).Returns((CollarTag?)null);
        var sut = new RegisterCollarTagCommandHandler(_tagRepo, _auditRepo, _uow);

        var result = await sut.Handle(new RegisterCollarTagCommand(Serial, "1.0.0"), default);

        result.IsSuccess.Should().BeTrue();
        await _auditRepo.Received(1).AddAsync(
            Arg.Is<CollarAuditEntry>(e => e.Event == CollarAuditEvent.SerialRegistered && e.Serial == Serial),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkCollarTagSold_HappyPath_LogsSerialMarkedSold()
    {
        var tag = CollarTag.CreateFromFactory(Serial, "1.0.0");
        _tagRepo.GetBySerialAsync(Serial, Arg.Any<CancellationToken>()).Returns(tag);
        var sut = new MarkCollarTagSoldCommandHandler(_tagRepo, _auditRepo, _uow);

        var result = await sut.Handle(new MarkCollarTagSoldCommand(Serial), default);

        result.IsSuccess.Should().BeTrue();
        await _auditRepo.Received(1).AddAsync(
            Arg.Is<CollarAuditEntry>(e => e.Event == CollarAuditEvent.SerialMarkedSold),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeCollarCredential_HappyPath_LogsDeviceKeyRevoked()
    {
        var collarId = Guid.NewGuid();
        var tag = CollarTag.CreateFromFactory(Serial, "1.0.0");
        tag.Activate(collarId);
        typeof(CollarTag).GetProperty("CollarId")!.SetValue(tag, collarId);
        var cred = CollarDeviceCredential.Create(collarId, "hash");

        _tagRepo.GetBySerialAsync(Serial, Arg.Any<CancellationToken>()).Returns(tag);
        _credRepo.GetForCollarAsync(collarId, Arg.Any<CancellationToken>())
            .Returns(new[] { cred } as IReadOnlyList<CollarDeviceCredential>);
        var sut = new RevokeCollarCredentialCommandHandler(_tagRepo, _credRepo, _auditRepo, _uow);

        var result = await sut.Handle(new RevokeCollarCredentialCommand(Serial), default);

        result.IsSuccess.Should().BeTrue();
        cred.IsRevoked.Should().BeTrue();
        await _auditRepo.Received(1).AddAsync(
            Arg.Is<CollarAuditEntry>(e => e.Event == CollarAuditEvent.DeviceKeyRevoked),
            Arg.Any<CancellationToken>());
    }
}
