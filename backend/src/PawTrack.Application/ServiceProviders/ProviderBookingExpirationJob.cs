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
        foreach (var booking in expiredBookings)
        {
            booking.Expire();
            repository.UpdateBooking(booking);
        }

        if (expiredBookings.Count > 0)
            await unitOfWork.SaveChangesAsync(ct);
    }
}