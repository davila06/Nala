using MediatR;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Municipalities;

namespace PawTrack.Application.Municipalities.Queries.GetCantonStatistics;

// ── Query — Full+ ─────────────────────────────────────────────────────────────

public sealed record GetCantonStatisticsQuery(
    Guid RequestingUserId,
    string? Canton = null) : IRequest<Result<CantonStatisticsDto>>;

public sealed record CantonStatisticsDto(
    string Canton,
    int TotalCaptured,
    int Received,
    int OwnerFound,
    int Transferred,
    int Released,
    int Adopted,
    double RecoveryRate,
    IReadOnlyList<DailyCountDto> Last30Days);

public sealed record DailyCountDto(DateOnly Date, int Count);

public sealed class GetCantonStatisticsQueryHandler(
    ICapturedAnimalRepository repository,
    IMunicipalSubscriptionService subscriptionService)
    : IRequestHandler<GetCantonStatisticsQuery, Result<CantonStatisticsDto>>
{
    public async Task<Result<CantonStatisticsDto>> Handle(
        GetCantonStatisticsQuery request, CancellationToken ct)
    {
        if (!await subscriptionService.IsFullOrAboveAsync(request.RequestingUserId, ct))
            return Result.Failure<CantonStatisticsDto>("Las estadísticas requieren el plan Full o Red Regional.");

        var cantons = await subscriptionService.GetAuthorizedCantonsAsync(request.RequestingUserId, ct);
        var canton = request.Canton ?? cantons[0];

        if (!cantons.Contains(canton, StringComparer.OrdinalIgnoreCase))
            return Result.Failure<CantonStatisticsDto>("No tienes acceso a las estadísticas de ese cantón.");

        var (all, _) = await repository.SearchAsync(canton, null, 1, int.MaxValue, ct);

        var total = all.Count;
        var received = all.Count(a => a.Status == CapturedAnimalStatus.Received);
        var ownerFound = all.Count(a => a.Status == CapturedAnimalStatus.OwnerFound);
        var transferred = all.Count(a => a.Status == CapturedAnimalStatus.Transferred);
        var released = all.Count(a => a.Status == CapturedAnimalStatus.Released);
        var adopted = all.Count(a => a.Status == CapturedAnimalStatus.Adopted);

        var recoveryRate = total == 0 ? 0.0 : Math.Round((double)ownerFound / total * 100, 1);

        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-29));
        var last30 = all
            .Where(a => DateOnly.FromDateTime(a.CapturedAt.Date) >= cutoff)
            .GroupBy(a => DateOnly.FromDateTime(a.CapturedAt.Date))
            .Select(g => new DailyCountDto(g.Key, g.Count()))
            .OrderBy(d => d.Date)
            .ToList();

        return Result.Success(new CantonStatisticsDto(
            canton, total, received, ownerFound, transferred, released, adopted,
            recoveryRate, last30));
    }
}
