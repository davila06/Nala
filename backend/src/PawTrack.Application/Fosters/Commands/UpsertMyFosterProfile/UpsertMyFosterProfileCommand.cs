using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Fosters;
using PawTrack.Domain.Pets;

namespace PawTrack.Application.Fosters.Commands.UpsertMyFosterProfile;

public sealed record UpsertMyFosterProfileCommand(
    Guid UserId,
    double HomeLat,
    double HomeLng,
    IReadOnlyList<PetSpecies> AcceptedSpecies,
    string? SizePreference,
    int MaxDays,
    bool IsAvailable,
    DateTimeOffset? AvailableUntil)
    : IRequest<Result<bool>>;

public sealed class UpsertMyFosterProfileCommandHandler(
    IFosterVolunteerRepository fosterVolunteerRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpsertMyFosterProfileCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        UpsertMyFosterProfileCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<bool>("User not found.");

        var existing = await fosterVolunteerRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (existing is null)
        {
            var created = FosterVolunteer.Create(
                request.UserId,
                user.Name,
                request.HomeLat,
                request.HomeLng,
                request.AcceptedSpecies,
                request.SizePreference,
                request.MaxDays,
                request.IsAvailable,
                request.AvailableUntil);

            await fosterVolunteerRepository.AddAsync(created, cancellationToken);
        }
        else
        {
            existing.UpdateProfile(
                user.Name,
                request.HomeLat,
                request.HomeLng,
                request.AcceptedSpecies,
                request.SizePreference,
                request.MaxDays,
                request.IsAvailable,
                request.AvailableUntil);

            fosterVolunteerRepository.Update(existing);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}
