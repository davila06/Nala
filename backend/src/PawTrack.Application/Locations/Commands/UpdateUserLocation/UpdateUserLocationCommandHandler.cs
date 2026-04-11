using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Locations;

namespace PawTrack.Application.Locations.Commands.UpdateUserLocation;

public sealed class UpdateUserLocationCommandHandler(
    IUserLocationRepository userLocationRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateUserLocationCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        UpdateUserLocationCommand request, CancellationToken cancellationToken)
    {
        var existing = await userLocationRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (existing is null)
        {
            var newLocation = UserLocation.Create(
                request.UserId,
                request.Lat,
                request.Lng,
                request.ReceiveNearbyAlerts,
                request.QuietHoursStart,
                request.QuietHoursEnd);

            await userLocationRepository.UpsertAsync(newLocation, cancellationToken);
        }
        else
        {
            existing.Update(request.Lat, request.Lng, request.ReceiveNearbyAlerts);
            existing.SetQuietHours(request.QuietHoursStart, request.QuietHoursEnd);
            await userLocationRepository.UpsertAsync(existing, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}
