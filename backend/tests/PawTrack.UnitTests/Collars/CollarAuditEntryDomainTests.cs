using FluentAssertions;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars;

public sealed class CollarAuditEntryDomainTests
{
    [Fact]
    public void Create_WithCollarId_Succeeds()
    {
        var entry = CollarAuditEntry.Create(
            CollarAuditEvent.Activated, "Vinculado", collarId: Guid.NewGuid());

        entry.Event.Should().Be(CollarAuditEvent.Activated);
        entry.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithSerialOnly_Succeeds()
    {
        var entry = CollarAuditEntry.Create(
            CollarAuditEvent.SerialRegistered, "Firmware 1.0.0", serial: "pt-a3f9-0001234");

        entry.Serial.Should().Be("PT-A3F9-0001234"); // normalized to uppercase
        entry.CollarId.Should().BeNull();
    }

    [Fact]
    public void Create_WithoutCollarIdOrSerial_Throws()
    {
        var act = () => CollarAuditEntry.Create(CollarAuditEvent.Activated, "details");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_TrimsDetails()
    {
        var entry = CollarAuditEntry.Create(
            CollarAuditEvent.Deactivated, "  algo de texto  ", collarId: Guid.NewGuid());

        entry.Details.Should().Be("algo de texto");
    }
}
