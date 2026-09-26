namespace PawTrack.Domain.Clinics;

public sealed class ClinicOrganization
{
    private readonly List<ClinicOrganizationMembership> _memberships = [];
    private readonly List<ClinicOrganizationSite> _sites = [];

    private ClinicOrganization() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyList<ClinicOrganizationMembership> Memberships => _memberships.AsReadOnly();
    public IReadOnlyList<ClinicOrganizationSite> Sites => _sites.AsReadOnly();

    public static ClinicOrganization Create(string name, Guid ownerUserId, Guid primaryClinicId)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Organization name is required.", nameof(name));
        if (ownerUserId == Guid.Empty) throw new ArgumentException("Owner user ID is required.", nameof(ownerUserId));
        if (primaryClinicId == Guid.Empty) throw new ArgumentException("Primary clinic ID is required.", nameof(primaryClinicId));

        var organization = new ClinicOrganization
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        organization._memberships.Add(ClinicOrganizationMembership.Create(
            organization.Id, ownerUserId, ClinicOrganizationRole.Owner));
        organization._sites.Add(ClinicOrganizationSite.Create(organization.Id, primaryClinicId, isPrimary: true));
        return organization;
    }

    public void AddSite(Guid clinicId, bool makePrimary = false)
    {
        if (clinicId == Guid.Empty) throw new ArgumentException("Clinic ID is required.", nameof(clinicId));
        if (_sites.Any(site => site.ClinicId == clinicId))
            throw new InvalidOperationException("Clinic is already assigned to this organization.");

        if (makePrimary)
            foreach (var site in _sites) site.SetPrimary(false);

        _sites.Add(ClinicOrganizationSite.Create(Id, clinicId, makePrimary));
    }

    public void AddMember(Guid userId, ClinicOrganizationRole role)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID is required.", nameof(userId));
        if (_memberships.Any(member => member.UserId == userId && !member.IsRevoked))
            throw new InvalidOperationException("User already has an active organization membership.");
        if (role == ClinicOrganizationRole.Owner
            && _memberships.Any(member => member.Role == ClinicOrganizationRole.Owner && !member.IsRevoked))
            throw new InvalidOperationException("Transfer organization ownership instead of adding another owner.");
        _memberships.Add(ClinicOrganizationMembership.Create(Id, userId, role));
    }

    public bool RevokeMember(Guid userId)
    {
        var membership = _memberships.FirstOrDefault(member => member.UserId == userId && !member.IsRevoked);
        if (membership is null) return false;
        if (membership.Role == ClinicOrganizationRole.Owner
            && !_memberships.Any(member => member.Role == ClinicOrganizationRole.Owner
                && !member.IsRevoked && member.UserId != userId))
            throw new InvalidOperationException("An organization must retain at least one active owner.");
        membership.Revoke();
        return true;
    }
}
