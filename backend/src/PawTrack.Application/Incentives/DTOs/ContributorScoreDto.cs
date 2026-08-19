using PawTrack.Domain.Incentives;

namespace PawTrack.Application.Incentives.DTOs;

/// <summary>
/// Public projection for the leaderboard and "my score" endpoints.
/// <para>
/// <b>Privacy note:</b> <c>UserId</c> is intentionally NOT exposed — returning the
/// raw GUID on the public leaderboard would allow any anonymous caller to map every
/// contributor’s display name to their internal account GUID and cross-reference that
/// with other endpoints (e.g. sightings, search zones) to build a tracking profile.
/// </para>
/// </summary>
public sealed record ContributorScoreDto(
    string DisplayName,
    int ReunificationCount,
    string Badge,
    int TotalPoints,
    DateTimeOffset UpdatedAt)
{
    public static ContributorScoreDto FromDomain(ContributorScore s) => new(
        FirstName(s.OwnerName),
        s.ReunificationCount,
        s.Badge.ToString(),
        s.TotalPoints,
        s.UpdatedAt);

    private static string FirstName(string fullName)
    {
        var first = fullName.Split(' ', System.StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? fullName;
        return first.Length > 20 ? first[..20] : first;
    }
}
