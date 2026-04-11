using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Incentives.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Incentives.Queries.GetMyScore;

public sealed record GetMyScoreQuery(Guid UserId) : IRequest<Result<ContributorScoreDto?>>;

public sealed class GetMyScoreQueryHandler(IContributorScoreRepository repo)
    : IRequestHandler<GetMyScoreQuery, Result<ContributorScoreDto?>>
{
    public async Task<Result<ContributorScoreDto?>> Handle(
        GetMyScoreQuery request, CancellationToken cancellationToken)
    {
        var score = await repo.GetByUserIdAsync(request.UserId, cancellationToken);
        // Returning null value is valid: user simply has no reunifications yet.
        return Result.Success<ContributorScoreDto?>(
            score is null ? null : ContributorScoreDto.FromDomain(score));
    }
}
