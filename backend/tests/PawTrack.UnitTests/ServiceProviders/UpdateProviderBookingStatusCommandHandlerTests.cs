using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;
using PawTrack.Domain.Notifications;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class UpdateProviderBookingStatusCommandHandlerTests
{
    [Fact]
    public async Task Handle_ProviderConfirmsBooking_NotifiesCustomer()
    {
        var providers = Substitute.For<IServiceProviderRepository>();
        var audit = Substitute.For<IAuditLogRepository>();
        var notifications = Substitute.For<INotificationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var providerOwnerId = Guid.NewGuid();
        var provider = ServiceProvider.Create(providerOwnerId, "Grooming CR", "Cuidado", ServiceProviderCategory.Groomer, "Heredia", 10m, -84m, "provider@example.cr");
        var booking = ProviderBooking.Request(provider.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Bano", DateTimeOffset.UtcNow.AddDays(2), 60, 20_000m, 1, null);
        providers.GetBookingByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        providers.GetByUserIdAsync(providerOwnerId, Arg.Any<CancellationToken>()).Returns(provider);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new UpdateProviderBookingStatusCommandHandler(providers, audit, unitOfWork, notifications);
        var result = await handler.Handle(new UpdateProviderBookingStatusCommand(providerOwnerId, booking.Id, ProviderBookingStatus.Confirmed, null), default);

        result.IsSuccess.Should().BeTrue();
        await notifications.Received(1).AddAsync(
            Arg.Is<Notification>(notification => notification.UserId == booking.CustomerUserId && notification.Type == NotificationType.ProviderBookingUpdate),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CustomerCancelsBooking_NotifiesProvider()
    {
        var providers = Substitute.For<IServiceProviderRepository>();
        var audit = Substitute.For<IAuditLogRepository>();
        var notifications = Substitute.For<INotificationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var providerOwnerId = Guid.NewGuid();
        var customerUserId = Guid.NewGuid();
        var provider = ServiceProvider.Create(providerOwnerId, "Grooming CR", "Cuidado", ServiceProviderCategory.Groomer, "Heredia", 10m, -84m, "provider@example.cr");
        var booking = ProviderBooking.Request(provider.Id, Guid.NewGuid(), customerUserId, Guid.NewGuid(), "Bano", DateTimeOffset.UtcNow.AddDays(2), 60, 20_000m, 1, null);
        providers.GetBookingByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        providers.GetByUserIdAsync(customerUserId, Arg.Any<CancellationToken>()).Returns((ServiceProvider?)null);
        providers.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new UpdateProviderBookingStatusCommandHandler(providers, audit, unitOfWork, notifications);
        var result = await handler.Handle(new UpdateProviderBookingStatusCommand(customerUserId, booking.Id, ProviderBookingStatus.CancelledByCustomer, "Cambio de planes"), default);

        result.IsSuccess.Should().BeTrue();
        await notifications.Received(1).AddAsync(
            Arg.Is<Notification>(notification => notification.UserId == providerOwnerId && notification.Type == NotificationType.ProviderBookingUpdate),
            Arg.Any<CancellationToken>());
    }
}