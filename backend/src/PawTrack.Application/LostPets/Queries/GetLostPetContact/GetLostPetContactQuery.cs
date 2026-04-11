using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.LostPets;

namespace PawTrack.Application.LostPets.Queries.GetLostPetContact;

// ── DTO ────────────────────────────────────────────────────────────────────────

/// <summary>
/// Contact information for a lost-pet report.
/// <b>ContactPhone is PII</b> — only returned from authenticated endpoints, never from public ones.
/// </summary>
public sealed record LostPetContactDto(
    string LostEventId,
    string? ContactName,
    string? ContactPhone);

// ── Query + Handler ────────────────────────────────────────────────────────────

/// <summary>
/// Returns the emergency contact details for an active lost-pet report.
/// Any authenticated user can call this — not restricted to the owner — so that
/// people who find a pet can see the contact phone after signing in.
/// Only Active reports are surfaced; resolved events return 404.
/// </summary>
public sealed record GetLostPetContactQuery(Guid LostEventId) : IRequest<Result<LostPetContactDto>>;

public sealed class GetLostPetContactQueryHandler(ILostPetRepository lostPetRepository)
    : IRequestHandler<GetLostPetContactQuery, Result<LostPetContactDto>>
{
    public async Task<Result<LostPetContactDto>> Handle(
        GetLostPetContactQuery request, CancellationToken cancellationToken)
    {
        var report = await lostPetRepository.GetByIdAsync(request.LostEventId, cancellationToken);

        if (report is null || report.Status != LostPetStatus.Active)
            return Result.Failure<LostPetContactDto>("Lost pet report not found.");

        return Result.Success(new LostPetContactDto(
            report.Id.ToString(),
            report.ContactName,
            report.ContactPhone));
    }
}
