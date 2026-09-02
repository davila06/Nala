using MediatR;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Common;
using System.Security.Cryptography;

namespace PawTrack.Application.Collars.Commands.ActivateCollarTag;

public sealed record ActivateCollarTagCommand(
    string Serial,
    Guid PetId,
    Guid OwnerId) : IRequest<Result<ActivateCollarTagResultDto>>;

/// <param name="CollarApiKey">Raw key shown once — never persisted in plain text.</param>
public sealed record ActivateCollarTagResultDto(Guid CollarId, string Serial, string CollarApiKey);

public sealed class ActivateCollarTagCommandHandler(
    ICollarTagRepository collarTagRepository,
    ICollarDeviceCredentialRepository credentialRepository,
    ICollarRepository collarRepository,
    ICollarAuditRepository auditRepository,
    IPetRepository petRepository,
    ISubscriptionService subscriptionService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ActivateCollarTagCommand, Result<ActivateCollarTagResultDto>>
{
    public async Task<Result<ActivateCollarTagResultDto>> Handle(
        ActivateCollarTagCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify serial exists and is available
        var tag = await collarTagRepository.GetBySerialAsync(request.Serial.ToUpperInvariant(), cancellationToken);
        if (tag is null)
            return Result.Failure<ActivateCollarTagResultDto>("Serial no encontrado.");
        if (!tag.IsAvailable)
            return Result.Failure<ActivateCollarTagResultDto>($"El CollarTag ya está {tag.Status}.");

        // 2. Verify pet ownership
        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null || pet.OwnerId != request.OwnerId)
            return Result.Failure<ActivateCollarTagResultDto>("Access denied.");

        // 3. Verify Plus plan
        var isPlus = await subscriptionService.IsAtLeastPlusAsync(request.OwnerId, cancellationToken);
        if (!isPlus)
            return Result.Failure<ActivateCollarTagResultDto>("El CollarTag requiere el plan Plus.");

        // 4. Deactivate previous active collar for this pet if any
        var existing = await collarRepository.GetActiveForPetAsync(request.PetId, cancellationToken);
        if (existing is not null)
        {
            existing.Deactivate();
            collarRepository.Update(existing);

            // Revoke all credentials of the previous collar
            var oldCredentials = await credentialRepository.GetForCollarAsync(existing.Id, cancellationToken);
            foreach (var old in oldCredentials.Where(c => c.IsUsable))
            {
                old.Revoke();
                credentialRepository.Update(old);
            }
        }

        // 5. Create Collar with Provider.Own
        var collar = Collar.Register(request.PetId, request.OwnerId, CollarProvider.Own, externalDeviceId: null);

        // 6. Link serial to the new Collar
        collar.SetTagSerial(tag.Serial);
        await collarRepository.AddAsync(collar, cancellationToken);

        // 7. Activate the CollarTag entity
        tag.Activate(collar.Id);
        collarTagRepository.Update(tag);

        // 8. Generate CollarDeviceCredential — raw key returned once, only hash persisted
        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var rawKey = "ptwk_collar_" + Convert.ToBase64String(rawBytes)
            .Replace("+", "-").Replace("/", "_").Replace("=", "");
        var keyHash = CollarDeviceKeyHasher.Compute(rawKey);
        var credential = CollarDeviceCredential.Create(collar.Id, keyHash);
        await credentialRepository.AddAsync(credential, cancellationToken);

        // 9. Audit trail — links the serial's pre-activation history to the new Collar
        await auditRepository.AddAsync(
            CollarAuditEntry.Create(
                CollarAuditEvent.Activated,
                $"Vinculado a mascota {request.PetId}",
                collarId: collar.Id, serial: tag.Serial, userId: request.OwnerId),
            cancellationToken);

        // 10. Commit everything atomically
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new ActivateCollarTagResultDto(collar.Id, tag.Serial, rawKey));
    }
}
