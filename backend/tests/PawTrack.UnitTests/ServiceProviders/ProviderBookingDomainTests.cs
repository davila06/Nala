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
    public void Request_SnapshotsCommercialTerms()
    {
        var booking = ProviderBooking.Request(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sesion",
            DateTimeOffset.UtcNow.AddDays(2), 60, 25_000m, 2, null,
            taxCrc: 3_250m, platformFeeCrc: 1_000m, cancellationPolicySnapshot: "48h-Standard");

        booking.SubtotalCrc.Should().Be(50_000m);
        booking.TotalCrc.Should().Be(54_250m);
        booking.CancellationPolicySnapshot.Should().Be("48h-Standard");
    }

    [Fact]
    public void CancelByCustomer_InsideFreeWindow_Succeeds()
    {
        var startsAt = DateTimeOffset.UtcNow.AddHours(72);
        var booking = ProviderBooking.Request(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sesion",
            startsAt, 60, 25_000m, 1, null);

        booking.CancelByCustomer("Cambio de planes", DateTimeOffset.UtcNow);

        booking.Status.Should().Be(ProviderBookingStatus.CancelledByCustomer);
    }

    [Fact]
    public void CancelByCustomer_OutsideFreeWindow_IsRejected()
    {
        var startsAt = DateTimeOffset.UtcNow.AddHours(24);
        var booking = ProviderBooking.Request(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sesion",
            startsAt, 60, 25_000m, 1, null);

        var action = () => booking.CancelByCustomer("Cambio de planes", DateTimeOffset.UtcNow);

        action.Should().Throw<InvalidOperationException>();
        booking.Status.Should().Be(ProviderBookingStatus.Requested);
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

    [Fact]
    public void MarkAwaitingPayment_AndDisputed_TransitionsBookingToCommercialHold()
    {
        var booking = ProviderBooking.Request(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sesion",
            DateTimeOffset.UtcNow.AddDays(2), 60, 25_000m, 1, null);

        booking.MarkAwaitingPayment();
        booking.Status.Should().Be(ProviderBookingStatus.AwaitingPayment);

        booking.MarkDisputed("Pago reportado pero no verificado.");

        booking.Status.Should().Be(ProviderBookingStatus.Disputed);
        booking.CancellationReason.Should().Be("Pago reportado pero no verificado.");
    }

    [Fact]
    public void IssueRefund_TransitionsBookingToRefunded()
    {
        var booking = ProviderBooking.Request(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sesion",
            DateTimeOffset.UtcNow.AddDays(2), 60, 25_000m, 1, null);

        booking.Confirm();
        booking.MarkDisputed("No se pudo cumplir el servicio.");
        booking.IssueRefund("Reembolso por incumplimiento del proveedor.");

        booking.Status.Should().Be(ProviderBookingStatus.Refunded);
        booking.CancellationReason.Should().Be("Reembolso por incumplimiento del proveedor.");
    }

    [Fact]
    public void Expire_AwaitingPaymentBooking_ReleasesTheSlot()
    {
        var booking = ProviderBooking.Request(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sesion",
            DateTimeOffset.UtcNow.AddDays(2), 60, 25_000m, 1, null);
        booking.MarkAwaitingPayment();

        booking.Expire();

        booking.Status.Should().Be(ProviderBookingStatus.Expired);
    }
}