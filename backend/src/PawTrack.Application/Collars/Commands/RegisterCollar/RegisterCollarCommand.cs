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
    ISubscriptionService subscriptionService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterCollarCommand, Result<CollarDto>>
{
    public async Task<Result<CollarDto>> Handle(
        RegisterCollarCommand request,
        CancellationToken cancellationToken)
    {
        var isPlus = await subscriptionService.IsAtLeastPlusAsync(request.OwnerId, cancellationToken);
        if (!isPlus)
            return Result.Failure<CollarDto>("El collar GPS requiere el plan Plus.");

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
