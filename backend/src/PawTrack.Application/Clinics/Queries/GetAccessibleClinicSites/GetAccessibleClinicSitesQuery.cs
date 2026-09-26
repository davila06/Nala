using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Queries.GetAccessibleClinicSites;

public sealed record GetAccessibleClinicSitesQuery(Guid UserId)
    : IRequest<Result<IReadOnlyList<AccessibleClinicSiteReadModel>>>;

public sealed class GetAccessibleClinicSitesQueryHandler(IClinicSiteAccessRepository siteAccessRepository)
    : IRequestHandler<GetAccessibleClinicSitesQuery, Result<IReadOnlyList<AccessibleClinicSiteReadModel>>>
{
    public async Task<Result<IReadOnlyList<AccessibleClinicSiteReadModel>>> Handle(
        GetAccessibleClinicSitesQuery request,
        CancellationToken cancellationToken)
    {
        return Result.Success(await siteAccessRepository.ListAccessibleSitesAsync(
            request.UserId,
            cancellationToken));
    }
}
