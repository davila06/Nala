using FluentAssertions;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars;

public sealed class CollarLostModeDomainTests
{
    private static Collar MakeCollar() =>
        Collar.Register(Guid.NewGuid(), Guid.NewGuid(), CollarProvider.Own, null);

    [Fact]
    public void ActivateLostMode_SetsIsLostAndLinksEvent()
    {
        var collar = MakeCollar();
        var lostPetEventId = Guid.NewGuid();

        collar.ActivateLostMode(lostPetEventId);

        collar.IsLost.Should().BeTrue();
        collar.LostPetEventId.Should().Be(lostPetEventId);
        collar.LostModeActivatedAt.Should().NotBeNull();
    }

    [Fact]
    public void DeactivateLostMode_ClearsAllLostModeFields()
    {
        var collar = MakeCollar();
        collar.ActivateLostMode(Guid.NewGuid());

        collar.DeactivateLostMode();

        collar.IsLost.Should().BeFalse();
        collar.LostPetEventId.Should().BeNull();
        collar.LostModeActivatedAt.Should().BeNull();
    }
}
