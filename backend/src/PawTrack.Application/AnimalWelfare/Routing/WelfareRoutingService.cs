using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.AnimalWelfare;
using PawTrack.Domain.Allies;
using PawTrack.Domain.Municipalities;

namespace PawTrack.Application.AnimalWelfare.Routing;

public sealed record WelfareRoutingCandidateDto(
    Guid UserId,
    WelfareReferralRecipientType RecipientType,
    string OrganizationName,
    string CoverageLabel,
    int? DistanceMetres);

public sealed class WelfareRoutingService(
    IAllyProfileRepository allyProfileRepository,
    IMunicipalProfileRepository municipalProfileRepository)
{
    public async Task<IReadOnlyList<WelfareRoutingCandidateDto>> GetCandidatesAsync(
        string canton,
        double? lat,
        double? lng,
        CancellationToken cancellationToken = default)
    {
        var candidates = new List<WelfareRoutingCandidateDto>();

        if (lat.HasValue && lng.HasValue)
        {
            var allies = await allyProfileRepository.GetVerifiedCoveringPointAsync(lat.Value, lng.Value, cancellationToken);
            candidates.AddRange(allies
                .Where(ally => ally.VerificationStatus == AllyVerificationStatus.Verified)
                .Select(ally => new WelfareRoutingCandidateDto(
                    ally.UserId,
                    WelfareReferralRecipientType.Ally,
                    ally.OrganizationName,
                    ally.CoverageLabel,
                    (int)Math.Round(GeoHelper.DistanceMetres(lat.Value, lng.Value, ally.CoverageLat, ally.CoverageLng))))
                .Where(candidate => candidate.DistanceMetres.HasValue)
                .OrderBy(candidate => candidate.DistanceMetres));
        }

        var normalizedCanton = canton.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedCanton))
        {
            var municipalities = await municipalProfileRepository.GetAllActiveAsync(cancellationToken);
            candidates.AddRange(municipalities
                .Where(profile => profile.IsActive && !profile.IsExpired &&
                    profile.AllCantons.Contains(normalizedCanton, StringComparer.OrdinalIgnoreCase))
                .Select(profile => new WelfareRoutingCandidateDto(
                    profile.UserId,
                    WelfareReferralRecipientType.Municipality,
                    profile.OrgName,
                    string.Join(", ", profile.AllCantons),
                    null))
                .OrderBy(candidate => candidate.OrganizationName, StringComparer.OrdinalIgnoreCase));
        }

        return candidates.AsReadOnly();
    }

    public async Task<WelfareRoutingCandidateDto?> GetSuggestedCandidateAsync(
        string canton,
        double? lat,
        double? lng,
        CancellationToken cancellationToken = default)
    {
        var candidates = await GetCandidatesAsync(canton, lat, lng, cancellationToken);
        return candidates.FirstOrDefault(candidate => candidate.RecipientType == WelfareReferralRecipientType.Ally)
            ?? candidates.FirstOrDefault(candidate => candidate.RecipientType == WelfareReferralRecipientType.Municipality);
    }

    public async Task<bool> IsEligibleRecipientAsync(
        string canton,
        double? lat,
        double? lng,
        Guid recipientUserId,
        WelfareReferralRecipientType recipientType,
        CancellationToken cancellationToken = default)
    {
        var candidates = await GetCandidatesAsync(canton, lat, lng, cancellationToken);
        return candidates.Any(candidate => candidate.UserId == recipientUserId && candidate.RecipientType == recipientType);
    }
}
