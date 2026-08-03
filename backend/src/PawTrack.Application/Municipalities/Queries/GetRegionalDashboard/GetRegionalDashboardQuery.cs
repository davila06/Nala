using MediatR;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Municipalities;

namespace PawTrack.Application.Municipalities.Queries.GetRegionalDashboard;

// ── Query — Red Regional only ─────────────────────────────────────────────────

public sealed record GetRegionalDashboardQuery(
    Guid RequestingUserId) : IRequest<Result<RegionalDashboardDto>>;

public sealed record RegionalDashboardDto(
    IReadOnlyList<string> Cantons,
    IReadOnlyList<CantonSummaryDto> Summary,
    int RegionalTotal,
    double RegionalRecoveryRate);

public sealed record CantonSummaryDto(
    string Canton,
    int Total,
    int Active,
    int OwnerFound,
    double RecoveryRate);

public sealed class GetRegionalDashboardQueryHandler(
    ICapturedAnimalRepository repository,
    IMunicipalSubscriptionService subscriptionService)
    : IRequestHandler<GetRegionalDashboardQuery, Result<RegionalDashboardDto>>
{
    public async Task<Result<RegionalDashboardDto>> Handle(
        GetRegionalDashboardQuery request, CancellationToken ct)
    {
        if (!await subscriptionService.IsRedRegionalAsync(request.RequestingUserId, ct))
            return Result.Failure<RegionalDashboardDto>("El dashboard regional requiere el plan Red Regional.");

        var cantons = await subscriptionService.GetAuthorizedCantonsAsync(request.RequestingUserId, ct);
        var summaries = new List<CantonSummaryDto>();

        int regionalTotal = 0, regionalOwnerFound = 0;

        foreach (var canton in cantons)
        {
            var (animals, total) = await repository.SearchAsync(canton, null, 1, int.MaxValue, ct);
            var ownerFound = animals.Count(a => a.Status == CapturedAnimalStatus.OwnerFound);
            var active = animals.Count(a => a.Status == CapturedAnimalStatus.Received);
            var rate = total == 0 ? 0.0 : Math.Round((double)ownerFound / total * 100, 1);

            summaries.Add(new CantonSummaryDto(canton, total, active, ownerFound, rate));
            regionalTotal += total;
            regionalOwnerFound += ownerFound;
        }

        var regionalRate = regionalTotal == 0
            ? 0.0
            : Math.Round((double)regionalOwnerFound / regionalTotal * 100, 1);

        return Result.Success(new RegionalDashboardDto(cantons, summaries, regionalTotal, regionalRate));
    }
}
