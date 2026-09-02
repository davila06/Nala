using MediatR;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Queries.GetCollarLocationHistoryRange;

public sealed record CollarLocationPointDto(double Lat, double Lng, int? Accuracy, DateTimeOffset RecordedAt);

/// <summary>
/// Owner-facing, collar-scoped location history over an explicit date range — used by the
/// history viewer, CSV export, and heatmap. Complements the older pet-scoped
/// <c>GetLocationHistoryQuery</c> (relative "last N hours" window used by the live GPS tab).
/// </summary>
public sealed record GetCollarLocationHistoryRangeQuery(
    Guid CollarId,
    Guid RequestingUserId,
    DateTimeOffset From,
    DateTimeOffset To,
    int MaxPoints = 2000) : IRequest<Result<IReadOnlyList<CollarLocationPointDto>>>;

public sealed class GetCollarLocationHistoryRangeQueryHandler(ICollarRepository collarRepository)
    : IRequestHandler<GetCollarLocationHistoryRangeQuery, Result<IReadOnlyList<CollarLocationPointDto>>>
{
    /// <summary>Matches CollarLocationPurgeJob's retention window — no raw data exists beyond this.</summary>
    private static readonly TimeSpan MaxLookback = TimeSpan.FromDays(30);

    public async Task<Result<IReadOnlyList<CollarLocationPointDto>>> Handle(
        GetCollarLocationHistoryRangeQuery request, CancellationToken cancellationToken)
    {
        var collar = await collarRepository.GetByIdAsync(request.CollarId, cancellationToken);
        if (collar is null)
            return Result.Failure<IReadOnlyList<CollarLocationPointDto>>("Collar no encontrado.");

        if (collar.OwnerId != request.RequestingUserId)
            return Result.Failure<IReadOnlyList<CollarLocationPointDto>>("Access denied.");

        var earliestAllowed = DateTimeOffset.UtcNow - MaxLookback;
        var from = request.From < earliestAllowed ? earliestAllowed : request.From;
        var to = request.To > DateTimeOffset.UtcNow ? DateTimeOffset.UtcNow : request.To;
        var maxPoints = Math.Clamp(request.MaxPoints, 1, 10_000);

        var history = await collarRepository.GetLocationHistoryRangeAsync(
            request.CollarId, from, to, maxPoints, cancellationToken);

        return Result.Success<IReadOnlyList<CollarLocationPointDto>>(
            history.Select(l => new CollarLocationPointDto(l.Lat, l.Lng, l.Accuracy, l.RecordedAt)).ToList());
    }
}
