using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Municipalities.DTOs;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Municipalities;

namespace PawTrack.Application.Municipalities.Commands.BulkUpdateStatus;

// ── Command — Full+ ───────────────────────────────────────────────────────────

public sealed record BulkUpdateStatusCommand(
    Guid RequestingUserId,
    IReadOnlyList<Guid> AnimalIds,
    CapturedAnimalStatus NewStatus,
    Guid? MatchedPetId = null) : IRequest<Result<BulkUpdateResultDto>>;

public sealed record BulkUpdateResultDto(int Updated, int NotFound);

public sealed class BulkUpdateStatusCommandHandler(
    ICapturedAnimalRepository repository,
    IMunicipalSubscriptionService subscriptionService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<BulkUpdateStatusCommand, Result<BulkUpdateResultDto>>
{
    public async Task<Result<BulkUpdateResultDto>> Handle(
        BulkUpdateStatusCommand request, CancellationToken ct)
    {
        if (!await subscriptionService.IsFullOrAboveAsync(request.RequestingUserId, ct))
            return Result.Failure<BulkUpdateResultDto>("La actualización masiva requiere el plan Full o Red Regional.");

        int updated = 0, notFound = 0;

        foreach (var id in request.AnimalIds)
        {
            var animal = await repository.GetByIdAsync(id, ct);
            if (animal is null) { notFound++; continue; }

            animal.UpdateStatus(request.NewStatus);
            if (request.MatchedPetId.HasValue) animal.LinkToPet(request.MatchedPetId.Value);
            repository.Update(animal);
            updated++;
        }

        if (updated > 0) await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(new BulkUpdateResultDto(updated, notFound));
    }
}
