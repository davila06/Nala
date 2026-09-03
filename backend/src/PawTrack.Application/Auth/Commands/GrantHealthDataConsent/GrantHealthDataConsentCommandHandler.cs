using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.GrantHealthDataConsent;

public sealed class GrantHealthDataConsentCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GrantHealthDataConsentCommand, Result<DateTimeOffset>>
{
    public async Task<Result<DateTimeOffset>> Handle(
        GrantHealthDataConsentCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<DateTimeOffset>("Usuario no encontrado.");

        user.GrantHealthDataConsent();
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(user.HealthDataConsentedAt!.Value);
    }
}
