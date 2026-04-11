using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<VerifyEmailCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        // The DB stores the SHA-256 hash of the token; hash the input before lookup.
        var tokenHash = User.ToHexHash(request.Token);

        var user = await userRepository.GetByEmailVerificationTokenAsync(tokenHash, cancellationToken);
        if (user is null)
            return Result.Failure<bool>("Invalid or expired verification token.");

        var verified = user.VerifyEmail(request.Token);
        if (!verified)
            return Result.Failure<bool>("Invalid or expired verification token.");

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
