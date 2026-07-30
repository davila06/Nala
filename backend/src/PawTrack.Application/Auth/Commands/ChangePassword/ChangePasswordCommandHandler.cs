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

        var currentHash = passwordHasher.Hash(request.CurrentPassword);
        var result = user.ChangePassword(currentHash, passwordHasher.Hash(request.NewPassword));

        if (result.IsFailure)
            return result;

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
