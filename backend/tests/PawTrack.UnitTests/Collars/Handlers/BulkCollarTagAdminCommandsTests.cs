using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Collars.Commands.Admin;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars.Handlers;

public sealed class BulkCollarTagAdminCommandsTests
{
    private readonly ICollarTagRepository _tagRepo = Substitute.For<ICollarTagRepository>();
    private readonly ICollarDeviceCredentialRepository _credRepo = Substitute.For<ICollarDeviceCredentialRepository>();
    private readonly ICollarAuditRepository _auditRepo = Substitute.For<ICollarAuditRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task BulkMarkSold_MixOfValidAndMissing_ReportsBoth()
    {
        var tag = CollarTag.CreateFromFactory("PT-A1B1-0000001", "1.0.0");
        _tagRepo.GetBySerialAsync("PT-A1B1-0000001", Arg.Any<CancellationToken>()).Returns(tag);
        _tagRepo.GetBySerialAsync("PT-ZZZZ-0000000", Arg.Any<CancellationToken>()).Returns((CollarTag?)null);
        var sut = new BulkMarkCollarTagsSoldCommandHandler(_tagRepo, _auditRepo, _uow);

        var result = await sut.Handle(
            new BulkMarkCollarTagsSoldCommand(["PT-A1B1-0000001", "PT-ZZZZ-0000000"]), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Succeeded.Should().Be(1);
        result.Value.Failed.Should().Be(1);
        tag.SoldAt.Should().NotBeNull();
    }

    [Fact]
    public async Task BulkRevoke_ActivatedTagWithCredentials_RevokesAll()
    {
        var collarId = Guid.NewGuid();
        var tag = CollarTag.CreateFromFactory("PT-A1B1-0000002", "1.0.0");
        tag.Activate(collarId);
        typeof(CollarTag).GetProperty("CollarId")!.SetValue(tag, collarId);
        var cred = CollarDeviceCredential.Create(collarId, "hash");

        _tagRepo.GetBySerialAsync("PT-A1B1-0000002", Arg.Any<CancellationToken>()).Returns(tag);
        _credRepo.GetForCollarAsync(collarId, Arg.Any<CancellationToken>())
            .Returns(new[] { cred } as IReadOnlyList<CollarDeviceCredential>);
        var sut = new BulkRevokeCollarTagsCommandHandler(_tagRepo, _credRepo, _auditRepo, _uow);

        var result = await sut.Handle(
            new BulkRevokeCollarTagsCommand(["PT-A1B1-0000002"], "Robado"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Succeeded.Should().Be(1);
        cred.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task BulkRevoke_NotActivated_CountsAsFailed()
    {
        var tag = CollarTag.CreateFromFactory("PT-A1B1-0000003", "1.0.0");
        _tagRepo.GetBySerialAsync("PT-A1B1-0000003", Arg.Any<CancellationToken>()).Returns(tag);
        var sut = new BulkRevokeCollarTagsCommandHandler(_tagRepo, _credRepo, _auditRepo, _uow);

        var result = await sut.Handle(
            new BulkRevokeCollarTagsCommand(["PT-A1B1-0000003"], null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Failed.Should().Be(1);
    }
}
