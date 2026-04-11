using PawTrack.Domain.Allies;

namespace PawTrack.Application.Allies.DTOs;

public sealed record AllyProfileDto(
    string UserId,
    string OrganizationName,
    string AllyType,
    string CoverageLabel,
    double CoverageLat,
    double CoverageLng,
    int CoverageRadiusMetres,
    string VerificationStatus,
    DateTimeOffset AppliedAt,
    DateTimeOffset? VerifiedAt)
{
    public static AllyProfileDto FromDomain(AllyProfile profile) => new(
        profile.UserId.ToString(),
        profile.OrganizationName,
        profile.AllyType.ToString(),
        profile.CoverageLabel,
        profile.CoverageLat,
        profile.CoverageLng,
        profile.CoverageRadiusMetres,
        profile.VerificationStatus.ToString(),
        profile.AppliedAt,
        profile.VerifiedAt);
}