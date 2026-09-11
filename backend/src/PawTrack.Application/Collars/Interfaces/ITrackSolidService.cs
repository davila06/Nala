namespace PawTrack.Application.Collars.Interfaces;

public sealed record TrackSolidPosition(
    double Lat,
    double Lng,
    int? BatteryPercent,
    DateTimeOffset RecordedAt,
    int? AccuracyMeters = null,
    bool IsOnline = true);

public interface ITrackSolidService
{
    Task<TrackSolidPosition?> GetLatestPositionAsync(
        string imei,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, TrackSolidPosition>> GetBatchPositionsAsync(
        IEnumerable<string> imeis,
        CancellationToken cancellationToken = default);
}
