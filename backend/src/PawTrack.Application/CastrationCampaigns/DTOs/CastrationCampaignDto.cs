using PawTrack.Domain.CastrationCampaigns;

namespace PawTrack.Application.CastrationCampaigns.DTOs;

public sealed record CastrationCampaignDto(
    Guid Id,
    Guid OrganizerUserId,
    Guid ExecutingClinicId,
    string Title,
    string VenueLabel,
    string Canton,
    double Latitude,
    double Longitude,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    DateTimeOffset ReservationsOpenAt,
    DateTimeOffset ReservationsCloseAt,
    int Capacity,
    int ReservedCount,
    int AvailableCapacity,
    decimal BasePriceCrc,
    string ConsentVersion,
    string Status)
{
    public static CastrationCampaignDto FromDomain(CastrationCampaign campaign) => new(
        campaign.Id,
        campaign.OrganizerUserId,
        campaign.ExecutingClinicId,
        campaign.Title,
        campaign.VenueLabel,
        campaign.Canton,
        campaign.Latitude,
        campaign.Longitude,
        campaign.StartsAt,
        campaign.EndsAt,
        campaign.ReservationsOpenAt,
        campaign.ReservationsCloseAt,
        campaign.Capacity,
        campaign.ReservedCount,
        campaign.AvailableCapacity,
        campaign.BasePriceCrc,
        campaign.ConsentVersion,
        campaign.Status.ToString());
}

public sealed record CastrationCampaignPageDto(
    IReadOnlyList<CastrationCampaignDto> Items,
    int Total,
    int Page,
    int PageSize);
