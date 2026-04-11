using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.LostPets.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.LostPets.Commands.ClaimZone;

/// <summary>Claims a <see cref="SearchZone"/> for the requesting user. Fails if already taken.</summary>
public sealed record ClaimZoneCommand(Guid ZoneId, Guid UserId) : IRequest<Result<SearchZoneDto>>;

public sealed class ClaimZoneCommandHandler(
    ISearchZoneRepository searchZoneRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ClaimZoneCommand, Result<SearchZoneDto>>
{
    public async Task<Result<SearchZoneDto>> Handle(
        ClaimZoneCommand request,
        CancellationToken cancellationToken)
    {
        var zone = await searchZoneRepository.GetByIdAsync(request.ZoneId, cancellationToken);

        if (zone is null)
            return Result.Failure<SearchZoneDto>("Zone not found.");

        if (!zone.TryClaim(request.UserId))
            return Result.Failure<SearchZoneDto>("Zone is not available.");

        searchZoneRepository.Update(zone);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SearchZoneDto.FromDomain(zone));
    }
}
