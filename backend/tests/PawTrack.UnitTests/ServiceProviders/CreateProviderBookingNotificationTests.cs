using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.Common;
using PawTrack.Domain.Notifications;
using PawTrack.Domain.Pets;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class CreateProviderBookingNotificationTests
{
    [Fact]
    public async Task Handle_ValidRequest_NotifiesProviderWithoutPetDetails()
    {
        var providers = Substitute.For<IServiceProviderRepository>();
        var pets = Substitute.For<IPetRepository>();
        var notifications = Substitute.For<INotificationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var customerId = Guid.NewGuid();
        var providerOwnerId = Guid.NewGuid();
        var provider = ServiceProvider.Create(providerOwnerId, "Grooming CR", "Cuidado", ServiceProviderCategory.Groomer, "Heredia", 10m, -84m, "provider@example.cr");
        provider.Activate();
        var service = ProviderService.Create(provider.Id, "Bano", "Bano", ServiceModality.AtProviderLocation, 60, 20_000m, 1);
        var pet = Pet.Create(customerId, "Luna", PetSpecies.Dog, null, null);
        var startsAt = new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.FromHours(-6));
        providers.GetServiceByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        providers.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);
        providers.GetActiveAvailabilityRulesAsync(service.Id, Arg.Any<CancellationToken>()).Returns([ServiceAvailabilityRule.Create(service.Id, startsAt.DayOfWeek, new TimeOnly(9, 0), new TimeOnly(17, 0))]);
        providers.TryAddBookingAsync(Arg.Any<ProviderBooking>(), 1, Arg.Any<CancellationToken>()).Returns(true);
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);

        var handler = new CreateProviderBookingCommandHandler(providers, pets, unitOfWork, notifications);
        var result = await handler.Handle(new CreateProviderBookingCommand(customerId, service.Id, pet.Id, startsAt, 1, null), default);

        result.IsSuccess.Should().BeTrue(string.Join("; ", result.Errors));
        await notifications.Received(1).AddAsync(
            Arg.Is<Notification>(notification => notification.UserId == providerOwnerId && notification.Type == NotificationType.ProviderBookingUpdate && !notification.Body.Contains("Luna")),
            Arg.Any<CancellationToken>());
    }
}