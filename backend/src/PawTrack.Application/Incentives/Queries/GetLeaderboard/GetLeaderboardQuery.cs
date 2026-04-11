using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Incentives.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Incentives.Queries.GetLeaderboard;

public sealed record GetLeaderboardQuery(int Take = 10) : IRequest<Result<IReadOnlyList<ContributorScoreDto>>>;

public sealed class GetLeaderboardQueryHandler(IContributorScoreRepository repo)
    : IRequestHandler<GetLeaderboardQuery, Result<IReadOnlyList<ContributorScoreDto>>>
{
    public async Task<Result<IReadOnlyList<ContributorScoreDto>>> Handle(
        GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var clampedTake = Math.Clamp(request.Take, 1, 50);
        var entries = await repo.GetLeaderboardAsync(clampedTake, cancellationToken);
        IReadOnlyList<ContributorScoreDto> dtos = entries.Select(ContributorScoreDto.FromDomain).ToList();
        return Result.Success(dtos);
    }
}
