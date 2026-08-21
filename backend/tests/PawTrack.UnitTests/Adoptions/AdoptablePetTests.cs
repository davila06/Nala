using FluentAssertions;
using PawTrack.Domain.Adoptions;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Adoptions;

public sealed class AdoptablePetTests
{
    private static AdoptablePet Make() =>
        AdoptablePet.Create(Guid.NewGuid(), "Max", PetSpecies.Dog,
            PetSize.Medium, AgeCategory.Young, "Es muy juguetón",
            9.93, -84.08, "San José");

    [Fact]
    public void NewAnimal_HasAvailableStatus() =>
        Make().Status.Should().Be(AdoptionStatus.Available);

    [Fact]
    public void NewAnimal_PhotoUrls_IsEmpty() =>
        Make().PhotoUrls.Should().BeEmpty();

    [Fact]
    public void AddPhoto_UpToLimit_Succeeds()
    {
        var animal = Make();
        for (var i = 0; i < 5; i++) animal.AddPhoto($"https://blob/photo{i}.jpg");
        animal.PhotoUrls.Should().HaveCount(5);
    }

    [Fact]
    public void AddPhoto_BeyondLimit_Throws()
    {
        var animal = Make();
        for (var i = 0; i < 5; i++) animal.AddPhoto($"https://blob/photo{i}.jpg");
        var act = () => animal.AddPhoto("https://blob/extra.jpg");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RemovePhoto_ExistingUrl_RemovesIt()
    {
        var animal = Make();
        animal.AddPhoto("https://blob/photo0.jpg");
        animal.RemovePhoto("https://blob/photo0.jpg");
        animal.PhotoUrls.Should().BeEmpty();
    }

    [Fact]
    public void MarkInProcess_SetsStatus()
    {
        var animal = Make();
        animal.MarkInProcess();
        animal.Status.Should().Be(AdoptionStatus.InProcess);
        animal.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAdopted_SetsStatusAndTimestamp()
    {
        var animal = Make();
        animal.MarkInProcess();
        animal.MarkAdopted();
        animal.Status.Should().Be(AdoptionStatus.Adopted);
        animal.AdoptedAt.Should().NotBeNull();
    }

    [Fact]
    public void Pause_SetsStatus()
    {
        var animal = Make();
        animal.Pause();
        animal.Status.Should().Be(AdoptionStatus.Paused);
    }

    [Fact]
    public void Republish_FromPaused_SetsAvailable()
    {
        var animal = Make();
        animal.Pause();
        animal.Republish();
        animal.Status.Should().Be(AdoptionStatus.Available);
    }

    [Fact]
    public void UpdateDetails_ChangesNameAndStory()
    {
        var animal = Make();
        animal.UpdateDetails("Luna", "Muy tranquila", null, null,
            true, true, false, true, false, true, false);
        animal.Name.Should().Be("Luna");
        animal.Story.Should().Be("Muy tranquila");
        animal.IsVaccinated.Should().BeTrue();
        animal.IsSterilized.Should().BeTrue();
        animal.UpdatedAt.Should().NotBeNull();
    }
}
