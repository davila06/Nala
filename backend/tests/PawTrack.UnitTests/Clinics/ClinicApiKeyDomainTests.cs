using FluentAssertions;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicApiKeyDomainTests
{
    [Fact]
    public void Create_DefaultLifetime_ExpiresOneYearOut()
    {
        var key = ClinicApiKey.Create(Guid.NewGuid(), "hash", "Integración HIS");

        key.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddYears(1), TimeSpan.FromMinutes(1));
        key.IsExpired.Should().BeFalse();
        key.IsUsable.Should().BeTrue();
    }

    [Fact]
    public void Create_CustomLifetime_RespectsIt()
    {
        var key = ClinicApiKey.Create(Guid.NewGuid(), "hash", "Corta duración", TimeSpan.FromDays(1));

        key.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(1), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void IsExpired_PastExpiresAt_ReturnsTrue()
    {
        var key = ClinicApiKey.Create(Guid.NewGuid(), "hash", "Vencida", TimeSpan.FromMilliseconds(1));
        Thread.Sleep(10);

        key.IsExpired.Should().BeTrue();
        key.IsUsable.Should().BeFalse();
    }

    [Fact]
    public void Revoke_SetsIsRevoked_AndIsUsableFalse()
    {
        var key = ClinicApiKey.Create(Guid.NewGuid(), "hash", "Label");
        key.Revoke();

        key.IsRevoked.Should().BeTrue();
        key.IsUsable.Should().BeFalse();
    }

    [Fact]
    public void MarkRotatedTo_RevokesAndLinksToNewKey()
    {
        var oldKey = ClinicApiKey.Create(Guid.NewGuid(), "hash-old", "Label");
        var newKeyId = Guid.NewGuid();

        oldKey.MarkRotatedTo(newKeyId);

        oldKey.IsRevoked.Should().BeTrue();
        oldKey.RotatedToKeyId.Should().Be(newKeyId);
    }

    [Fact]
    public void HasScope_OnlyAllowsGrantedOperations()
    {
        var key = ClinicApiKey.Create(
            Guid.NewGuid(),
            "hash",
            "HIS read",
            scopes: [ClinicApiScope.MedicalRead, ClinicApiScope.Certificates]);

        key.HasScope(ClinicApiScope.MedicalRead).Should().BeTrue();
        key.HasScope(ClinicApiScope.MedicalWrite).Should().BeFalse();
        key.HasScope(ClinicApiScope.Certificates).Should().BeTrue();
    }

    [Fact]
    public void MedicalReadKey_DoesNotGrantMedicalExport()
    {
        var key = ClinicApiKey.Create(
            Guid.NewGuid(), "hash", "Read-only HIS",
            scopes: [ClinicApiScope.MedicalRead]);

        key.HasScope(ClinicApiScope.MedicalRead).Should().BeTrue();
        key.HasScope(ClinicApiScope.MedicalExport).Should().BeFalse();
    }
}
