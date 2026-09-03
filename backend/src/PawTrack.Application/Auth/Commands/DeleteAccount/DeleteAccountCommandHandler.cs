using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.DeleteAccount;

public sealed class DeleteAccountCommandHandler(
    IUserRepository userRepository,
    IPetRepository petRepository,
    IBlobStorageService blobStorage,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAccountCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result.Failure<bool>("User not found.");

        // Confirm password before irreversible action
        if (!passwordHasher.Verify(request.ConfirmPassword, user.PasswordHash))
            return Result.Failure<bool>("Password confirmation is incorrect.");

        // Delete all pet photos from Blob Storage before soft-deleting the account
        var pets = await petRepository.GetByOwnerIdAsync(request.UserId, cancellationToken);
        foreach (var pet in pets)
        {
            if (!string.IsNullOrEmpty(pet.PhotoUrl))
                await blobStorage.DeleteAsync(pet.PhotoUrl, cancellationToken);

            petRepository.Delete(pet);
        }

        user.SoftDelete();
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
