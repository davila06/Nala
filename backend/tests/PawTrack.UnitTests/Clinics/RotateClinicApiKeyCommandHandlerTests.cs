using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Clinics.Commands.ManageApiKey;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Clinics;

public sealed class RotateClinicApiKeyCommandHandlerTests
{
    private readonly IClinicRepository _clinics = Substitute.For<IClinicRepository>();
    private readonly IClinicApiKeyRepository _keys = Substitute.For<IClinicApiKeyRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private RotateClinicApiKeyCommandHandler BuildHandler() => new(_clinics, _keys, _uow);

    private static Clinic MakeClinic(Guid userId) =>
        Clinic.Create(userId, "VetSalud", "SEN-123", "Heredia", 10m, -84.1m, "vet@x.com");

    [Fact]
    public async Task Handle_AccessDenied_WhenClinicNotOwnedByRequester()
    {
        var userId = Guid.NewGuid();
        var clinic = MakeClinic(Guid.NewGuid()); // different owner
        _clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);

        var result = await BuildHandler().Handle(
            new RotateClinicApiKeyCommand(Guid.NewGuid(), clinic.Id, userId), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_KeyNotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var clinic = MakeClinic(userId);
        _clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        _keys.GetForClinicAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(new List<ClinicApiKey>());

        var result = await BuildHandler().Handle(
            new RotateClinicApiKeyCommand(Guid.NewGuid(), clinic.Id, userId), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidKey_RevokesOldAndIssuesNewWithSameLabel()
    {
        var userId = Guid.NewGuid();
        var clinic = MakeClinic(userId);
        var oldKey = ClinicApiKey.Create(clinic.Id, "old-hash", "Integración HIS");

        _clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        _keys.GetForClinicAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(new List<ClinicApiKey> { oldKey });

        var result = await BuildHandler().Handle(
            new RotateClinicApiKeyCommand(oldKey.Id, clinic.Id, userId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Label.Should().Be("Integración HIS");
        result.Value.RawKey.Should().NotBeNullOrEmpty();
        oldKey.IsRevoked.Should().BeTrue();
        oldKey.RotatedToKeyId.Should().Be(result.Value.Id);
        await _keys.Received(1).AddAsync(Arg.Any<ClinicApiKey>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyRevokedKey_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var clinic = MakeClinic(userId);
        var revokedKey = ClinicApiKey.Create(clinic.Id, "hash", "Label");
        revokedKey.Revoke();

        _clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        _keys.GetForClinicAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(new List<ClinicApiKey> { revokedKey });

        var result = await BuildHandler().Handle(
            new RotateClinicApiKeyCommand(revokedKey.Id, clinic.Id, userId), default);

        result.IsFailure.Should().BeTrue();
    }
}
