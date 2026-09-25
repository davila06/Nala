using FluentAssertions;
using PawTrack.Infrastructure.Clinics;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicBusinessDateTests
{
    [Fact]
    public void CostaRicaBusinessDay_BeginsAtSixUtcAndEndsAtNextSixUtc()
    {
        var (start, end) = ClinicBusinessDate.UtcRange(new DateOnly(2026, 9, 25));

        start.Should().Be(new DateTimeOffset(2026, 9, 25, 6, 0, 0, TimeSpan.Zero));
        end.Should().Be(new DateTimeOffset(2026, 9, 26, 6, 0, 0, TimeSpan.Zero));
    }
}
