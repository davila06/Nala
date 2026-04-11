using MediatR;
using PawTrack.Application.Allies.DTOs;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Allies.Commands.ReviewAllyApplication;

public sealed class ReviewAllyApplicationCommandHandler(
    IAllyProfileRepository allyProfileRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReviewAllyApplicationCommand, Result<AllyProfileDto>>
{
    public async Task<Result<AllyProfileDto>> Handle(
        ReviewAllyApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<AllyProfileDto>("User not found.");

        var profile = await allyProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile is null)
            return Result.Failure<AllyProfileDto>("Ally application not found.");

        if (request.Approve)
        {
            profile.Approve();
            user.PromoteToAlly();
            userRepository.Update(user);
        }
        else
        {
            profile.Reject();
        }

        allyProfileRepository.Update(profile);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(AllyProfileDto.FromDomain(profile));
    }
}