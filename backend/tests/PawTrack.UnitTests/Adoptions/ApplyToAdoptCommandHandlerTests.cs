using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.Application.Adoptions;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Adoptions;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Adoptions;

public sealed class ApplyToAdoptCommandHandlerTests
{
    private readonly IAdoptionRepository _adoptions = Substitute.For<IAdoptionRepository>();
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private ApplyToAdoptCommandHandler BuildHandler() => new(
        _adoptions, _dispatcher, _uow, NullLogger<ApplyToAdoptCommandHandler>.Instance);

    private static AdoptablePet MakeAnimal() =>
        AdoptablePet.Create(Guid.NewGuid(), "Max", PetSpecies.Dog,
            PetSize.Medium, AgeCategory.Young, "Juguetón", 9.93, -84.08, "San José");

    [Fact]
    public async Task Handle_AnimalNotFound_ReturnsFailure()
    {
        _adoptions.GetAnimalByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((AdoptablePet?)null);

        var result = await BuildHandler().Handle(
            new ApplyToAdoptCommand(Guid.NewGuid(), Guid.NewGuid(), "Quiero adoptarlo"), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(ApplyToAdoptCommandHandler.AnimalNotFoundError);
    }

    [Fact]
    public async Task Handle_AnimalNotAvailable_ReturnsFailure()
    {
        var animal = MakeAnimal();
        animal.MarkInProcess();
        _adoptions.GetAnimalByIdAsync(animal.Id, Arg.Any<CancellationToken>()).Returns(animal);

        var result = await BuildHandler().Handle(
            new ApplyToAdoptCommand(Guid.NewGuid(), animal.Id, "Quiero adoptarlo"), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(ApplyToAdoptCommandHandler.AnimalNotAvailableError);
    }

    [Fact]
    public async Task Handle_DuplicatePendingApplication_ReturnsFailure()
    {
        var animal = MakeAnimal();
        var applicantId = Guid.NewGuid();
        var existing = AdoptionApplication.Create(animal.Id, applicantId, "Ya apliqué antes");

        _adoptions.GetAnimalByIdAsync(animal.Id, Arg.Any<CancellationToken>()).Returns(animal);
        _adoptions.GetApplicationByApplicantAndAnimalAsync(applicantId, animal.Id, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await BuildHandler().Handle(
            new ApplyToAdoptCommand(applicantId, animal.Id, "Intento de duplicado"), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(ApplyToAdoptCommandHandler.DuplicateApplicationError);
    }

    [Fact]
    public async Task Handle_ValidApplication_SavesAndReturnsDto()
    {
        var animal = MakeAnimal();
        _adoptions.GetAnimalByIdAsync(animal.Id, Arg.Any<CancellationToken>()).Returns(animal);
        _adoptions.GetApplicationByApplicantAndAnimalAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((AdoptionApplication?)null);

        var result = await BuildHandler().Handle(
            new ApplyToAdoptCommand(Guid.NewGuid(), animal.Id, "Tengo experiencia con perros"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Pending");
        result.Value.ApplicantNote.Should().Be("Tengo experiencia con perros");
        await _adoptions.Received(1).AddApplicationAsync(
            Arg.Any<AdoptionApplication>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
