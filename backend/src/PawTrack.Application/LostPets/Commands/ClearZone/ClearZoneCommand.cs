using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.LostPets.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.LostPets.Commands.ClearZone;

/// <summary>Marks a <see cref="SearchZone"/> as fully searched. Only the volunteer who claimed it may clear it.</summary>
public sealed record ClearZoneCommand(Guid ZoneId, Guid UserId) : IRequest<Result<SearchZoneDto>>;

public sealed class ClearZoneCommandHandler(
    ISearchZoneRepository searchZoneRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ClearZoneCommand, Result<SearchZoneDto>>
{
    public async Task<Result<SearchZoneDto>> Handle(
        ClearZoneCommand request,
        CancellationToken cancellationToken)
    {
        var zone = await searchZoneRepository.GetByIdAsync(request.ZoneId, cancellationToken);

        if (zone is null)
            return Result.Failure<SearchZoneDto>("Zone not found.");

        if (!zone.TryClear(request.UserId))
            return Result.Failure<SearchZoneDto>("Zone cannot be cleared. It must be taken by you.");

        searchZoneRepository.Update(zone);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SearchZoneDto.FromDomain(zone));
    }
}
