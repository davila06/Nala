using MediatR;
using PawTrack.Application.Collars.DTOs;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Queries.GetCollarStatus;

public sealed record GetCollarStatusQuery(Guid PetId, Guid RequestingUserId) : IRequest<Result<CollarDto?>>;

public sealed class GetCollarStatusQueryHandler(ICollarRepository collarRepository, IPetRepository petRepository)
    : IRequestHandler<GetCollarStatusQuery, Result<CollarDto?>>
{
    public async Task<Result<CollarDto?>> Handle(
        GetCollarStatusQuery request,
        CancellationToken cancellationToken)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null || pet.OwnerId != request.RequestingUserId)
            return Result.Failure<CollarDto?>("Access denied.");

        var collar = await collarRepository.GetActiveForPetAsync(request.PetId, cancellationToken);
        return Result.Success<CollarDto?>(collar is null ? null : CollarDto.FromDomain(collar));
    }
}
