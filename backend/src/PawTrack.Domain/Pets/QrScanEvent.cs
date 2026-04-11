namespace PawTrack.Domain.Pets;

public sealed class QrScanEvent
{
    private QrScanEvent() { } // EF Core

    public Guid Id { get; private set; }
    public Guid PetId { get; private set; }
    public string? ScannedByUserId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? CountryCode { get; private set; }
    public string? CityName { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset ScannedAt { get; private set; }

    public static QrScanEvent Create(
        Guid petId,
        string? scannedByUserId,
        string? ipAddress,
        string? userAgent,
        string? countryCode,
        string? cityName,
        DateTimeOffset scannedAt)
    {
        return new QrScanEvent
        {
            Id = Guid.CreateVersion7(),
            PetId = petId,
            ScannedByUserId = string.IsNullOrWhiteSpace(scannedByUserId) ? null : scannedByUserId.Trim(),
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim(),
            CountryCode = string.IsNullOrWhiteSpace(countryCode) ? null : countryCode.Trim().ToUpperInvariant(),
            CityName = string.IsNullOrWhiteSpace(cityName) ? null : cityName.Trim(),
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim(),
            ScannedAt = scannedAt,
        };
    }
}
