using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ChangePasswordCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result.Failure<bool>("User not found.");

        // bcrypt is salted — must Verify against stored hash, not hash-and-compare
        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.Failure<bool>("Current password is incorrect.");

        // Pass stored hash as "confirmation token" so domain guard passes, then set new hash
        var newHash = passwordHasher.Hash(request.NewPassword);
        var result = user.ChangePassword(user.PasswordHash, newHash);

        if (result.IsFailure)
            return result;

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
