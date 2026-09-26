namespace PawTrack.Domain.Clinics;

public sealed class ClinicOrganizationSite
{
    private ClinicOrganizationSite() { }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid ClinicId { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }

    internal static ClinicOrganizationSite Create(Guid organizationId, Guid clinicId, bool isPrimary)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization ID is required.", nameof(organizationId));
        if (clinicId == Guid.Empty) throw new ArgumentException("Clinic ID is required.", nameof(clinicId));
        return new ClinicOrganizationSite
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            ClinicId = clinicId,
            IsPrimary = isPrimary,
            AddedAt = DateTimeOffset.UtcNow,
        };
    }

    internal void SetPrimary(bool value) => IsPrimary = value;
}
