using MediatR;
using PawTrack.Application.Municipalities.DTOs;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Municipalities;

namespace PawTrack.Application.Municipalities.Queries.SearchCaptures;

public sealed record SearchCapturesQuery(
    Guid RequestingUserId,
    string? Canton,
    CapturedAnimalStatus? Status,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<CapturedAnimalPageDto>>;

public sealed record CapturedAnimalPageDto(IReadOnlyList<CapturedAnimalDto> Items, int Total, int Page, int PageSize);

public sealed class SearchCapturesQueryHandler(
    ICapturedAnimalRepository repository,
    IMunicipalSubscriptionService subscriptionService)
    : IRequestHandler<SearchCapturesQuery, Result<CapturedAnimalPageDto>>
{
    public async Task<Result<CapturedAnimalPageDto>> Handle(
        SearchCapturesQuery request,
        CancellationToken cancellationToken)
    {
        var authorizedCantons = await subscriptionService.GetAuthorizedCantonsAsync(
            request.RequestingUserId, cancellationToken);

        // Básica can only see their own primary canton; Full/RedRegional can filter freely
        string? cantonFilter = request.Canton;
        if (authorizedCantons.Count == 1)
        {
            // Básica — force their own canton regardless of what was requested
            cantonFilter = authorizedCantons[0];
        }
        else if (!string.IsNullOrWhiteSpace(request.Canton)
                 && !authorizedCantons.Contains(request.Canton, StringComparer.OrdinalIgnoreCase))
        {
            return Result.Failure<CapturedAnimalPageDto>(
                "No tienes permiso para consultar ese cantón.");
        }

        var (items, total) = await repository.SearchAsync(
            cantonFilter, request.Status, request.Page, request.PageSize, cancellationToken);

        return Result.Success(new CapturedAnimalPageDto(
            items.Select(CapturedAnimalDto.FromDomain).ToList(),
            total,
            request.Page,
            request.PageSize));
    }
}
