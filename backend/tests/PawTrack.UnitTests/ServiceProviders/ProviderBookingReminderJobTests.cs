using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.Common;
using PawTrack.Domain.Notifications;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class ProviderBookingReminderJobTests
{
    [Fact]
    public async Task ExecuteAsync_ConfirmedBookingTomorrow_CreatesOneReminder()
    {
        var providers = Substitute.For<IServiceProviderRepository>();
        var notifications = Substitute.For<INotificationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var booking = ProviderBooking.Request(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sesion",
            DateTimeOffset.UtcNow.AddHours(20), 60, 25_000m, 1, null);
        booking.Confirm();
        providers.GetConfirmedBookingsStartingBetweenAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([booking]);
        notifications.HasRecentByUserTypeAndEntityAsync(
            booking.CustomerUserId, NotificationType.ProviderBookingReminder, booking.Id.ToString(),
            Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(false);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var job = new ProviderBookingReminderJob(
            providers, notifications, unitOfWork, NullLogger<ProviderBookingReminderJob>.Instance);
        await job.ExecuteAsync(default);

        await notifications.Received(1).AddAsync(
            Arg.Is<Notification>(notification =>
                notification.UserId == booking.CustomerUserId &&
                notification.Type == NotificationType.ProviderBookingReminder &&
                notification.RelatedEntityId == booking.Id.ToString()),
            Arg.Any<CancellationToken>());
    }
}