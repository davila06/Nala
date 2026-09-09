using FluentAssertions;
using PawTrack.Domain.Certificates;

namespace PawTrack.UnitTests.Certificates.Domain;

public sealed class ClinicVeterinarianPermissionTests
{
    [Fact]
    public void AuthorizedVeterinarian_HasExplicitPermissions()
    {
        var veterinarian = ClinicVeterinarian.Create(Guid.NewGuid(), "Dra. Rivera", "VET-1");

        veterinarian.HasPermission(ClinicVeterinarianPermission.IssueCertificates).Should().BeTrue();
        veterinarian.HasPermission(ClinicVeterinarianPermission.WriteMedical).Should().BeTrue();
        veterinarian.HasPermission(ClinicVeterinarianPermission.ExportMedical).Should().BeFalse();
    }
}