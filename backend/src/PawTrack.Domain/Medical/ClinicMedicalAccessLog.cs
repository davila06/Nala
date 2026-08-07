namespace PawTrack.Domain.Medical;

/// <summary>Immutable audit record — one row per clinic access to a pet's expediente.</summary>
public sealed class ClinicMedicalAccessLog
{
    private ClinicMedicalAccessLog() { }

    public Guid Id { get; private set; }
    public Guid PetId { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid AccessedByUserId { get; private set; }
    public DateTimeOffset AccessedAt { get; private set; }

    public static ClinicMedicalAccessLog Create(Guid petId, Guid clinicId, Guid accessedByUserId) => new()
    {
        Id = Guid.CreateVersion7(),
        PetId = petId,
        ClinicId = clinicId,
        AccessedByUserId = accessedByUserId,
        AccessedAt = DateTimeOffset.UtcNow,
    };
}
