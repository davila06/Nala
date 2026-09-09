using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;
using PawTrack.Domain.Notifications;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class RescheduleProviderBookingCommandHandlerTests
{
    [Fact]
    public async Task Handle_OwnersRequestedBookingWithAvailableSlot_ReschedulesAndNotifiesProvider()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var audit = Substitute.For<IAuditLogRepository>();
        var notifications = Substitute.For<INotificationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var customerUserId = Guid.NewGuid();
        var provider = ServiceProvider.Create(Guid.NewGuid(), "Grooming", "Cuidado", ServiceProviderCategory.Groomer, "Heredia", 10m, -84m, "provider@example.cr");
        var service = ProviderService.Create(provider.Id, "Bano", "Bano", ServiceModality.AtProviderLocation, 60, 20_000m, 1);
        var booking = ProviderBooking.Request(provider.Id, service.Id, customerUserId, Guid.NewGuid(), service.Name, DateTimeOffset.UtcNow.AddDays(2), 60, 20_000m, 1, null);
        var newStartsAt = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(2).AddHours(10), TimeSpan.Zero);
        repository.GetBookingByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        repository.GetServiceByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        repository.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);
        repository.GetActiveAvailabilityRulesAsync(service.Id, Arg.Any<CancellationToken>()).Returns([ServiceAvailabilityRule.Create(service.Id, newStartsAt.DayOfWeek, new TimeOnly(9, 0), new TimeOnly(17, 0))]);
        repository.GetAvailabilityBlocksByServiceRangeAsync(service.Id, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([]);
        repository.TryRescheduleBookingAsync(booking, newStartsAt, service.DurationMinutes, service.Capacity, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                booking.Reschedule(newStartsAt, service.DurationMinutes);
                return true;
            });
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new RescheduleProviderBookingCommandHandler(repository, audit, unitOfWork, notifications);
        var result = await handler.Handle(new RescheduleProviderBookingCommand(customerUserId, booking.Id, newStartsAt), default);

        result.IsSuccess.Should().BeTrue();
        booking.StartsAt.Should().Be(newStartsAt);
        await notifications.Received(1).AddAsync(
            Arg.Is<Notification>(notification => notification.UserId == provider.UserId && notification.Type == NotificationType.ProviderBookingUpdate),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnrelatedCustomerCannotRescheduleBooking()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var audit = Substitute.For<IAuditLogRepository>();
        var notifications = Substitute.For<INotificationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var customerUserId = Guid.NewGuid();
        var attackerUserId = Guid.NewGuid();
        var provider = ServiceProvider.Create(Guid.NewGuid(), "Grooming", "Cuidado", ServiceProviderCategory.Groomer, "Heredia", 10m, -84m, "provider@example.cr");
        var service = ProviderService.Create(provider.Id, "Bano", "Bano", ServiceModality.AtProviderLocation, 60, 20_000m, 1);
        var booking = ProviderBooking.Request(provider.Id, service.Id, customerUserId, Guid.NewGuid(), service.Name, DateTimeOffset.UtcNow.AddDays(2), 60, 20_000m, 1, null);
        repository.GetBookingByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);

        var handler = new RescheduleProviderBookingCommandHandler(repository, audit, unitOfWork, notifications);
        var result = await handler.Handle(new RescheduleProviderBookingCommand(
            attackerUserId, booking.Id, DateTimeOffset.UtcNow.AddDays(4)), default);

        result.IsFailure.Should().BeTrue();
        booking.StartsAt.Should().NotBe(DateTimeOffset.UtcNow.AddDays(4));
        await repository.DidNotReceive().TryRescheduleBookingAsync(
            Arg.Any<ProviderBooking>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}