namespace PawTrack.Domain.Clinics;

public enum ClinicOrganizationRole
{
    Owner,
    Administrator,
    FinanceManager,
    Member,
}

public sealed class ClinicOrganizationMembership
{
    private ClinicOrganizationMembership() { }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public ClinicOrganizationRole Role { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTimeOffset GrantedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    internal static ClinicOrganizationMembership Create(
        Guid organizationId,
        Guid userId,
        ClinicOrganizationRole role)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization ID is required.", nameof(organizationId));
        if (userId == Guid.Empty) throw new ArgumentException("User ID is required.", nameof(userId));

        return new ClinicOrganizationMembership
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            UserId = userId,
            Role = role,
            GrantedAt = DateTimeOffset.UtcNow,
        };
    }

    internal void Revoke()
    {
        if (IsRevoked) return;
        IsRevoked = true;
        RevokedAt = DateTimeOffset.UtcNow;
    }
}
