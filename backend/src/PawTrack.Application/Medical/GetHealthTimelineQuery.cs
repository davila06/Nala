using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Medical;

public sealed record HealthTimelineItemDto(
    Guid Id, string Source, DateOnly Date, string Label, string Kind,
    string? DocumentUrl, string? VerificationCode, bool IsRevoked,
    string? DocumentKind = null);

public sealed record HealthTimelinePageDto(IReadOnlyList<HealthTimelineItemDto> Items, bool HasMore);

public interface IHealthTimelineReadRepository
{
    Task<HealthTimelinePageDto> GetPageAsync(Guid petId, int offset, int pageSize, CancellationToken cancellationToken);
}

public sealed record GetHealthTimelineQuery(Guid PetId, Guid RequestingUserId, int Page, int PageSize)
    : IRequest<Result<HealthTimelinePageDto>>;

public sealed class GetHealthTimelineQueryHandler(
    IPetRepository petRepository,
    IFamilyRepository familyRepository,
    ISubscriptionService subscriptionService,
    IHealthTimelineReadRepository timelineRepository)
    : IRequestHandler<GetHealthTimelineQuery, Result<HealthTimelinePageDto>>
{
    public async Task<Result<HealthTimelinePageDto>> Handle(GetHealthTimelineQuery request, CancellationToken ct)
    {
        if (request.PetId == Guid.Empty || request.Page < 1 || request.PageSize is < 1 or > 50 ||
            (long)(request.Page - 1) * request.PageSize > int.MaxValue - request.PageSize - 1)
            return Result.Failure<HealthTimelinePageDto>("Paginación inválida.");

        if (!await subscriptionService.IsFamiliaAsync(request.RequestingUserId, ct))
            return Result.Failure<HealthTimelinePageDto>("El historial consolidado requiere el plan Familia.");

        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null || pet.OwnerId != request.RequestingUserId &&
            !(await familyRepository.GetActiveMemberIdsAsync(pet.OwnerId, ct)).Contains(request.RequestingUserId))
            return Result.Failure<HealthTimelinePageDto>("Acceso denegado.");

        return Result.Success(await timelineRepository.GetPageAsync(
            pet.Id, (request.Page - 1) * request.PageSize, request.PageSize, ct));
    }
}
