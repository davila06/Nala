using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Municipalities.DTOs;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Municipalities;

namespace PawTrack.Application.Municipalities.Commands.UpdateCaptureStatus;

public sealed record UpdateCaptureStatusCommand(
    Guid                 AnimalId,
    CapturedAnimalStatus Status,
    Guid?                MatchedPetId = null) : IRequest<Result<CapturedAnimalDto>>;

public sealed class UpdateCaptureStatusCommandHandler(
    ICapturedAnimalRepository repository,
    IUnitOfWork               unitOfWork)
    : IRequestHandler<UpdateCaptureStatusCommand, Result<CapturedAnimalDto>>
{
    public async Task<Result<CapturedAnimalDto>> Handle(
        UpdateCaptureStatusCommand request,
        CancellationToken cancellationToken)
    {
        var animal = await repository.GetByIdAsync(request.AnimalId, cancellationToken);
        if (animal is null) return Result.Failure<CapturedAnimalDto>("Animal record not found.");

        animal.UpdateStatus(request.Status);
        if (request.MatchedPetId.HasValue) animal.LinkToPet(request.MatchedPetId.Value);

        repository.Update(animal);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CapturedAnimalDto.FromDomain(animal));
    }
}
