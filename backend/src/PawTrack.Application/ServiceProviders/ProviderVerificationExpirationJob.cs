using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Application.ServiceProviders;

public sealed class ProviderVerificationExpirationJob(IServiceProviderRepository repository, IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var verifications = await repository.GetVerifiedVerificationsExpiredBeforeAsync(DateOnly.FromDateTime(DateTime.UtcNow), 500, ct);
        foreach (var verification in verifications)
        {
            verification.MarkExpired();
            repository.UpdateVerification(verification);
        }
        if (verifications.Count > 0) await unitOfWork.SaveChangesAsync(ct);
    }
}