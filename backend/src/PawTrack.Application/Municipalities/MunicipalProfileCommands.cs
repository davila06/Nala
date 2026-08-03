using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Municipalities;

namespace PawTrack.Application.Municipalities;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public sealed record MunicipalProfileDto(
    Guid Id,
    Guid UserId,
    string Canton,
    string OrgName,
    string Tier,
    bool IsActive,
    bool IsExpired,
    IReadOnlyList<string> AllCantons,
    DateTimeOffset SubscribedAt,
    DateTimeOffset? ExpiresAt)
{
    public static MunicipalProfileDto FromDomain(MunicipalityProfile p) => new(
        p.Id, p.UserId, p.Canton, p.OrgName, p.Tier.ToString(),
        p.IsActive, p.IsExpired, p.AllCantons,
        p.SubscribedAt, p.ExpiresAt);
}

// ── Get profile ───────────────────────────────────────────────────────────────

public sealed record GetMunicipalProfileQuery(Guid UserId) : IRequest<Result<MunicipalProfileDto?>>;

public sealed class GetMunicipalProfileQueryHandler(IMunicipalProfileRepository repo)
    : IRequestHandler<GetMunicipalProfileQuery, Result<MunicipalProfileDto?>>
{
    public async Task<Result<MunicipalProfileDto?>> Handle(
        GetMunicipalProfileQuery request, CancellationToken ct)
    {
        var profile = await repo.GetByUserIdAsync(request.UserId, ct);
        return Result.Success(profile is null ? null : MunicipalProfileDto.FromDomain(profile));
    }
}

// ── Create / upsert profile (Admin only) ──────────────────────────────────────

public sealed record UpsertMunicipalProfileCommand(
    Guid UserId,
    string Canton,
    string OrgName,
    MunicipalTier Tier,
    DateTimeOffset? ExpiresAt,
    IEnumerable<string>? AdditionalCantons) : IRequest<Result<MunicipalProfileDto>>;

public sealed class UpsertMunicipalProfileCommandHandler(
    IMunicipalProfileRepository repo,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpsertMunicipalProfileCommand, Result<MunicipalProfileDto>>
{
    public async Task<Result<MunicipalProfileDto>> Handle(
        UpsertMunicipalProfileCommand request, CancellationToken ct)
    {
        var existing = await repo.GetByUserIdAsync(request.UserId, ct);

        if (existing is null)
        {
            var newProfile = MunicipalityProfile.Create(
                request.UserId, request.Canton, request.OrgName, request.Tier, request.ExpiresAt);

            if (request.AdditionalCantons?.Any() == true)
                newProfile.SetAdditionalCantons(request.AdditionalCantons);

            await repo.AddAsync(newProfile, ct);
            await unitOfWork.SaveChangesAsync(ct);
            return Result.Success(MunicipalProfileDto.FromDomain(newProfile));
        }

        existing.Upgrade(request.Tier, request.ExpiresAt);
        if (request.AdditionalCantons is not null)
            existing.SetAdditionalCantons(request.AdditionalCantons);
        repo.Update(existing);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(MunicipalProfileDto.FromDomain(existing));
    }
}
