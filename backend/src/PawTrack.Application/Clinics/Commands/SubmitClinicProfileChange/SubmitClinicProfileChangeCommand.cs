using System.Text.Json;
using FluentValidation;
using MediatR;
using PawTrack.Application.Clinics.DTOs;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Commands.SubmitClinicProfileChange;

public sealed record SubmitClinicProfileChangeCommand(
    Guid ClinicId,
    Guid RequestingUserId,
    string Name,
    string Address,
    string? PhoneNumber,
    string? Website,
    bool? IsEmergency24h,
    string? EmergencyPhone,
    string? Description,
    string? Services,
    string? OpeningHours) : IRequest<Result<ClinicProfileChangeDto>>;

public sealed record ClinicProfileChangeDto(
    Guid Id, Guid ClinicId, string Status, DateTimeOffset CreatedAt,
    DateTimeOffset? ReviewedAt, string? ReviewReason);

public sealed class SubmitClinicProfileChangeCommandValidator : AbstractValidator<SubmitClinicProfileChangeCommand>
{
    public SubmitClinicProfileChangeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.PhoneNumber).MaximumLength(20);
        RuleFor(x => x.Website).MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Services).MaximumLength(2000);
        RuleFor(x => x.OpeningHours).MaximumLength(2000);
    }
}

public sealed class SubmitClinicProfileChangeCommandHandler(
    IClinicRepository clinicRepository,
    IClinicProfileChangeRepository changeRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SubmitClinicProfileChangeCommand, Result<ClinicProfileChangeDto>>
{
    public async Task<Result<ClinicProfileChangeDto>> Handle(SubmitClinicProfileChangeCommand request, CancellationToken ct)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, ct);
        if (clinic is null || clinic.UserId != request.RequestingUserId)
            return Result.Failure<ClinicProfileChangeDto>("Acceso denegado.");

        var json = JsonSerializer.Serialize(new
        {
            request.Name,
            request.Address,
            request.PhoneNumber,
            request.Website,
            request.IsEmergency24h,
            request.EmergencyPhone,
            request.Description,
            request.Services,
            request.OpeningHours,
        });
        var change = ClinicProfileChange.Submit(clinic.Id, request.RequestingUserId, json);
        await changeRepository.AddAsync(change, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(ToDto(change));
    }

    public static ClinicProfileChangeDto ToDto(ClinicProfileChange change) =>
        new(change.Id, change.ClinicId, change.Status.ToString(), change.CreatedAt, change.ReviewedAt, change.ReviewReason);
}