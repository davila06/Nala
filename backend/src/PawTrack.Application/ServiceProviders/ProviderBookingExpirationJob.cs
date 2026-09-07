using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Application.ServiceProviders;

public sealed class ProviderBookingExpirationJob(
    IServiceProviderRepository repository,
    IUnitOfWork unitOfWork)
{
    private static readonly TimeSpan RequestLifetime = TimeSpan.FromHours(24);

    public async Task ExecuteAsync(CancellationToken ct)
    {
        var expiredBookings = await repository.GetRequestedBookingsCreatedBeforeAsync(
            DateTimeOffset.UtcNow - RequestLifetime, 500, ct);
        var paymentPendingBookings = await repository.GetPaymentPendingBookingsCreatedBeforeAsync(
            DateTimeOffset.UtcNow - RequestLifetime, 500, ct);
        var allExpiredBookings = expiredBookings.Concat(paymentPendingBookings).DistinctBy(booking => booking.Id).ToList();
        foreach (var booking in allExpiredBookings)
        {
            booking.Expire();
            repository.UpdateBooking(booking);
            var payment = await repository.GetPaymentByBookingAsync(booking.Id, ct);
            if (payment is not null)
            {
                payment.Expire("La reserva vencio sin confirmacion de pago.");
                repository.UpdatePayment(payment);
            }
        }

        if (allExpiredBookings.Count > 0)
            await unitOfWork.SaveChangesAsync(ct);
    }
}