using MediatR;
using PawTrack.Application.Clinics;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;
using PawTrack.Domain.Subscriptions;
using System.Security.Cryptography;

namespace PawTrack.Application.Clinics.Commands.ManageApiKey;

public sealed record ClinicApiKeyDto(
    Guid Id, string Label, bool IsRevoked,
    DateTimeOffset CreatedAt, DateTimeOffset? LastUsedAt,
    string? RawKey = null);  // only populated on create

// ── Create ────────────────────────────────────────────────────────────────────

public sealed record CreateClinicApiKeyCommand(Guid ClinicId, Guid RequestingUserId, string Label)
    : IRequest<Result<ClinicApiKeyDto>>;

public sealed class CreateClinicApiKeyCommandHandler(
    IClinicRepository clinicRepository,
    IClinicApiKeyRepository keyRepository,
    ISubscriptionRepository subscriptionRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateClinicApiKeyCommand, Result<ClinicApiKeyDto>>
{
    public async Task<Result<ClinicApiKeyDto>> Handle(
        CreateClinicApiKeyCommand request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.RequestingUserId)
            return Result.Failure<ClinicApiKeyDto>("Access denied.");

        var sub = await subscriptionRepository.GetActiveForClinicAsync(request.ClinicId, cancellationToken);
        if (sub is null || sub.Tier < SubscriptionTier.ClinicPartner)
            return Result.Failure<ClinicApiKeyDto>("Las API Keys requieren el plan Clínica Partner.");

        // Generate a random 32-byte key and hash it for storage
        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var rawKey = "ptwk_" + Convert.ToBase64String(rawBytes)
            .Replace("+", "-").Replace("/", "_").Replace("=", "");
        var hash = ClinicApiKeyHasher.Compute(rawKey);

        var key = ClinicApiKey.Create(request.ClinicId, hash, request.Label);
        await keyRepository.AddAsync(key, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new ClinicApiKeyDto(key.Id, key.Label, false, key.CreatedAt, null, rawKey));
    }
}

// ── Revoke ────────────────────────────────────────────────────────────────────

public sealed record RevokeClinicApiKeyCommand(Guid KeyId, Guid ClinicId, Guid RequestingUserId)
    : IRequest<Result<bool>>;

public sealed class RevokeClinicApiKeyCommandHandler(
    IClinicRepository clinicRepository,
    IClinicApiKeyRepository keyRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RevokeClinicApiKeyCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        RevokeClinicApiKeyCommand request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.RequestingUserId)
            return Result.Failure<bool>("Access denied.");

        var keys = await keyRepository.GetForClinicAsync(request.ClinicId, cancellationToken);
        var key = keys.FirstOrDefault(k => k.Id == request.KeyId);
        if (key is null)
            return Result.Failure<bool>("API key not found.");

        // Re-fetch as tracked
        var tracked = await keyRepository.GetByHashAsync(key.KeyHash, cancellationToken);
        if (tracked is null)
            return Result.Failure<bool>("Key not found or already revoked.");

        tracked.Revoke();
        keyRepository.Update(tracked);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}

// ── List ──────────────────────────────────────────────────────────────────────

public sealed record GetClinicApiKeysQuery(Guid ClinicId, Guid RequestingUserId)
    : IRequest<Result<IReadOnlyList<ClinicApiKeyDto>>>;

public sealed class GetClinicApiKeysQueryHandler(
    IClinicRepository clinicRepository,
    IClinicApiKeyRepository keyRepository)
    : IRequestHandler<GetClinicApiKeysQuery, Result<IReadOnlyList<ClinicApiKeyDto>>>
{
    public async Task<Result<IReadOnlyList<ClinicApiKeyDto>>> Handle(
        GetClinicApiKeysQuery request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.RequestingUserId)
            return Result.Failure<IReadOnlyList<ClinicApiKeyDto>>("Access denied.");

        var keys = await keyRepository.GetForClinicAsync(request.ClinicId, cancellationToken);
        var dtos = keys
            .Select(k => new ClinicApiKeyDto(k.Id, k.Label, k.IsRevoked, k.CreatedAt, k.LastUsedAt))
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<ClinicApiKeyDto>>(dtos);
    }
}
