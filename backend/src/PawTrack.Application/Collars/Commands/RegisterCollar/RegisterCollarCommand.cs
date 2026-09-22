using MediatR;
using PawTrack.Application.Collars.DTOs;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Commands.RegisterCollar;

public sealed record RegisterCollarCommand(
    Guid PetId,
    Guid OwnerId,
    CollarProvider Provider,
    string? ExternalDeviceId) : IRequest<Result<CollarDto>>;

public sealed class RegisterCollarCommandHandler(
    ICollarRepository collarRepository,
    IPetRepository petRepository,
    ISubscriptionService subscriptionService,
    IUnitOfWork unitOfWork,
    IEntitlementService? entitlementService = null)
    : IRequestHandler<RegisterCollarCommand, Result<CollarDto>>
{
    public async Task<Result<CollarDto>> Handle(
        RegisterCollarCommand request,
        CancellationToken cancellationToken)
    {
        // Verify the requesting user owns the pet before allowing collar registration
        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null || pet.OwnerId != request.OwnerId)
            return Result.Failure<CollarDto>("Access denied.");

        var isPlus = await subscriptionService.IsAtLeastPlusAsync(request.OwnerId, cancellationToken);
        if (!isPlus)
            return Result.Failure<CollarDto>("El collar GPS requiere el plan Plus.");

        if (entitlementService is not null)
        {
            var decision = await entitlementService.AuthorizeAsync(
                request.OwnerId,
                "MaxGpsCollars",
                1m,
                new EntitlementContext("gps-collar", request.OwnerId),
                cancellationToken);
            var activeCount = await collarRepository.CountActiveByOwnerAsync(request.OwnerId, cancellationToken);
            var existingForPet = await collarRepository.GetActiveForPetAsync(request.PetId, cancellationToken);
            if (existingForPet is null &&
                ((!decision.Allowed) ||
                 (decision.Limit.HasValue && activeCount >= decision.Limit.Value)))
                return Result.Failure<CollarDto>("La cuenta alcanzó el límite de collares GPS de su plan.");
        }

        // Deactivate any existing active collar for this pet
        var existing = await collarRepository.GetActiveForPetAsync(request.PetId, cancellationToken);
        if (existing is not null)
        {
            existing.Deactivate();
            collarRepository.Update(existing);
        }

        var collar = Collar.Register(request.PetId, request.OwnerId, request.Provider, request.ExternalDeviceId);
        await collarRepository.AddAsync(collar, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CollarDto.FromDomain(collar));
    }
}
