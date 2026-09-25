namespace PawTrack.Domain.Clinics;

public enum ClinicStaffRole { Veterinarian, Receptionist, Assistant, ReadOnly }

public enum ClinicStaffPermission { ViewAgenda, ManageAgenda, ReadMedical, WriteMedical, ManageInventory, ViewCrm, ManageCrm }

public sealed class ClinicStaffMembership
{
    private ClinicStaffMembership() { }

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid UserId { get; private set; }
    public ClinicStaffRole Role { get; private set; }
    public Guid? VeterinarianId { get; private set; }
    public bool IsRevoked { get; private set; }
    public Guid GrantedByUserId { get; private set; }
    public DateTimeOffset GrantedAt { get; private set; }
    public Guid? RevokedByUserId { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public static ClinicStaffMembership Grant(Guid clinicId, Guid userId, ClinicStaffRole role, Guid grantedByUserId, Guid? veterinarianId = null)
    {
        if (clinicId == Guid.Empty || userId == Guid.Empty || grantedByUserId == Guid.Empty)
            throw new ArgumentException("Clinic, user and grantor are required.");
        if (role == ClinicStaffRole.Veterinarian && (!veterinarianId.HasValue || veterinarianId.Value == Guid.Empty))
            throw new ArgumentException("Registered veterinarian required.", nameof(veterinarianId));
        if (role != ClinicStaffRole.Veterinarian && veterinarianId.HasValue)
            throw new ArgumentException("Only veterinarians can be linked to a registration.", nameof(veterinarianId));
        return new ClinicStaffMembership
        {
            Id = Guid.CreateVersion7(), ClinicId = clinicId, UserId = userId, Role = role, VeterinarianId = veterinarianId,
            GrantedByUserId = grantedByUserId, GrantedAt = DateTimeOffset.UtcNow,
        };
    }

    public bool Allows(ClinicStaffPermission permission) => !IsRevoked && Role switch
    {
        ClinicStaffRole.Veterinarian => permission is ClinicStaffPermission.ViewAgenda or ClinicStaffPermission.ManageAgenda
            or ClinicStaffPermission.ReadMedical or ClinicStaffPermission.WriteMedical or ClinicStaffPermission.ViewCrm,
        ClinicStaffRole.Receptionist => permission is ClinicStaffPermission.ViewAgenda or ClinicStaffPermission.ManageAgenda
            or ClinicStaffPermission.ViewCrm or ClinicStaffPermission.ManageCrm,
        ClinicStaffRole.Assistant => permission is ClinicStaffPermission.ViewAgenda or ClinicStaffPermission.ReadMedical
            or ClinicStaffPermission.ManageInventory,
        ClinicStaffRole.ReadOnly => permission is ClinicStaffPermission.ViewAgenda,
        _ => false,
    };

    public void ChangeRole(ClinicStaffRole role, Guid? veterinarianId)
    {
        if (IsRevoked) throw new InvalidOperationException("Membership is revoked.");
        if (role == ClinicStaffRole.Veterinarian && (!veterinarianId.HasValue || veterinarianId.Value == Guid.Empty))
            throw new ArgumentException("Registered veterinarian required.", nameof(veterinarianId));
        if (role != ClinicStaffRole.Veterinarian && veterinarianId.HasValue)
            throw new ArgumentException("Only veterinarians can be linked to a registration.", nameof(veterinarianId));
        Role = role;
        VeterinarianId = veterinarianId;
    }

    public void Revoke(Guid revokedByUserId)
    {
        if (IsRevoked) throw new InvalidOperationException("Membership already revoked.");
        if (revokedByUserId == Guid.Empty) throw new ArgumentException("Revoker is required.", nameof(revokedByUserId));
        IsRevoked = true;
        RevokedByUserId = revokedByUserId;
        RevokedAt = DateTimeOffset.UtcNow;
    }
}