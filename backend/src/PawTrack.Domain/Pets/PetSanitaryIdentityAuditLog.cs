namespace PawTrack.Domain.Pets;

public enum PetSanitaryIdentityAuditAction
{
    SanitaryIdentityUpdated,
    MicrochipDeclared,
    MicrochipVerified,
    MicrochipConflictFlagged,
    MicrochipVerificationRevoked,
    SterilizationUpdated,
    ResidenceCantonUpdated,
}

public sealed class PetSanitaryIdentityAuditLog
{
    private PetSanitaryIdentityAuditLog() { } // EF Core

    public Guid Id { get; private set; }
    public Guid PetId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public Guid? ActorClinicId { get; private set; }
    public PetSanitaryIdentityAuditAction Action { get; private set; }
    public string FieldName { get; private set; } = string.Empty;
    public string? PreviousValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static PetSanitaryIdentityAuditLog Create(
        Guid petId,
        Guid actorUserId,
        Guid? actorClinicId,
        PetSanitaryIdentityAuditAction action,
        string fieldName,
        string? previousValue,
        string? newValue,
        string? reason = null)
    {
        if (petId == Guid.Empty) throw new ArgumentException("PetId is required.", nameof(petId));
        if (actorUserId == Guid.Empty) throw new ArgumentException("ActorUserId is required.", nameof(actorUserId));
        if (string.IsNullOrWhiteSpace(fieldName)) throw new ArgumentException("FieldName is required.", nameof(fieldName));

        return new PetSanitaryIdentityAuditLog
        {
            Id = Guid.CreateVersion7(),
            PetId = petId,
            ActorUserId = actorUserId,
            ActorClinicId = actorClinicId,
            Action = action,
            FieldName = fieldName.Trim(),
            PreviousValue = Normalize(previousValue),
            NewValue = Normalize(newValue),
            Reason = Normalize(reason),
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
