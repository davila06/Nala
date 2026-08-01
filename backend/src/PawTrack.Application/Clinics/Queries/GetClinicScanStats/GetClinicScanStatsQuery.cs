using FluentValidation;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Clinics.Queries.GetClinicScanStats;

public sealed record GetClinicScanStatsQuery(
    Guid ClinicId,
    Guid RequestingUserId,
    int Year,
    int Month) : IRequest<Result<ClinicScanStatsDto>>;

public sealed record ClinicDayStat(string Day, int Total, int Matched, int QrCount, int RfidCount);

public sealed record ClinicScanStatsDto(
    int Year,
    int Month,
    int TotalScans,
    int MatchedScans,
    int QrScans,
    int RfidScans,
    IReadOnlyList<ClinicDayStat> ByDay);

public sealed class GetClinicScanStatsQueryValidator : AbstractValidator<GetClinicScanStatsQuery>
{
    public GetClinicScanStatsQueryValidator()
    {
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.Year).GreaterThan(2020);
    }
}

public sealed class GetClinicScanStatsQueryHandler(
    IClinicScanRepository scanRepository,
    IClinicRepository clinicRepository,
    ISubscriptionRepository subscriptionRepository)
    : IRequestHandler<GetClinicScanStatsQuery, Result<ClinicScanStatsDto>>
{
    public async Task<Result<ClinicScanStatsDto>> Handle(
        GetClinicScanStatsQuery request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null)
            return Result.Failure<ClinicScanStatsDto>("Clinic not found.");
        if (clinic.UserId != request.RequestingUserId)
            return Result.Failure<ClinicScanStatsDto>("Access denied.");

        // Gate: ClinicPlus or ClinicPartner required
        var sub = await subscriptionRepository.GetActiveForClinicAsync(request.ClinicId, cancellationToken);
        if (sub is null || sub.Tier < SubscriptionTier.ClinicPlus)
            return Result.Failure<ClinicScanStatsDto>("Las estadísticas de escaneos requieren el plan Clínica Plus.");

        var raw = await scanRepository.GetMonthlyStatsAsync(
            request.ClinicId, request.Year, request.Month, cancellationToken);

        var byDay = raw.ByDay
            .Select(d => new ClinicDayStat(d.Day.ToString("yyyy-MM-dd"), d.Total, d.Matched, d.QrCount, d.RfidCount))
            .ToList()
            .AsReadOnly();

        return Result.Success(new ClinicScanStatsDto(
            request.Year, request.Month,
            raw.TotalScans, raw.MatchedScans,
            raw.QrScans, raw.RfidScans,
            byDay));
    }
}
