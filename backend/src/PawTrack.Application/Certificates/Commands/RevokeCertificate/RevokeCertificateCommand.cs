using FluentValidation;
using MediatR;
using PawTrack.Application.Certificates.DTOs;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Certificates.Commands.RevokeCertificate;

public sealed record RevokeCertificateCommand(
    Guid CertificateId,
    Guid RequestingUserId,
    bool IsAdmin,
    string Reason) : IRequest<Result<CertificateDto>>;

public sealed class RevokeCertificateCommandValidator : AbstractValidator<RevokeCertificateCommand>
{
    public RevokeCertificateCommandValidator()
    {
        RuleFor(command => command.CertificateId).NotEmpty();
        RuleFor(command => command.RequestingUserId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(300);
    }
}

public sealed class RevokeCertificateCommandHandler(
    ICertificateRepository certificateRepository,
    IClinicRepository clinicRepository,
    ICertificateAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RevokeCertificateCommand, Result<CertificateDto>>
{
    public async Task<Result<CertificateDto>> Handle(
        RevokeCertificateCommand request,
        CancellationToken cancellationToken)
    {
        var certificate = await certificateRepository.GetByIdAsync(request.CertificateId, cancellationToken);
        if (certificate is null)
            return Result.Failure<CertificateDto>("Certificado no encontrado.");

        if (!request.IsAdmin)
        {
            var clinic = await clinicRepository.GetByIdAsync(certificate.ClinicId, cancellationToken);
            if (clinic is null || clinic.UserId != request.RequestingUserId)
                return Result.Failure<CertificateDto>("Acceso denegado.");
        }

        var revokeResult = certificate.Revoke(request.RequestingUserId, request.Reason);
        if (revokeResult.IsFailure)
            return Result.Failure<CertificateDto>(revokeResult.Errors);

        certificateRepository.Update(certificate);
        await auditLogRepository.AddAsync(
            CertificateAuditLog.Create(certificate.Id, CertificateAuditAction.Revoked, request.RequestingUserId, request.Reason),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CertificateDto.FromDomain(certificate));
    }
}
