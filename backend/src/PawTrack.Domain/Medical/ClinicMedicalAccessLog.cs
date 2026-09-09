namespace PawTrack.Domain.Medical;

/// <summary>Immutable audit record — one row per clinic access to a pet's expediente.</summary>
public sealed class ClinicMedicalAccessLog
{
    private ClinicMedicalAccessLog() { }

    public Guid Id { get; private set; }
    public Guid PetId { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid AccessedByUserId { get; private set; }
    public string Operation { get; private set; } = string.Empty;
    public string Permission { get; private set; } = string.Empty;
    public string AccessMethod { get; private set; } = string.Empty;
    public string Outcome { get; private set; } = string.Empty;
    public string? Reason { get; private set; }
    public DateTimeOffset AccessedAt { get; private set; }

    public static ClinicMedicalAccessLog Create(
        Guid petId,
        Guid clinicId,
        Guid accessedByUserId,
        string operation = "read_medical_history",
        string permission = ClinicMedicalAccessPermission.Read,
        string accessMethod = "unknown",
        string outcome = "allowed",
        string? reason = null) => new()
        {
            Id = Guid.CreateVersion7(),
            PetId = petId,
            ClinicId = clinicId,
            AccessedByUserId = accessedByUserId,
            Operation = operation.Trim(),
            Permission = permission.Trim(),
            AccessMethod = accessMethod.Trim(),
            Outcome = outcome.Trim(),
            Reason = reason?.Trim(),
            AccessedAt = DateTimeOffset.UtcNow,
        };
}
