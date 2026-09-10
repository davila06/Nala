using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Adoptions;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Adoptions;
using PawTrack.Domain.Common;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Adoptions;

public sealed class SubmitOwnerAdoptionCommandHandlerTests
{
    [Fact]
    public async Task Handle_OwnedPetWithPhoto_CreatesPendingOwnerSubmission()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Luna", PetSpecies.Dog, "Mestiza", null);
        pet.SetPhoto("https://storage.example/luna.jpg");
        var pets = Substitute.For<IPetRepository>();
        var adoptions = Substitute.For<IAdoptionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        var handler = new SubmitOwnerAdoptionCommandHandler(pets, adoptions, unitOfWork);

        var result = await handler.Handle(
            new SubmitOwnerAdoptionCommand(ownerId, pet.Id, PetSize.Medium, AgeCategory.Adult,
                "Necesita un hogar responsable.", "Solo familias comprometidas.", 9.93, -84.08,
                "San Jose", true, true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("PendingReview");
        result.Value.Source.Should().Be("Owner");
        await adoptions.Received(1).AddAnimalAsync(
            Arg.Is<AdoptablePet>(animal => animal.Status == AdoptionStatus.PendingReview && animal.Source == AdoptionSource.Owner && animal.PhotoUrls.Count == 1),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}