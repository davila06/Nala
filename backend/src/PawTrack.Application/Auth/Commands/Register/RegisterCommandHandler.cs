using MediatR;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    ILogger<RegisterCommandHandler> logger)
    : IRequestHandler<RegisterCommand, Result<string>>
{
    public async Task<Result<string>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Anti-enumeration: always return the same success shape regardless of
        // whether the email is already taken. The caller cannot distinguish a
        // new registration from a duplicate — they must check their inbox.
        if (await userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            logger.LogWarning("Auth.Register.DuplicateEmail EmailHash={EmailHash}",
                PiiHelper.MaskEmail(request.Email));
            return Result.Success(string.Empty);
        }

        var passwordHash = passwordHasher.Hash(request.Password);
        var (user, rawVerificationToken) = User.Create(request.Email, passwordHash, request.Name);

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailSender.SendEmailVerificationAsync(
            to: user.Email,
            name: user.Name,
            verificationToken: rawVerificationToken,
            cancellationToken: cancellationToken);

        logger.LogInformation("Auth.Register.Success UserId={UserId}", user.Id);

        return Result.Success(user.Id.ToString());
    }
}
