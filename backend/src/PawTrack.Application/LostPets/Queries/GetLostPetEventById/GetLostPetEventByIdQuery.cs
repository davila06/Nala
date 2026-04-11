using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.LostPets.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.LostPets.Queries.GetLostPetEventById;

public sealed record GetLostPetEventByIdQuery(
    Guid LostPetEventId,
    Guid RequestingUserId) : IRequest<Result<LostPetEventDto>>;

public sealed class GetLostPetEventByIdQueryHandler(
    ILostPetRepository lostPetRepository)
    : IRequestHandler<GetLostPetEventByIdQuery, Result<LostPetEventDto>>
{
    public async Task<Result<LostPetEventDto>> Handle(
        GetLostPetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var report = await lostPetRepository.GetByIdAsync(request.LostPetEventId, cancellationToken);

        if (report is null)
            return Result.Failure<LostPetEventDto>("Lost pet report not found.");

        if (report.OwnerId != request.RequestingUserId)
            return Result.Failure<LostPetEventDto>("Access denied.");

        return Result.Success(LostPetEventDto.FromDomain(report));
    }
}
