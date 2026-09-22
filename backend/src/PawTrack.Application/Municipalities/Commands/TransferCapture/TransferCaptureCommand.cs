using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Municipalities.DTOs;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Municipalities;
using PawTrack.Application.Subscriptions.Services;

namespace PawTrack.Application.Municipalities.Commands.TransferCapture;

// ── Command — Red Regional only ───────────────────────────────────────────────

public sealed record TransferCaptureCommand(
    Guid RequestingUserId,
    Guid AnimalId,
    string DestinationCanton,
    string? TransferNotes) : IRequest<Result<CapturedAnimalDto>>;

public sealed class TransferCaptureCommandHandler(
    ICapturedAnimalRepository repository,
    IMunicipalSubscriptionService subscriptionService,
    IUnitOfWork unitOfWork,
    IEntitlementService? entitlementService = null)
    : IRequestHandler<TransferCaptureCommand, Result<CapturedAnimalDto>>
{
    public async Task<Result<CapturedAnimalDto>> Handle(
        TransferCaptureCommand request, CancellationToken ct)
    {
        if (!await subscriptionService.IsRedRegionalAsync(request.RequestingUserId, ct))
            return Result.Failure<CapturedAnimalDto>("Las transferencias entre municipalidades requieren el plan Red Regional.");

        if (entitlementService is not null)
        {
            var decision = await entitlementService.AuthorizeAsync(
                request.RequestingUserId, "InterCantonTransfersEnabled", 1m,
                new EntitlementContext("municipality-transfer", request.AnimalId), ct);
            if (!decision.Allowed)
                return Result.Failure<CapturedAnimalDto>("Las transferencias entre cantones no están incluidas en el plan.");
        }

        var animal = await repository.GetByIdAsync(request.AnimalId, ct);
        if (animal is null)
            return Result.Failure<CapturedAnimalDto>("Registro de animal no encontrado.");

        animal.UpdateStatus(CapturedAnimalStatus.Transferred);
        if (!string.IsNullOrWhiteSpace(request.TransferNotes))
            animal.AppendNote($"[Transferido a {request.DestinationCanton}] {request.TransferNotes}");

        repository.Update(animal);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(CapturedAnimalDto.FromDomain(animal));
    }
}
