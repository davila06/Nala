using PawTrack.Domain.Safety;

namespace PawTrack.Application.Common.Interfaces;

public interface IAnonymousContactRequestRepository
{
    Task AddAsync(AnonymousContactRequest request, CancellationToken cancellationToken = default);
}