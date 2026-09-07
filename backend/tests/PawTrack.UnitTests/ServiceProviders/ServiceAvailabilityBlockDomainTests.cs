using FluentAssertions;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class ServiceAvailabilityBlockDomainTests
{
    [Fact]
    public void Blocks_OverlappingSlot_ReturnsTrue()
    {
        var block = ServiceAvailabilityBlock.Create(
            Guid.NewGuid(),
            new DateTimeOffset(2026, 9, 8, 9, 0, 0, TimeSpan.FromHours(-6)),
            new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.FromHours(-6)),
            "Capacitacion");

        block.Blocks(
            new DateTimeOffset(2026, 9, 8, 10, 0, 0, TimeSpan.FromHours(-6)),
            new DateTimeOffset(2026, 9, 8, 11, 0, 0, TimeSpan.FromHours(-6))).Should().BeTrue();
        block.Blocks(
            new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.FromHours(-6)),
            new DateTimeOffset(2026, 9, 8, 13, 0, 0, TimeSpan.FromHours(-6))).Should().BeFalse();
    }
}