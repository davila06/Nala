namespace PawTrack.Domain.Clinics;

public sealed class ClinicScan
{
    private ClinicScan() { } // EF Core

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    /// <summary>Resolved pet Id; null when the input did not match any registered pet.</summary>
    public Guid? MatchedPetId { get; private set; }
    /// <summary>Raw QR URL or RFID chip identifier entered by the clinic operator.</summary>
    public string ScanInput { get; private set; } = string.Empty;
    public ScanInputType InputType { get; private set; }
    public DateTimeOffset ScannedAt { get; private set; }

    public static ClinicScan Create(
        Guid clinicId,
        string scanInput,
        ScanInputType inputType,
        Guid? matchedPetId)
    {
        return new ClinicScan
        {
            Id           = Guid.CreateVersion7(),
            ClinicId     = clinicId,
            ScanInput    = scanInput.Trim(),
            InputType    = inputType,
            MatchedPetId = matchedPetId,
            ScannedAt    = DateTimeOffset.UtcNow,
        };
    }
}
