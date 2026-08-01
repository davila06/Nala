using FluentAssertions;
using PawTrack.Domain.Municipalities;

namespace PawTrack.UnitTests.Municipalities.Domain;

public sealed class CapturedAnimalDomainTests
{
    [Fact]
    public void Record_ValidInputs_SetsReceivedStatus()
    {
        var a = CapturedAnimal.Record(Guid.NewGuid(), "Desamparados", "Perro", "Negro");

        a.Status.Should().Be(CapturedAnimalStatus.Received);
        a.Canton.Should().Be("Desamparados");
        a.MatchedPetId.Should().BeNull();
    }

    [Fact]
    public void UpdateStatus_ToOwnerFound_SucceedsFromReceived()
    {
        var a = CapturedAnimal.Record(Guid.NewGuid(), "Curridabat", "Gato", "Gris");
        a.UpdateStatus(CapturedAnimalStatus.OwnerFound);
        a.Status.Should().Be(CapturedAnimalStatus.OwnerFound);
    }

    [Fact]
    public void LinkToPet_SetsPetId()
    {
        var petId = Guid.NewGuid();
        var a = CapturedAnimal.Record(Guid.NewGuid(), "Curridabat", "Gato", "Gris");
        a.LinkToPet(petId);
        a.MatchedPetId.Should().Be(petId);
    }

    [Fact]
    public void SetPhotoUrl_SetsUrl()
    {
        var a = CapturedAnimal.Record(Guid.NewGuid(), "Cartago", "Perro", "Café");
        a.SetPhotoUrl("https://storage/photo.jpg");
        a.PhotoUrl.Should().Be("https://storage/photo.jpg");
    }

    [Fact]
    public void UpdateDetails_OverwritesNotesAndChip()
    {
        var a = CapturedAnimal.Record(Guid.NewGuid(), "Heredia", "Perro", "Blanco", notes: "Manso");
        a.UpdateDetails("Agresivo", "985000000000001");
        a.Notes.Should().Be("Agresivo");
        a.CollarChipNumber.Should().Be("985000000000001");
    }
}
