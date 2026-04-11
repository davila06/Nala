using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Locations.Commands.UpdateUserLocation;

/// <summary>
/// Upserts the caller's last known location and notification opt-in preference.
/// Idempotent — safe to call on every significant position change.
/// </summary>
public sealed record UpdateUserLocationCommand(
    Guid UserId,
    double Lat,
    double Lng,
    bool ReceiveNearbyAlerts,
    /// <summary>Quiet-hours window start in Costa Rica local time (UTC-6). Null = no quiet window.</summary>
    TimeOnly? QuietHoursStart,
    /// <summary>Quiet-hours window end in Costa Rica local time (UTC-6). Null = no quiet window.</summary>
    TimeOnly? QuietHoursEnd) : IRequest<Result<bool>>;
