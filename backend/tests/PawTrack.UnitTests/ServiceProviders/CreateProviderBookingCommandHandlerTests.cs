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
    public async Task Handle_ProviderOnFreeTier_ReturnsFailureWithoutCreatingBooking()
    {
        var providers = Substitute.For<IServiceProviderRepository>();
        var pets = Substitute.For<IPetRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var customerUserId = Guid.NewGuid();
        var provider = ServiceProvider.Create(
            Guid.NewGuid(), "Escuela Canina", "Adiestramiento", ServiceProviderCategory.Trainer,
            "San Jose", 9.9m, -84m, "provider@example.cr");
        provider.Activate();
        provider.SetMembership(ProviderMembershipTier.Free, manual: true); // trial ended / never upgraded
        var service = ProviderService.Create(
            provider.Id, "Sesion", "Individual", ServiceModality.AtProviderLocation, 60, 25_000m, 1);
        var pet = Pet.Create(customerUserId, "Luna", PetSpecies.Dog, null, null);

        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        providers.GetServiceByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        providers.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);

        var handler = new CreateProviderBookingCommandHandler(providers, pets, unitOfWork);
        var result = await handler.Handle(new CreateProviderBookingCommand(
            customerUserId, service.Id, pet.Id, DateTimeOffset.UtcNow.AddDays(2), 1, null), default);

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

    [Fact]
    public async Task Handle_CapturesCommercialTermsInBookingSnapshot()
    {
        var providers = Substitute.For<IServiceProviderRepository>();
        var pets = Substitute.For<IPetRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var customerUserId = Guid.NewGuid();
        var provider = ServiceProvider.Create(Guid.NewGuid(), "Escuela", "Servicio", ServiceProviderCategory.Trainer, "San Jose", 9m, -84m, "provider@example.cr");
        provider.Activate();
        var service = ProviderService.Create(provider.Id, "Sesion", "Individual", ServiceModality.AtProviderLocation, 60, 25_000m, 1);
        var pet = Pet.Create(customerUserId, "Luna", PetSpecies.Dog, null, null);
        var startsAt = new DateTimeOffset(2026, 9, 9, 10, 0, 0, TimeSpan.FromHours(-6));
        var rule = ServiceAvailabilityRule.Create(service.Id, startsAt.DayOfWeek, new TimeOnly(0, 0), new TimeOnly(23, 59));

        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        providers.GetServiceByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        providers.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);
        providers.GetActiveAvailabilityRulesAsync(service.Id, Arg.Any<CancellationToken>()).Returns([rule]);
        providers.TryAddBookingAsync(Arg.Any<ProviderBooking>(), service.Capacity, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new CreateProviderBookingCommandHandler(providers, pets, unitOfWork);
        var result = await handler.Handle(new CreateProviderBookingCommand(
            customerUserId, service.Id, pet.Id, startsAt, 1, null,
            TaxCrc: 3_250m, PlatformFeeCrc: 1_250m,
            CancellationPolicySnapshot: "Cancelacion gratuita hasta 24h"), default);

        result.IsSuccess.Should().BeTrue(string.Join("; ", result.Errors));
        result.Value!.SubtotalCrc.Should().Be(25_000m);
        result.Value.TaxCrc.Should().Be(3_250m);
        result.Value.PlatformFeeCrc.Should().Be(1_250m);
        result.Value.TotalCrc.Should().Be(29_500m);
        result.Value.CancellationPolicySnapshot.Should().Be("Cancelacion gratuita hasta 24h");
    }
}