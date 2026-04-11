using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Pets.DTOs;
using PawTrack.Domain.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PawTrack.Application.Common.Settings;

namespace PawTrack.Application.Pets.Queries.GetPetScanHistory;

public sealed record GetPetScanHistoryQuery(Guid PetId, Guid RequestingUserId)
    : IRequest<Result<PetScanHistoryDto>>;

public sealed class GetPetScanHistoryQueryHandler(
    IPetRepository petRepository,
    IQrScanEventRepository qrScanEventRepository,
    IOptions<PetScanExportSettings> exportSettings)
    : IRequestHandler<GetPetScanHistoryQuery, Result<PetScanHistoryDto>>
{
    private const int DefaultPageSize = 50;

    public async Task<Result<PetScanHistoryDto>> Handle(
        GetPetScanHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);

        if (pet is null)
            return Result.Failure<PetScanHistoryDto>("Pet not found.");

        if (pet.OwnerId != request.RequestingUserId)
            return Result.Failure<PetScanHistoryDto>("Access denied.");

        var events = await qrScanEventRepository.GetByPetIdAsync(
            request.PetId,
            DefaultPageSize,
            cancellationToken);

        var ordered = events
            .OrderByDescending(e => e.ScannedAt)
            .Select(PetScanEventDto.FromDomain)
            .ToList();

        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var scansToday = events.Count(e => DateOnly.FromDateTime(e.ScannedAt.UtcDateTime) == today);

        var (signature, signedAt) = SignExport(request.PetId, ordered);

        return Result.Success(new PetScanHistoryDto(scansToday, ordered, signature, signedAt));
    }

    private (string? Signature, DateTimeOffset? SignedAt) SignExport(
        Guid petId,
        IReadOnlyList<PetScanEventDto> events)
    {
        var signingKey = exportSettings.Value.SigningKey;
        if (string.IsNullOrWhiteSpace(signingKey))
            return (null, null);

        var signedAt = DateTimeOffset.UtcNow;
        // Deterministic payload: petId + signedAt epoch + event count + ordered scanned timestamps
        var payload = new
        {
            PetId      = petId,
            SignedAtUtc = signedAt.ToUnixTimeSeconds(),
            EventCount = events.Count,
            Timestamps = events.Select(e => e.ScannedAt.ToUnixTimeSeconds()).ToArray(),
        };
        var payloadJson  = JsonSerializer.Serialize(payload);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        var keyBytes     = Encoding.UTF8.GetBytes(signingKey);
        var hash         = HMACSHA256.HashData(keyBytes, payloadBytes);
        var signature    = $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";

        return (signature, signedAt);
    }
}
