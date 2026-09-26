namespace PawTrack.Domain.Clinics;

public sealed class ClinicOrganizationSiteAccess
{
    private ClinicOrganizationSiteAccess() { }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid GrantedByUserId { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTimeOffset GrantedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public static ClinicOrganizationSiteAccess Grant(
        Guid organizationId,
        Guid clinicId,
        Guid userId,
        Guid grantedByUserId)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization ID is required.", nameof(organizationId));
        if (clinicId == Guid.Empty) throw new ArgumentException("Clinic ID is required.", nameof(clinicId));
        if (userId == Guid.Empty) throw new ArgumentException("User ID is required.", nameof(userId));
        if (grantedByUserId == Guid.Empty) throw new ArgumentException("Granting user ID is required.", nameof(grantedByUserId));

        return new ClinicOrganizationSiteAccess
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            ClinicId = clinicId,
            UserId = userId,
            GrantedByUserId = grantedByUserId,
            GrantedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Revoke()
    {
        if (IsRevoked) return;
        IsRevoked = true;
        RevokedAt = DateTimeOffset.UtcNow;
    }
}
