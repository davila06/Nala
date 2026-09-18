using FluentValidation;
using MediatR;
using PawTrack.Application.CastrationCampaigns.DTOs;
using PawTrack.Application.CastrationCampaigns.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Common;

namespace PawTrack.Application.CastrationCampaigns.Commands.OperateCastrationAppointment;

public enum CastrationAppointmentOperation { Confirm, CheckIn, Complete, NoShow }

public sealed record OperateCastrationAppointmentCommand(
    Guid AppointmentId, Guid RequestingUserId, CastrationAppointmentOperation Operation,
    Guid? VeterinarianId = null, string? ClinicalOutcome = null, string? PostOperativeInstructions = null)
    : IRequest<Result<CastrationAppointmentDto>>;

public sealed class OperateCastrationAppointmentCommandValidator : AbstractValidator<OperateCastrationAppointmentCommand>
{
    public OperateCastrationAppointmentCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
        When(x => x.Operation == CastrationAppointmentOperation.Complete, () =>
        {
            RuleFor(x => x.VeterinarianId).NotNull().NotEqual(Guid.Empty);
            RuleFor(x => x.ClinicalOutcome).NotEmpty().MaximumLength(1_000);
            RuleFor(x => x.PostOperativeInstructions).NotEmpty().MaximumLength(2_000);
        });
    }
}

public sealed class OperateCastrationAppointmentCommandHandler(
    ICastrationAppointmentRepository appointmentRepository,
    ICastrationCampaignRepository campaignRepository,
    IClinicRepository clinicRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<OperateCastrationAppointmentCommand, Result<CastrationAppointmentDto>>
{
    public async Task<Result<CastrationAppointmentDto>> Handle(OperateCastrationAppointmentCommand request, CancellationToken ct)
    {
        var appointment = await appointmentRepository.GetByIdAsync(request.AppointmentId, ct);
        if (appointment is null) return Result.Failure<CastrationAppointmentDto>("Appointment was not found.");
        var campaign = await campaignRepository.GetByIdAsync(appointment.CampaignId, ct);
        if (campaign is null) return Result.Failure<CastrationAppointmentDto>("Campaign was not found.");
        var user = await userRepository.GetByIdAsync(request.RequestingUserId, ct);
        var clinic = await clinicRepository.GetByUserIdAsync(request.RequestingUserId, ct);
        if (user is null || !user.Role.IsAdminOrSuperAdmin() && clinic?.Id != campaign.ExecutingClinicId)
            return Result.Failure<CastrationAppointmentDto>("Only the executing clinic or an administrator can operate this appointment.");

        try
        {
            switch (request.Operation)
            {
                case CastrationAppointmentOperation.Confirm: appointment.Confirm(); break;
                case CastrationAppointmentOperation.CheckIn: appointment.CheckIn(); break;
                case CastrationAppointmentOperation.NoShow: appointment.MarkNoShow(); break;
                case CastrationAppointmentOperation.Complete:
                    appointment.Complete(request.VeterinarianId!.Value, request.ClinicalOutcome!, request.PostOperativeInstructions!); break;
            }
        }
        catch (InvalidOperationException ex) { return Result.Failure<CastrationAppointmentDto>(ex.Message); }

        appointmentRepository.Update(appointment);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(CastrationAppointmentDto.FromDomain(appointment));
    }
}
