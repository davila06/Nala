using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Payments.Commands.DeletePaymentProfile;

public sealed record DeletePaymentProfileCommand(Guid ProfileId, Guid RequestingUserId)
    : IRequest<Result<bool>>;

public sealed class DeletePaymentProfileCommandHandler(
    IUserPaymentProfileRepository profileRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePaymentProfileCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        DeletePaymentProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await profileRepository.GetByIdAsync(request.ProfileId, cancellationToken);
        if (profile is null)
            return Result.Failure<bool>("Payment profile not found.");

        if (profile.UserId != request.RequestingUserId)
            return Result.Failure<bool>("Access denied.");

        profileRepository.Delete(profile);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
