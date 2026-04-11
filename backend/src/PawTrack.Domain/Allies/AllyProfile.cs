namespace PawTrack.Domain.Allies;

public sealed class AllyProfile
{
    private AllyProfile() { } // EF Core

    public Guid UserId { get; private set; }
    public string OrganizationName { get; private set; } = string.Empty;
    public AllyType AllyType { get; private set; }
    public string CoverageLabel { get; private set; } = string.Empty;
    public double CoverageLat { get; private set; }
    public double CoverageLng { get; private set; }
    public int CoverageRadiusMetres { get; private set; }
    public AllyVerificationStatus VerificationStatus { get; private set; }
    public DateTimeOffset AppliedAt { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }

    public static AllyProfile Create(
        Guid userId,
        string organizationName,
        AllyType allyType,
        string coverageLabel,
        double coverageLat,
        double coverageLng,
        int coverageRadiusMetres)
    {
        return new AllyProfile
        {
            UserId = userId,
            OrganizationName = organizationName.Trim(),
            AllyType = allyType,
            CoverageLabel = coverageLabel.Trim(),
            CoverageLat = coverageLat,
            CoverageLng = coverageLng,
            CoverageRadiusMetres = coverageRadiusMetres,
            VerificationStatus = AllyVerificationStatus.Pending,
            AppliedAt = DateTimeOffset.UtcNow,
            VerifiedAt = null,
        };
    }

    public void Resubmit(
        string organizationName,
        AllyType allyType,
        string coverageLabel,
        double coverageLat,
        double coverageLng,
        int coverageRadiusMetres)
    {
        OrganizationName = organizationName.Trim();
        AllyType = allyType;
        CoverageLabel = coverageLabel.Trim();
        CoverageLat = coverageLat;
        CoverageLng = coverageLng;
        CoverageRadiusMetres = coverageRadiusMetres;
        VerificationStatus = AllyVerificationStatus.Pending;
        AppliedAt = DateTimeOffset.UtcNow;
        VerifiedAt = null;
    }

    public void Approve()
    {
        VerificationStatus = AllyVerificationStatus.Verified;
        VerifiedAt = DateTimeOffset.UtcNow;
    }

    public void Reject()
    {
        VerificationStatus = AllyVerificationStatus.Rejected;
        VerifiedAt = null;
    }
}