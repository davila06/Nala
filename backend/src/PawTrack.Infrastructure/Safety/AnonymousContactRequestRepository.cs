using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Safety;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Safety;

public sealed class AnonymousContactRequestRepository(PawTrackDbContext db)
    : IAnonymousContactRequestRepository
{
    public async Task AddAsync(AnonymousContactRequest request, CancellationToken cancellationToken = default) =>
        await db.AnonymousContactRequests.AddAsync(request, cancellationToken);
}