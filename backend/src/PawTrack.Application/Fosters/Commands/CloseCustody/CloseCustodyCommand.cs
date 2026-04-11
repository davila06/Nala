using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Fosters.Commands.CloseCustody;

public sealed record CloseCustodyCommand(
    Guid RecordId,
    Guid FosterUserId,
    string Outcome) : IRequest<Result<bool>>;

public sealed class CloseCustodyCommandHandler(
    ICustodyRecordRepository custodyRecordRepository,
    IFosterVolunteerRepository fosterVolunteerRepository,
    IFoundPetRepository foundPetRepository,
    ILostPetRepository lostPetRepository,
    IUserRepository userRepository,
    IPetRepository petRepository,
    INotificationDispatcher notificationDispatcher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CloseCustodyCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(CloseCustodyCommand request, CancellationToken cancellationToken)
    {
        var record = await custodyRecordRepository.GetByIdAsync(request.RecordId, cancellationToken);
        if (record is null)
            return Result.Failure<bool>("Custody record not found.");

        if (record.FosterUserId != request.FosterUserId)
            return Result.Failure<bool>("Access denied.");

        record.Close(request.Outcome);
        custodyRecordRepository.Update(record);

        var foster = await fosterVolunteerRepository.GetByUserIdAsync(request.FosterUserId, cancellationToken);
        if (foster is not null)
        {
            foster.MarkFosterCompleted();
            fosterVolunteerRepository.Update(foster);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Dispatch bidirectional custody-closed notifications (best-effort).
        var foundReport = await foundPetRepository.GetByIdAsync(record.FoundPetReportId, cancellationToken);
        if (foundReport?.MatchedLostPetEventId is { } lostEventId)
        {
            var lostEvent = await lostPetRepository.GetByIdAsync(lostEventId, cancellationToken);
            if (lostEvent is not null)
            {
                var fosterUserTask = userRepository.GetByIdAsync(request.FosterUserId, cancellationToken);
                var ownerUserTask  = userRepository.GetByIdAsync(lostEvent.OwnerId, cancellationToken);
                var petTask        = petRepository.GetByIdAsync(lostEvent.PetId, cancellationToken);

                await Task.WhenAll(fosterUserTask, ownerUserTask, petTask);

                var fosterUser = fosterUserTask.Result;
                var ownerUser  = ownerUserTask.Result;
                var pet        = petTask.Result;

                if (fosterUser is not null && ownerUser is not null && pet is not null)
                {
                    await notificationDispatcher.DispatchCustodyClosedAsync(
                        record.Id,
                        fosterUser.Id,
                        fosterUser.Email,
                        fosterUser.Name,
                        ownerUser.Id,
                        ownerUser.Email,
                        ownerUser.Name,
                        pet.Name,
                        request.Outcome,
                        cancellationToken);
                }
            }
        }

        return Result.Success(true);
    }
}
