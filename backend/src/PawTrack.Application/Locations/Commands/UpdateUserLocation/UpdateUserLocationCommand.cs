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
    TimeOnly? QuietHoursStart,
    TimeOnly? QuietHoursEnd,
    string? TimeZoneId = null) : IRequest<Result<bool>>;
