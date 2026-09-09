using FluentAssertions;
using PawTrack.Domain.Medical;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicMedicalAccessGrantDomainTests
{
    [Fact]
    public void AcceptedGrant_EnforcesPermissions()
    {
        var (grant, code) = ClinicMedicalAccessGrant.Generate(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Owner",
            permissions: [ClinicMedicalAccessPermission.Read]);

        grant.TryAccept(code).Should().BeTrue();
        grant.HasPermission(ClinicMedicalAccessPermission.Read).Should().BeTrue();
        grant.HasPermission(ClinicMedicalAccessPermission.Write).Should().BeFalse();
    }

    [Fact]
    public void AcceptedGrant_ExpiresAtAccessDeadline()
    {
        var (grant, code) = ClinicMedicalAccessGrant.Generate(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Owner",
            accessExpiresAt: DateTimeOffset.UtcNow.AddMilliseconds(-1));

        grant.TryAccept(code).Should().BeTrue();
        grant.IsEffectivelyActive.Should().BeFalse();
    }
}