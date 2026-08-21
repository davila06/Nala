using FluentAssertions;
using PawTrack.Domain.Adoptions;

namespace PawTrack.UnitTests.Adoptions;

public sealed class AdoptionFairTests
{
    private static AdoptionFair Make() =>
        AdoptionFair.Create(Guid.NewGuid(), "Feria de Adopción",
            "Parque La Sabana", 9.93, -84.08,
            startsAt: DateTimeOffset.UtcNow.AddDays(3),
            endsAt: DateTimeOffset.UtcNow.AddDays(3).AddHours(6));

    [Fact]
    public void Create_ValidDates_HasUpcomingStatus() =>
        Make().Status.Should().Be(FairStatus.Upcoming);

    [Fact]
    public void Create_EndsBeforeStarts_Throws()
    {
        var now = DateTimeOffset.UtcNow;
        var act = () => AdoptionFair.Create(Guid.NewGuid(), "Feria",
            "Parque", 9.93, -84.08,
            startsAt: now.AddDays(2), endsAt: now.AddDays(1));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_EndsEqualsStarts_Throws()
    {
        var now = DateTimeOffset.UtcNow.AddDays(2);
        var act = () => AdoptionFair.Create(Guid.NewGuid(), "Feria",
            "Parque", 9.93, -84.08, startsAt: now, endsAt: now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddAnimal_NoDuplicates()
    {
        var fair = Make();
        var id = Guid.NewGuid();
        fair.AddAnimal(id);
        fair.AddAnimal(id); // duplicate — should be ignored
        fair.AnimalIds.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveAnimal_RemovesFromList()
    {
        var fair = Make();
        var id = Guid.NewGuid();
        fair.AddAnimal(id);
        fair.RemoveAnimal(id);
        fair.AnimalIds.Should().BeEmpty();
    }

    [Fact]
    public void Activate_SetsStatus()
    {
        var fair = Make();
        fair.Activate();
        fair.Status.Should().Be(FairStatus.Active);
    }

    [Fact]
    public void Cancel_SetsStatus()
    {
        var fair = Make();
        fair.Cancel();
        fair.Status.Should().Be(FairStatus.Cancelled);
    }
}
