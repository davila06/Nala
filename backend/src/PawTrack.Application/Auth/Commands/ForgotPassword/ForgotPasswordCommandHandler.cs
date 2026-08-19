using MediatR;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    ILogger<ForgotPasswordCommandHandler> logger)
    : IRequestHandler<ForgotPasswordCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // Anti-enumeration: always return success, regardless of account existence.
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            logger.LogWarning("Auth.ForgotPassword.EmailNotFound EmailHash={EmailHash}", PiiHelper.MaskEmail(request.Email));
            return Result.Success(true);
        }

        var rawResetToken = user.IssuePasswordResetToken();
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        _ = emailSender.SendPasswordResetAsync(
            to: user.Email,
            name: user.Name,
            resetToken: rawResetToken,
            cancellationToken: CancellationToken.None)
            .ContinueWith(t => logger.LogWarning(t.Exception,
                "Auth.ForgotPassword.EmailFailed UserId={UserId}", user.Id),
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

        logger.LogInformation("Auth.ForgotPassword.TokenIssued UserId={UserId}", user.Id);
        return Result.Success(true);
    }
}
