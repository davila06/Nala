using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.LostPets.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.LostPets.Commands.ReleaseZone;

/// <summary>Releases a <see cref="SearchZone"/> back to <c>Free</c>. Only the assigned volunteer may release it.</summary>
public sealed record ReleaseZoneCommand(Guid ZoneId, Guid UserId) : IRequest<Result<SearchZoneDto>>;

public sealed class ReleaseZoneCommandHandler(
    ISearchZoneRepository searchZoneRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReleaseZoneCommand, Result<SearchZoneDto>>
{
    public async Task<Result<SearchZoneDto>> Handle(
        ReleaseZoneCommand request,
        CancellationToken cancellationToken)
    {
        var zone = await searchZoneRepository.GetByIdAsync(request.ZoneId, cancellationToken);

        if (zone is null)
            return Result.Failure<SearchZoneDto>("Zone not found.");

        if (!zone.TryRelease(request.UserId))
            return Result.Failure<SearchZoneDto>("Zone cannot be released. It must be taken by you.");

        searchZoneRepository.Update(zone);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SearchZoneDto.FromDomain(zone));
    }
}
