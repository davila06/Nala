using MediatR;
using PawTrack.Application.Common.Behaviors;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Commands.SelectActiveClinicSite;

[BypassClinicActiveSite]
public sealed record SelectActiveClinicSiteCommand(Guid UserId, Guid SessionId, Guid ClinicId)
    : IRequest<Result<bool>>;

public sealed class SelectActiveClinicSiteCommandHandler(
    IClinicSiteAccessRepository siteAccessRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SelectActiveClinicSiteCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(SelectActiveClinicSiteCommand request, CancellationToken cancellationToken)
    {
        var selected = await siteAccessRepository.SetActiveClinicIdAsync(
            request.UserId, request.SessionId, request.ClinicId, cancellationToken);
        if (!selected)
            return Result.Failure<bool>("La sede no está autorizada para esta sesión.");

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}
