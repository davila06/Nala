using MediatR;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Queries.GetCollarLostModeStatus;

public sealed record GetCollarLostModeStatusQuery(Guid CollarId, Guid RequestingUserId)
    : IRequest<Result<CollarLostModeStatusDto>>;

public sealed record CollarLostModeStatusDto(
    bool IsLost, DateTimeOffset? LostModeActivatedAt, Guid? LostPetEventId);

public sealed class GetCollarLostModeStatusQueryHandler(ICollarRepository collarRepository)
    : IRequestHandler<GetCollarLostModeStatusQuery, Result<CollarLostModeStatusDto>>
{
    public async Task<Result<CollarLostModeStatusDto>> Handle(
        GetCollarLostModeStatusQuery request, CancellationToken cancellationToken)
    {
        var collar = await collarRepository.GetByIdAsync(request.CollarId, cancellationToken);
        if (collar is null)
            return Result.Failure<CollarLostModeStatusDto>("Collar no encontrado.");

        if (collar.OwnerId != request.RequestingUserId)
            return Result.Failure<CollarLostModeStatusDto>("Access denied.");

        return Result.Success(new CollarLostModeStatusDto(
            collar.IsLost, collar.LostModeActivatedAt, collar.LostPetEventId));
    }
}
