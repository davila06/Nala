using FluentValidation;
using MediatR;
using PawTrack.Application.Clinics.DTOs;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Commands.UpdateClinicProfile;

public sealed record UpdateClinicProfileCommand(
    Guid UserId,
    string Name,
    string Address,
    string? PhoneNumber,
    string? Website,
    bool? IsEmergency24h,
    string? EmergencyPhone,
    string? Description = null,
    string? Services = null,
    string? OpeningHours = null,
    string? WhatsAppNumber = null,
    bool IsWhatsAppContactEnabled = false) : IRequest<Result<ClinicDto>>;

public sealed class UpdateClinicProfileCommandValidator
    : AbstractValidator<UpdateClinicProfileCommand>
{
    public UpdateClinicProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.PhoneNumber).MaximumLength(20);
        RuleFor(x => x.EmergencyPhone).MaximumLength(20);
        RuleFor(x => x.Website).MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Services).MaximumLength(2000);
        RuleFor(x => x.OpeningHours).MaximumLength(2000);
        RuleFor(x => x.WhatsAppNumber).MaximumLength(20);
    }
}

public sealed class UpdateClinicProfileCommandHandler(
    IClinicRepository clinicRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateClinicProfileCommand, Result<ClinicDto>>
{
    public async Task<Result<ClinicDto>> Handle(
        UpdateClinicProfileCommand request,
        CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (clinic is null)
            return Result.Failure<ClinicDto>("Clínica no encontrada o acceso denegado.");

        clinic.UpdateProfile(
            request.Name,
            request.Address,
            request.PhoneNumber,
            request.Website,
            request.IsEmergency24h,
            request.EmergencyPhone,
            request.Description,
            request.Services,
            request.OpeningHours);
        clinic.UpdateWhatsAppContact(request.WhatsAppNumber, request.IsWhatsAppContactEnabled);
        clinicRepository.Update(clinic);

        await auditLogRepository.AddAsync(
            AuditLogEntry.Create(
                request.UserId,
                AuditAction.ClinicProfileUpdated,
                "Clinic",
                clinic.Id.ToString(),
                "Clinic profile updated by owner."),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ClinicDto.FromDomain(clinic));
    }
}