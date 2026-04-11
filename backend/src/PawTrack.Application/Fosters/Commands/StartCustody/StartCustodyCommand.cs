using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Fosters;

namespace PawTrack.Application.Fosters.Commands.StartCustody;

public sealed record StartCustodyCommand(
    Guid FosterUserId,
    Guid FoundPetReportId,
    int ExpectedDays,
    string? Note) : IRequest<Result<Guid>>;

public sealed class StartCustodyCommandHandler(
    IFosterVolunteerRepository fosterVolunteerRepository,
    ICustodyRecordRepository custodyRecordRepository,
    IFoundPetRepository foundPetRepository,
    ILostPetRepository lostPetRepository,
    IUserRepository userRepository,
    IPetRepository petRepository,
    INotificationDispatcher notificationDispatcher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<StartCustodyCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(StartCustodyCommand request, CancellationToken cancellationToken)
    {
        var foster = await fosterVolunteerRepository.GetByUserIdAsync(request.FosterUserId, cancellationToken);
        if (foster is null)
            return Result.Failure<Guid>("Foster profile not found.");

        if (!foster.IsAvailable)
            return Result.Failure<Guid>("Foster is not available.");

        var foundReport = await foundPetRepository.GetByIdAsync(request.FoundPetReportId, cancellationToken);
        if (foundReport is null)
            return Result.Failure<Guid>("Found pet report not found.");

        var record = CustodyRecord.Start(
            request.FosterUserId,
            request.FoundPetReportId,
            request.ExpectedDays,
            request.Note);

        await custodyRecordRepository.AddAsync(record, cancellationToken);
        foster.UpdateProfile(
            foster.FullName,
            foster.HomeLat,
            foster.HomeLng,
            foster.AcceptedSpecies,
            foster.SizePreference,
            foster.MaxDays,
            false,
            foster.AvailableUntil);
        fosterVolunteerRepository.Update(foster);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Dispatch bidirectional custody-started notifications when we have enough context.
        if (foundReport.MatchedLostPetEventId.HasValue)
        {
            var lostEvent = await lostPetRepository.GetByIdAsync(foundReport.MatchedLostPetEventId.Value, cancellationToken);
            if (lostEvent is not null)
            {
                var fosterUser = await userRepository.GetByIdAsync(request.FosterUserId, cancellationToken);
                var ownerUser  = await userRepository.GetByIdAsync(lostEvent.OwnerId, cancellationToken);
                var pet        = await petRepository.GetByIdAsync(lostEvent.PetId, cancellationToken);

                if (fosterUser is not null && ownerUser is not null && pet is not null)
                {
                    await notificationDispatcher.DispatchCustodyStartedAsync(
                        record.Id,
                        fosterUser.Id,
                        fosterUser.Email,
                        fosterUser.Name,
                        ownerUser.Id,
                        ownerUser.Email,
                        ownerUser.Name,
                        pet.Name,
                        request.ExpectedDays,
                        cancellationToken);
                }
            }
        }

        return Result.Success(record.Id);
    }
}
