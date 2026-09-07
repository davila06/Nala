using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.Pets;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class CreateProviderBookingCommandHandlerTests
{
    [Fact]
    public async Task Handle_PetBelongsToAnotherUser_ReturnsFailureWithoutCreatingBooking()
    {
        var providers = Substitute.For<IServiceProviderRepository>();
        var pets = Substitute.For<IPetRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var customerUserId = Guid.NewGuid();
        var pet = Pet.Create(Guid.NewGuid(), "Luna", PetSpecies.Dog, null, null);
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);

        var handler = new CreateProviderBookingCommandHandler(providers, pets, unitOfWork);
        var result = await handler.Handle(new CreateProviderBookingCommand(
            customerUserId, Guid.NewGuid(), pet.Id, DateTimeOffset.UtcNow.AddDays(2), 1, null), default);

        result.IsFailure.Should().BeTrue();
        await providers.DidNotReceive().AddBookingAsync(Arg.Any<ProviderBooking>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AtomicCapacityCheckRejectsFullSlot()
    {
        var providers = Substitute.For<IServiceProviderRepository>();
        var pets = Substitute.For<IPetRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var customerUserId = Guid.NewGuid();
        var provider = ServiceProvider.Create(
            Guid.NewGuid(), "Escuela Canina", "Adiestramiento", ServiceProviderCategory.Trainer,
            "San Jose", 9.9m, -84m, "provider@example.cr");
        provider.Activate();
        var service = ProviderService.Create(
            provider.Id, "Sesion", "Individual", ServiceModality.AtProviderLocation, 60, 25_000m, 1);
        var pet = Pet.Create(customerUserId, "Luna", PetSpecies.Dog, null, null);
        var startsAt = new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.FromHours(-6));
        var rule = ServiceAvailabilityRule.Create(service.Id, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0));

        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        providers.GetServiceByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        providers.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);
        providers.GetActiveAvailabilityRulesAsync(service.Id, Arg.Any<CancellationToken>()).Returns([rule]);
        providers.TryAddBookingAsync(Arg.Any<ProviderBooking>(), service.Capacity, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new CreateProviderBookingCommandHandler(providers, pets, unitOfWork);
        var result = await handler.Handle(new CreateProviderBookingCommand(
            customerUserId, service.Id, pet.Id, startsAt, 1, null), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Contains("capacidad"));
    }
}