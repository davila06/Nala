using MediatR;
using PawTrack.Application.AnimalWelfare.Interfaces;
using PawTrack.Application.AnimalWelfare.Routing;
using PawTrack.Domain.AnimalWelfare;
using PawTrack.Domain.Common;

namespace PawTrack.Application.AnimalWelfare.Queries;

public sealed record GetWelfareRoutingCandidatesQuery(Guid CaseId) : IRequest<Result<IReadOnlyList<WelfareRoutingCandidateDto>>>;

public sealed class GetWelfareRoutingCandidatesQueryHandler(
    IAnimalWelfareCaseRepository caseRepository,
    WelfareRoutingService routingService)
    : IRequestHandler<GetWelfareRoutingCandidatesQuery, Result<IReadOnlyList<WelfareRoutingCandidateDto>>>
{
    public async Task<Result<IReadOnlyList<WelfareRoutingCandidateDto>>> Handle(
        GetWelfareRoutingCandidatesQuery request,
        CancellationToken cancellationToken)
    {
        var welfareCase = await caseRepository.GetByIdAsync(request.CaseId, cancellationToken);
        if (welfareCase is null)
            return Result.Failure<IReadOnlyList<WelfareRoutingCandidateDto>>("Caso de bienestar no encontrado.");

        var candidates = await routingService.GetCandidatesAsync(
            welfareCase.Canton,
            welfareCase.ApproxLat,
            welfareCase.ApproxLng,
            cancellationToken);
        return Result.Success(candidates);
    }
}
