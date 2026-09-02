using MediatR;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Commands.CreateCollarSafeZone;

public sealed record CreateCollarSafeZoneCommand(Guid CollarId, Guid OwnerId, string Name, string PolygonJson)
    : IRequest<Result<CollarSafeZoneDto>>;

public sealed record CollarSafeZoneDto(
    Guid Id, Guid CollarId, string Name, string PolygonJson, bool Enabled, DateTimeOffset CreatedAt)
{
    public static CollarSafeZoneDto FromDomain(CollarSafeZone z) =>
        new(z.Id, z.CollarId, z.Name, z.PolygonJson, z.Enabled, z.CreatedAt);
}

public sealed class CreateCollarSafeZoneCommandHandler(
    ICollarRepository collarRepository,
    ICollarSafeZoneRepository safeZoneRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCollarSafeZoneCommand, Result<CollarSafeZoneDto>>
{
    public async Task<Result<CollarSafeZoneDto>> Handle(
        CreateCollarSafeZoneCommand request, CancellationToken cancellationToken)
    {
        var collar = await collarRepository.GetByIdAsync(request.CollarId, cancellationToken);
        if (collar is null || !collar.IsActive)
            return Result.Failure<CollarSafeZoneDto>("Collar no encontrado o inactivo.");

        if (collar.OwnerId != request.OwnerId)
            return Result.Failure<CollarSafeZoneDto>("Access denied.");

        CollarSafeZone zone;
        try { zone = CollarSafeZone.Create(request.CollarId, request.Name, request.PolygonJson); }
        catch (ArgumentException ex) { return Result.Failure<CollarSafeZoneDto>(ex.Message); }

        await safeZoneRepository.AddAsync(zone, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(CollarSafeZoneDto.FromDomain(zone));
    }
}
