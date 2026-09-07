using FluentAssertions;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class ProviderBookingDomainTests
{
    [Fact]
    public void Request_CapturesServiceTermsAndCanBeCancelled()
    {
        var startsAt = new DateTimeOffset(2026, 9, 10, 14, 0, 0, TimeSpan.Zero);

        var booking = ProviderBooking.Request(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Sesion de obediencia",
            startsAt,
            60,
            25_000m,
            1,
            "Mi perro es reactivo con otros perros.");

        booking.Status.Should().Be(ProviderBookingStatus.Requested);
        booking.StartsAt.Should().Be(startsAt);
        booking.EndsAt.Should().Be(startsAt.AddMinutes(60));
        booking.PriceCrc.Should().Be(25_000m);
        booking.Quantity.Should().Be(1);

        booking.CancelByCustomer("Cambio de planes");

        booking.Status.Should().Be(ProviderBookingStatus.CancelledByCustomer);
        booking.CancellationReason.Should().Be("Cambio de planes");
    }

    [Fact]
    public void Expire_RequestTransitionsToExpired()
    {
        var booking = ProviderBooking.Request(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sesion",
            DateTimeOffset.UtcNow.AddDays(2), 60, 25_000m, 1, null);

        booking.Expire();

        booking.Status.Should().Be(ProviderBookingStatus.Expired);
    }

    [Fact]
    public void MarkNoShow_ConfirmedBooking_TransitionsToNoShow()
    {
        var booking = ProviderBooking.Request(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sesion",
            DateTimeOffset.UtcNow.AddDays(2), 60, 25_000m, 1, null);
        booking.Confirm();

        booking.MarkNoShow();

        booking.Status.Should().Be(ProviderBookingStatus.NoShow);
    }
}