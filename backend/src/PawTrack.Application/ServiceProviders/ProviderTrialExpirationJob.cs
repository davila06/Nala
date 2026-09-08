using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Application.ServiceProviders;

public sealed class ProviderTrialExpirationJob(IServiceProviderRepository repository, IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var providers = await repository.GetProvidersWithExpiredTrialAsync(now, 500, ct);
        foreach (var provider in providers)
        {
            if (provider.ExpireTrialIfDue(now)) repository.Update(provider);
        }
        if (providers.Count > 0) await unitOfWork.SaveChangesAsync(ct);
    }
}
