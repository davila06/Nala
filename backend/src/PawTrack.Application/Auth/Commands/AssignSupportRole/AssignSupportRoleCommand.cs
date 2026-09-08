using MediatR;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Auth;

namespace PawTrack.Application.Auth.Commands.AssignSupportRole;

public sealed record AssignSupportRoleCommand(Guid AdminUserId, Guid TargetUserId)
    : IRequest<Result<Unit>>;

public sealed class AssignSupportRoleCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AssignSupportRoleCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(AssignSupportRoleCommand request, CancellationToken ct)
    {
        var target = await userRepository.GetByIdAsync(request.TargetUserId, ct);
        if (target is null)
            return Result.Failure<Unit>("Usuario no encontrado.");
        if (target.Role == UserRole.Admin)
            return Result.Failure<Unit>("No se puede degradar ni reemplazar el rol de un administrador.");

        target.AssignSupportRole();
        userRepository.Update(target);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(Unit.Value);
    }
}
