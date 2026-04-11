using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ResetPasswordCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = User.ToHexHash(request.Token);

        var user = await userRepository.GetByPasswordResetTokenAsync(tokenHash, cancellationToken);
        if (user is null)
            return Result.Failure<bool>("Invalid or expired reset token.");

        var passwordHash = passwordHasher.Hash(request.NewPassword);
        var reset = user.ResetPassword(request.Token, passwordHash);
        if (!reset)
            return Result.Failure<bool>("Invalid or expired reset token.");

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
