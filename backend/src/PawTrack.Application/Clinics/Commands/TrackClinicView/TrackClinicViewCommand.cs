using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Clinics.Commands.TrackClinicView;

// ── Command (fire-and-forget — no response needed) ────────────────────────────

public sealed record TrackClinicViewCommand(
    Guid ClinicId,
    string Source,
    string? IpHash = null) : IRequest<Unit>;

public sealed class TrackClinicViewCommandHandler(
    IClinicProfileViewRepository viewRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<TrackClinicViewCommand, Unit>
{
    public async Task<Unit> Handle(TrackClinicViewCommand request, CancellationToken ct)
    {
        var view = ClinicProfileView.Record(request.ClinicId, request.Source, request.IpHash);
        await viewRepository.AddAsync(view, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ── Query ─────────────────────────────────────────────────────────────────────

public sealed record GetClinicVisibilityStatsQuery(
    Guid ClinicId, int Days = 30) : IRequest<Result<ClinicVisibilityStatsDto>>;

public sealed class GetClinicVisibilityStatsQueryHandler(
    IClinicProfileViewRepository viewRepository,
    ISubscriptionRepository subscriptionRepository)
    : IRequestHandler<GetClinicVisibilityStatsQuery, Result<ClinicVisibilityStatsDto>>
{
    public async Task<Result<ClinicVisibilityStatsDto>> Handle(
        GetClinicVisibilityStatsQuery request, CancellationToken ct)
    {
        var sub = await subscriptionRepository.GetActiveForClinicAsync(request.ClinicId, ct);
        if (sub is null || sub.Tier < SubscriptionTier.ClinicPlus)
            return Result.Failure<ClinicVisibilityStatsDto>(
                "Las métricas de visibilidad requieren el plan Clínica Plus o Partner.");

        var stats = await viewRepository.GetStatsAsync(request.ClinicId, request.Days, ct);
        return Result.Success(stats);
    }
}
