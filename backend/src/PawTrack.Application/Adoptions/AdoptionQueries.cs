using MediatR;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Adoptions;
using PawTrack.Domain.Allies;
using PawTrack.Domain.Common;
using PawTrack.Domain.Pets;

namespace PawTrack.Application.Adoptions;

// ── Get public animals (paginado, filtrable) ──────────────────────────────────

public sealed record GetAdoptablePetsQuery(
    PetSpecies? Species,
    PetSize? Size,
    AgeCategory? AgeCategory,
    bool? IsVaccinated,
    bool? IsSterilized,
    bool? OkWithKids,
    bool? OkWithDogs,
    double? NearLat,
    double? NearLng,
    int? RadiusKm,
    int Page,
    int PageSize) : IRequest<Result<PagedResult<AdoptablePetDto>>>;

public sealed class GetAdoptablePetsQueryHandler(
    IAdoptionRepository adoptionRepository,
    IAllyProfileRepository allyProfileRepository)
    : IRequestHandler<GetAdoptablePetsQuery, Result<PagedResult<AdoptablePetDto>>>
{
    public async Task<Result<PagedResult<AdoptablePetDto>>> Handle(
        GetAdoptablePetsQuery request, CancellationToken ct)
    {
        var (items, total) = await adoptionRepository.GetAvailablePagedAsync(
            request.Species, request.Size, request.AgeCategory,
            request.IsVaccinated, request.IsSterilized, request.OkWithKids, request.OkWithDogs,
            request.NearLat, request.NearLng, request.RadiusKm ?? 50,
            (request.Page - 1) * request.PageSize, request.PageSize, ct);

        var orgIds = items.Select(a => a.OrganizationUserId).Distinct().ToList();
        var allies = await allyProfileRepository.GetByUserIdsAsync(orgIds, ct);
        var orgNames = allies.ToDictionary(a => a.UserId, a => a.OrganizationName);

        var dtos = items
            .Select(a => AdoptablePetDto.FromDomain(a,
                orgNames.GetValueOrDefault(a.OrganizationUserId, "Organización")))
            .ToList();

        return Result.Success(new PagedResult<AdoptablePetDto>(dtos, total, request.Page, request.PageSize));
    }
}

// ── Get single animal by ID ───────────────────────────────────────────────────

public sealed record GetAdoptablePetByIdQuery(Guid Id) : IRequest<Result<AdoptablePetDto>>;

public sealed class GetAdoptablePetByIdQueryHandler(
    IAdoptionRepository adoptionRepository,
    IAllyProfileRepository allyProfileRepository)
    : IRequestHandler<GetAdoptablePetByIdQuery, Result<AdoptablePetDto>>
{
    public async Task<Result<AdoptablePetDto>> Handle(
        GetAdoptablePetByIdQuery request, CancellationToken ct)
    {
        var animal = await adoptionRepository.GetAnimalByIdAsync(request.Id, ct);
        if (animal is null || animal.Status == AdoptionStatus.Removed)
            return Result.Failure<AdoptablePetDto>("animal_not_found");

        var ally = await allyProfileRepository.GetByUserIdAsync(animal.OrganizationUserId, ct);
        var orgName = ally?.OrganizationName ?? "Organización";

        return Result.Success(AdoptablePetDto.FromDomain(animal, orgName));
    }
}

// ── Get organization's own animals ────────────────────────────────────────────

public sealed record GetMyAdoptionAnimalsQuery(
    Guid OrganizationUserId,
    int Page,
    int PageSize) : IRequest<Result<PagedResult<AdoptablePetDto>>>;

public sealed class GetMyAdoptionAnimalsQueryHandler(
    IAdoptionRepository adoptionRepository,
    IAllyProfileRepository allyProfileRepository)
    : IRequestHandler<GetMyAdoptionAnimalsQuery, Result<PagedResult<AdoptablePetDto>>>
{
    public async Task<Result<PagedResult<AdoptablePetDto>>> Handle(
        GetMyAdoptionAnimalsQuery request, CancellationToken ct)
    {
        var ally = await allyProfileRepository.GetByUserIdAsync(request.OrganizationUserId, ct);
        if (ally is null)
            return Result.Failure<PagedResult<AdoptablePetDto>>("ally_not_found");

        var skip = (request.Page - 1) * request.PageSize;
        var items = await adoptionRepository.GetByOrganizationAsync(request.OrganizationUserId, skip, request.PageSize, ct);
        var total = await adoptionRepository.CountByOrganizationAsync(request.OrganizationUserId, ct);

        var dtos = items.Select(a => AdoptablePetDto.FromDomain(a, ally.OrganizationName)).ToList();
        return Result.Success(new PagedResult<AdoptablePetDto>(dtos, total, request.Page, request.PageSize));
    }
}

// ── Get applications for an animal (shelter view) ────────────────────────────

public sealed record GetApplicationsForAnimalQuery(
    Guid OrganizationUserId,
    Guid AdoptablePetId) : IRequest<Result<IReadOnlyList<AdoptionApplicationDto>>>;

public sealed class GetApplicationsForAnimalQueryHandler(
    IAdoptionRepository adoptionRepository,
    IAllyProfileRepository allyProfileRepository)
    : IRequestHandler<GetApplicationsForAnimalQuery, Result<IReadOnlyList<AdoptionApplicationDto>>>
{
    public async Task<Result<IReadOnlyList<AdoptionApplicationDto>>> Handle(
        GetApplicationsForAnimalQuery request, CancellationToken ct)
    {
        var ally = await allyProfileRepository.GetVerifiedByUserIdAsync(request.OrganizationUserId, ct);
        if (ally is null || ally.AllyType != AllyType.Shelter)
            return Result.Failure<IReadOnlyList<AdoptionApplicationDto>>("not_verified_shelter");

        var animal = await adoptionRepository.GetAnimalByIdAsync(request.AdoptablePetId, ct);
        if (animal is null || animal.OrganizationUserId != request.OrganizationUserId)
            return Result.Failure<IReadOnlyList<AdoptionApplicationDto>>("access_denied");

        var apps = await adoptionRepository.GetApplicationsByAnimalAsync(request.AdoptablePetId, ct);
        return Result.Success<IReadOnlyList<AdoptionApplicationDto>>(
            apps.Select(a => new AdoptionApplicationDto(
                a.Id.ToString(), a.AdoptablePetId.ToString(), a.ApplicantUserId.ToString(),
                a.ApplicantNote, a.Status.ToString(), a.ReviewNote, a.AppliedAt, a.ReviewedAt))
            .ToList());
    }
}

// ── Get my applications (applicant view) ─────────────────────────────────────

public sealed record GetMyAdoptionApplicationsQuery(
    Guid ApplicantUserId,
    int Page,
    int PageSize) : IRequest<Result<PagedResult<AdoptionApplicationDto>>>;

public sealed class GetMyAdoptionApplicationsQueryHandler(IAdoptionRepository adoptionRepository)
    : IRequestHandler<GetMyAdoptionApplicationsQuery, Result<PagedResult<AdoptionApplicationDto>>>
{
    public async Task<Result<PagedResult<AdoptionApplicationDto>>> Handle(
        GetMyAdoptionApplicationsQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;
        var items = await adoptionRepository.GetApplicationsByApplicantAsync(
            request.ApplicantUserId, skip, request.PageSize, ct);
        var total = await adoptionRepository.CountApplicationsByApplicantAsync(request.ApplicantUserId, ct);

        var dtos = items.Select(a => new AdoptionApplicationDto(
            a.Id.ToString(), a.AdoptablePetId.ToString(), a.ApplicantUserId.ToString(),
            a.ApplicantNote, a.Status.ToString(), a.ReviewNote, a.AppliedAt, a.ReviewedAt))
            .ToList();

        return Result.Success(new PagedResult<AdoptionApplicationDto>(dtos, total, request.Page, request.PageSize));
    }
}

// ── Get upcoming fairs (public, geo-filtered) ─────────────────────────────────

public sealed record GetUpcomingFairsQuery(
    double? NearLat,
    double? NearLng,
    int? RadiusKm) : IRequest<Result<IReadOnlyList<AdoptionFairDto>>>;

public sealed class GetUpcomingFairsQueryHandler(IAdoptionRepository adoptionRepository)
    : IRequestHandler<GetUpcomingFairsQuery, Result<IReadOnlyList<AdoptionFairDto>>>
{
    public async Task<Result<IReadOnlyList<AdoptionFairDto>>> Handle(
        GetUpcomingFairsQuery request, CancellationToken ct)
    {
        var fairs = await adoptionRepository.GetUpcomingFairsAsync(
            request.NearLat, request.NearLng, request.RadiusKm ?? 50, ct);

        return Result.Success<IReadOnlyList<AdoptionFairDto>>(
            fairs.Select(AdoptionFairDto.FromDomain).ToList());
    }
}
