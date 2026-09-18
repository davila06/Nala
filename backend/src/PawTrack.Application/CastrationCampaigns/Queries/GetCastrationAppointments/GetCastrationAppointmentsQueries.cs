using MediatR;
using PawTrack.Application.CastrationCampaigns.DTOs;
using PawTrack.Application.CastrationCampaigns.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Common;

namespace PawTrack.Application.CastrationCampaigns.Queries.GetCastrationAppointments;

public sealed record GetCampaignAppointmentsQuery(Guid CampaignId, Guid RequestingUserId, int Page, int PageSize)
    : IRequest<Result<CastrationAppointmentPageDto>>;
public sealed record GetMyCastrationAppointmentsQuery(Guid OwnerUserId, int Page, int PageSize)
    : IRequest<Result<CastrationAppointmentPageDto>>;

public sealed class GetCampaignAppointmentsQueryHandler(
    ICastrationCampaignRepository campaignRepository,
    ICastrationAppointmentRepository appointmentRepository,
    IClinicRepository clinicRepository,
    IUserRepository userRepository)
    : IRequestHandler<GetCampaignAppointmentsQuery, Result<CastrationAppointmentPageDto>>
{
    public async Task<Result<CastrationAppointmentPageDto>> Handle(GetCampaignAppointmentsQuery request, CancellationToken ct)
    {
        var campaign = await campaignRepository.GetByIdAsync(request.CampaignId, ct);
        if (campaign is null) return Result.Failure<CastrationAppointmentPageDto>("Campaign was not found.");
        var user = await userRepository.GetByIdAsync(request.RequestingUserId, ct);
        var clinic = await clinicRepository.GetByUserIdAsync(request.RequestingUserId, ct);
        if (user is null || !user.Role.IsAdminOrSuperAdmin() && clinic?.Id != campaign.ExecutingClinicId)
            return Result.Failure<CastrationAppointmentPageDto>("Only the executing clinic or an administrator can view this agenda.");
        var (items, total) = await appointmentRepository.GetByCampaignPagedAsync(
            request.CampaignId, (request.Page - 1) * request.PageSize, request.PageSize, ct);
        return Result.Success(new CastrationAppointmentPageDto(
            items.Select(CastrationAppointmentDto.FromDomain).ToList(), total, request.Page, request.PageSize));
    }
}

public sealed class GetMyCastrationAppointmentsQueryHandler(ICastrationAppointmentRepository repository)
    : IRequestHandler<GetMyCastrationAppointmentsQuery, Result<CastrationAppointmentPageDto>>
{
    public async Task<Result<CastrationAppointmentPageDto>> Handle(GetMyCastrationAppointmentsQuery request, CancellationToken ct)
    {
        var (items, total) = await repository.GetByOwnerPagedAsync(
            request.OwnerUserId, (request.Page - 1) * request.PageSize, request.PageSize, ct);
        return Result.Success(new CastrationAppointmentPageDto(
            items.Select(CastrationAppointmentDto.FromDomain).ToList(), total, request.Page, request.PageSize));
    }
}
