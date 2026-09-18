using PawTrack.Domain.CastrationCampaigns;

namespace PawTrack.Application.CastrationCampaigns.DTOs;

public sealed record CastrationAppointmentDto(
    Guid Id,
    Guid CampaignId,
    Guid PetId,
    Guid OwnerUserId,
    DateTimeOffset ScheduledAt,
    decimal BasePriceCrc,
    decimal IvaAmountCrc,
    decimal TotalAmountCrc,
    string Status,
    Guid? ExecutingVeterinarianId,
    string? ClinicalOutcome,
    string? PostOperativeInstructions)
{
    public static CastrationAppointmentDto FromDomain(CastrationAppointment appointment) => new(
        appointment.Id,
        appointment.CampaignId,
        appointment.PetId,
        appointment.OwnerUserId,
        appointment.ScheduledAt,
        appointment.BasePriceCrc,
        appointment.IvaAmountCrc,
        appointment.TotalAmountCrc,
        appointment.Status.ToString(),
        appointment.ExecutingVeterinarianId,
        appointment.ClinicalOutcome,
        appointment.PostOperativeInstructions);
}

public sealed record CastrationAppointmentPageDto(
    IReadOnlyList<CastrationAppointmentDto> Items, int Total, int Page, int PageSize);
