using MediatR;
using PawTrack.Application.Municipalities.DTOs;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Municipalities;

namespace PawTrack.Application.Municipalities.Queries.SearchCaptures;

public sealed record SearchCapturesQuery(
    string? Canton,
    CapturedAnimalStatus? Status,
    int Page     = 1,
    int PageSize = 20) : IRequest<Result<CapturedAnimalPageDto>>;

public sealed record CapturedAnimalPageDto(IReadOnlyList<CapturedAnimalDto> Items, int Total, int Page, int PageSize);

public sealed class SearchCapturesQueryHandler(ICapturedAnimalRepository repository)
    : IRequestHandler<SearchCapturesQuery, Result<CapturedAnimalPageDto>>
{
    public async Task<Result<CapturedAnimalPageDto>> Handle(
        SearchCapturesQuery request,
        CancellationToken cancellationToken)
    {
        var (items, total) = await repository.SearchAsync(
            request.Canton, request.Status, request.Page, request.PageSize, cancellationToken);

        return Result.Success(new CapturedAnimalPageDto(
            items.Select(CapturedAnimalDto.FromDomain).ToList(),
            total,
            request.Page,
            request.PageSize));
    }
}
