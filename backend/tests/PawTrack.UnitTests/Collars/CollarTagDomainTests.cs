using FluentAssertions;
using PawTrack.Application.Collars.DTOs;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars;

public sealed class CollarTagDomainTests
{
    // ── CollarTag state machine ───────────────────────────────────────────────

    [Fact]
    public void CreateFromFactory_ValidSerial_CreatesUnactivatedTag()
    {
        var tag = CollarTag.CreateFromFactory("PT-A3F9-0001234", "1.0.0");

        tag.Serial.Should().Be("PT-A3F9-0001234");
        tag.Status.Should().Be(CollarTagStatus.Unactivated);
        tag.CollarId.Should().BeNull();
        tag.IsAvailable.Should().BeTrue();
    }

    [Theory]
    [InlineData("PT-XXXX")]           // too short
    [InlineData("XX-A3F9-0001234")]   // wrong prefix
    [InlineData("PT-GGG0-0001234")]   // G is not hex
    [InlineData("PT-A3F9-000123")]    // 6 digits instead of 7
    public void CreateFromFactory_InvalidSerial_Throws(string bad)
    {
        var act = () => CollarTag.CreateFromFactory(bad, "1.0.0");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Activate_FromUnactivated_SetsActivatedStatus()
    {
        var tag = CollarTag.CreateFromFactory("PT-B2C1-0000001", "1.0.0");
        var collarId = Guid.NewGuid();

        tag.Activate(collarId);

        tag.Status.Should().Be(CollarTagStatus.Activated);
        tag.CollarId.Should().Be(collarId);
        tag.ActivatedAt.Should().NotBeNull();
        tag.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void Activate_AlreadyActivated_Throws()
    {
        var tag = CollarTag.CreateFromFactory("PT-B2C1-0000002", "1.0.0");
        tag.Activate(Guid.NewGuid());

        var act = () => tag.Activate(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Deactivate_FromActivated_ReturnsToUnactivated()
    {
        var tag = CollarTag.CreateFromFactory("PT-B2C1-0000003", "1.0.0");
        tag.Activate(Guid.NewGuid());

        tag.Deactivate();

        tag.Status.Should().Be(CollarTagStatus.Unactivated);
        tag.CollarId.Should().BeNull();
        tag.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_WhenNotActivated_Throws()
    {
        var tag = CollarTag.CreateFromFactory("PT-B2C1-0000004", "1.0.0");
        var act = () => tag.Deactivate();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkSold_WhenUnactivated_SetsSoldAt()
    {
        var tag = CollarTag.CreateFromFactory("PT-B2C1-0000005", "1.0.0");
        tag.MarkSold();
        tag.SoldAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkSold_WhenAlreadyActivated_Throws()
    {
        var tag = CollarTag.CreateFromFactory("PT-B2C1-0000006", "1.0.0");
        tag.Activate(Guid.NewGuid());
        var act = () => tag.MarkSold();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UpdateLastPing_SetsLastPingAt()
    {
        var tag = CollarTag.CreateFromFactory("PT-B2C1-0000007", "1.0.0");
        tag.UpdateLastPing();
        tag.LastPingAt.Should().NotBeNull();
    }

    // ── CollarDeviceCredential ────────────────────────────────────────────────

    [Fact]
    public void Create_SetsFreshCredential()
    {
        var collarId = Guid.NewGuid();
        var cred = CollarDeviceCredential.Create(collarId, "somehash");

        cred.CollarId.Should().Be(collarId);
        cred.KeyHash.Should().Be("somehash");
        cred.IsRevoked.Should().BeFalse();
        cred.IsUsable.Should().BeTrue();
    }

    [Fact]
    public void Revoke_SetsRevokedAt()
    {
        var cred = CollarDeviceCredential.Create(Guid.NewGuid(), "h");
        cred.Revoke();
        cred.IsRevoked.Should().BeTrue();
        cred.IsUsable.Should().BeFalse();
        cred.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public void RecordUsage_SetsLastUsedAt()
    {
        var cred = CollarDeviceCredential.Create(Guid.NewGuid(), "h");
        cred.RecordUsage();
        cred.LastUsedAt.Should().NotBeNull();
    }

    // ── CollarDto ─────────────────────────────────────────────────────────────

    [Fact]
    public void CollarDto_FromDomain_MapsCollarTagSerial()
    {
        var collar = Collar.Register(Guid.NewGuid(), Guid.NewGuid(), CollarProvider.Own, null);
        collar.SetTagSerial("PT-A3F9-0001234");

        var dto = CollarDto.FromDomain(collar);

        dto.CollarTagSerial.Should().Be("PT-A3F9-0001234");
    }

    [Fact]
    public void CollarDto_FromDomain_CollarTagSerial_NullWhenNotSet()
    {
        var collar = Collar.Register(Guid.NewGuid(), Guid.NewGuid(), CollarProvider.Tractive, "ext-id");

        var dto = CollarDto.FromDomain(collar);

        dto.CollarTagSerial.Should().BeNull();
    }
}
