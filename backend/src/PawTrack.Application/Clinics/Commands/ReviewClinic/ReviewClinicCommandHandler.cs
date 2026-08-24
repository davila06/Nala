using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Commands.ReviewClinic;

public sealed class ReviewClinicCommandHandler(
    IClinicRepository clinicRepository,
    IAuditLogRepository auditLog,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReviewClinicCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        ReviewClinicCommand request,
        CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null)
            return Result.Failure<bool>(["Clínica no encontrada."]);

        if (request.Approve)
            clinic.Activate();
        else
            clinic.Suspend();

        clinicRepository.Update(clinic);

        await auditLog.AddAsync(AuditLogEntry.Create(
            Guid.Empty,
            request.Approve ? AuditAction.ClinicApproved : AuditAction.ClinicRejected,
            "Clinic", request.ClinicId.ToString()), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
