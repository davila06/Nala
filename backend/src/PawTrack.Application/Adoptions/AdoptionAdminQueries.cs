using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Adoptions;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Adoptions;

// ── Admin stats ───────────────────────────────────────────────────────────────

public sealed record AdoptionAdminStatsDto(
    int TotalPublished,
    int TotalAvailable,
    int TotalInProcess,
    int TotalAdopted,
    int TotalPaused,
    int TotalApplications,
    int TotalFairs);

public sealed record GetAdoptionAdminStatsQuery : IRequest<Result<AdoptionAdminStatsDto>>;

public sealed class GetAdoptionAdminStatsQueryHandler(IAdoptionRepository adoptionRepository)
    : IRequestHandler<GetAdoptionAdminStatsQuery, Result<AdoptionAdminStatsDto>>
{
    public async Task<Result<AdoptionAdminStatsDto>> Handle(
        GetAdoptionAdminStatsQuery request, CancellationToken ct)
    {
        var stats = await adoptionRepository.GetAdminStatsAsync(ct);
        return Result.Success(stats);
    }
}

// ── Admin list all animals (paged, any status) ────────────────────────────────

public sealed record GetAllAdoptableAnimalsAdminQuery(
    string? StatusFilter,
    int Page,
    int PageSize) : IRequest<Result<Common.PagedResult<AdoptablePetDto>>>;

public sealed class GetAllAdoptableAnimalsAdminQueryHandler(
    IAdoptionRepository adoptionRepository,
    IAllyProfileRepository allyProfileRepository)
    : IRequestHandler<GetAllAdoptableAnimalsAdminQuery, Result<Common.PagedResult<AdoptablePetDto>>>
{
    public async Task<Result<Common.PagedResult<AdoptablePetDto>>> Handle(
        GetAllAdoptableAnimalsAdminQuery request, CancellationToken ct)
    {
        AdoptionStatus? status = request.StatusFilter is not null
            ? Enum.TryParse<AdoptionStatus>(request.StatusFilter, out var s) ? s : null
            : null;

        var (items, total) = await adoptionRepository.GetAllAdminPagedAsync(
            status, (request.Page - 1) * request.PageSize, request.PageSize, ct);

        var orgIds = items.Select(a => a.OrganizationUserId).Distinct().ToList();
        var allies = await allyProfileRepository.GetByUserIdsAsync(orgIds, ct);
        var orgNames = allies.ToDictionary(a => a.UserId, a => a.OrganizationName);

        var dtos = items
            .Select(a => AdoptablePetDto.FromDomain(a,
                orgNames.GetValueOrDefault(a.OrganizationUserId, "Organización")))
            .ToList();

        return Result.Success(new Common.PagedResult<AdoptablePetDto>(
            dtos, total, request.Page, request.PageSize));
    }
}

// ── Admin moderate animal ─────────────────────────────────────────────────────

public sealed record AdminModerateAnimalCommand(
    Guid AnimalId,
    string Action) : IRequest<Result<bool>>;

public sealed class AdminModerateAnimalCommandHandler(
    IAdoptionRepository adoptionRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AdminModerateAnimalCommand, Result<bool>>
{
    public const string InvalidActionError = "invalid_action";

    public async Task<Result<bool>> Handle(AdminModerateAnimalCommand request, CancellationToken ct)
    {
        var animal = await adoptionRepository.GetAnimalByIdAsync(request.AnimalId, ct);
        if (animal is null)
            return Result.Failure<bool>("animal_not_found");

        switch (request.Action.ToLowerInvariant())
        {
            case "remove":  animal.Remove();  break;
            case "pause":   animal.Pause();   break;
            case "restore": animal.Republish(); break;
            default:
                return Result.Failure<bool>(InvalidActionError);
        }

        adoptionRepository.UpdateAnimal(animal);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(true);
    }
}
