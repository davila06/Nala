using PawTrack.Domain.Collars;

namespace PawTrack.Application.Collars.DTOs;

public sealed record CollarDto(
    Guid           Id,
    Guid           PetId,
    CollarProvider Provider,
    string?        ExternalDeviceId,
    int?           BatteryPercent,
    double?        LastLat,
    double?        LastLng,
    DateTimeOffset? LastSeenAt,
    bool           IsActive)
{
    public static CollarDto FromDomain(Collar c) => new(
        c.Id, c.PetId, c.Provider, c.ExternalDeviceId,
        c.BatteryPercent, c.LastLat, c.LastLng, c.LastSeenAt, c.IsActive);
}
