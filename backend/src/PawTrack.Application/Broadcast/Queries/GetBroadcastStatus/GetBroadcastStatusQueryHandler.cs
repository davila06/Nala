using MediatR;
using PawTrack.Application.Broadcast.DTOs;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Broadcast;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Broadcast.Queries.GetBroadcastStatus;

public sealed class GetBroadcastStatusQueryHandler(
    IBroadcastAttemptRepository broadcastAttemptRepository,
    ILostPetRepository lostPetRepository)
    : IRequestHandler<GetBroadcastStatusQuery, Result<BroadcastStatusDto>>
{
    public async Task<Result<BroadcastStatusDto>> Handle(
        GetBroadcastStatusQuery request,
        CancellationToken cancellationToken)
    {
        var lostEvent = await lostPetRepository.GetByIdAsync(request.LostPetEventId, cancellationToken);
        if (lostEvent is null)
            return Result.Failure<BroadcastStatusDto>("Lost pet report not found.");

        if (lostEvent.OwnerId != request.RequestingUserId)
            return Result.Failure<BroadcastStatusDto>("Access denied.");

        var attempts = await broadcastAttemptRepository
            .GetByLostEventIdAsync(request.LostPetEventId, cancellationToken);

        var dtos = attempts.Select(BroadcastAttemptDto.FromDomain).ToList();

        var dto = new BroadcastStatusDto(
            LostPetEventId: request.LostPetEventId.ToString(),
            Attempts: dtos,
            SentCount: dtos.Count(a => a.Status == nameof(BroadcastStatus.Sent)),
            FailedCount: dtos.Count(a => a.Status == nameof(BroadcastStatus.Failed)),
            SkippedCount: dtos.Count(a => a.Status == nameof(BroadcastStatus.Skipped)),
            TotalClicks: dtos.Sum(a => a.TrackingClicks));

        return Result.Success(dto);
    }
}
