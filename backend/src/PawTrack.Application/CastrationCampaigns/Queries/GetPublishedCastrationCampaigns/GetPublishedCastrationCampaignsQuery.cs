using MediatR;
using PawTrack.Application.CastrationCampaigns.DTOs;
using PawTrack.Application.CastrationCampaigns.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.CastrationCampaigns.Queries.GetPublishedCastrationCampaigns;

public sealed record GetPublishedCastrationCampaignsQuery(
    string? Canton,
    int Page,
    int PageSize) : IRequest<Result<CastrationCampaignPageDto>>;

public sealed class GetPublishedCastrationCampaignsQueryHandler(
    ICastrationCampaignRepository repository)
    : IRequestHandler<GetPublishedCastrationCampaignsQuery, Result<CastrationCampaignPageDto>>
{
    public async Task<Result<CastrationCampaignPageDto>> Handle(
        GetPublishedCastrationCampaignsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, total) = await repository.GetPublishedPagedAsync(
            request.Canton,
            (request.Page - 1) * request.PageSize,
            request.PageSize,
            cancellationToken);

        return Result.Success(new CastrationCampaignPageDto(
            items.Select(CastrationCampaignDto.FromDomain).ToList(),
            total,
            request.Page,
            request.PageSize));
    }
}
