using FluentAssertions;
using PawTrack.Domain.Certificates;

namespace PawTrack.UnitTests.Certificates.Domain;

public sealed class ClinicVeterinarianAvailabilityTests
{
    [Fact]
    public void Availability_RejectsOverlappingAppointments()
    {
        var veterinarianId = Guid.NewGuid();
        var first = VeterinarianAppointment.Schedule(
            Guid.NewGuid(), veterinarianId, Guid.NewGuid(),
            new DateTimeOffset(2026, 9, 10, 10, 0, 0, TimeSpan.FromHours(-6)),
            TimeSpan.FromMinutes(30));
        var overlap = VeterinarianAppointment.Schedule(
            Guid.NewGuid(), veterinarianId, Guid.NewGuid(),
            first.StartsAt.AddMinutes(15), TimeSpan.FromMinutes(30));

        VeterinarianAppointment.Overlaps(first, overlap).Should().BeTrue();
    }
}