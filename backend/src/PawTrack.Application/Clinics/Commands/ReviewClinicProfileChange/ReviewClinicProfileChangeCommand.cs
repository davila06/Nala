using System.Text.Json;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Commands.ReviewClinicProfileChange;

public sealed record ReviewClinicProfileChangeCommand(
    Guid ChangeId, Guid AdminUserId, bool Approve, string? Reason)
    : IRequest<Result<bool>>;

public sealed class ReviewClinicProfileChangeCommandHandler(
    IClinicProfileChangeRepository changeRepository,
    IClinicRepository clinicRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReviewClinicProfileChangeCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ReviewClinicProfileChangeCommand request, CancellationToken ct)
    {
        var change = await changeRepository.GetByIdAsync(request.ChangeId, ct);
        if (change is null) return Result.Failure<bool>("Cambio no encontrado.");
        if (!request.Approve)
        {
            var rejected = change.Reject(request.AdminUserId, request.Reason ?? string.Empty);
            if (rejected.IsFailure) return rejected;
            changeRepository.Update(change);
            await unitOfWork.SaveChangesAsync(ct);
            return Result.Success(true);
        }

        var clinic = await clinicRepository.GetByIdAsync(change.ClinicId, ct);
        if (clinic is null) return Result.Failure<bool>("Clínica no encontrada.");
        var proposed = JsonSerializer.Deserialize<ClinicProfilePayload>(change.ProposedProfileJson);
        if (proposed is null) return Result.Failure<bool>("Propuesta inválida.");
        clinic.UpdateProfile(proposed.Name, proposed.Address, proposed.PhoneNumber, proposed.Website,
            proposed.IsEmergency24h, proposed.EmergencyPhone, proposed.Description, proposed.Services, proposed.OpeningHours);
        var approved = change.Approve(request.AdminUserId, request.Reason);
        if (approved.IsFailure) return approved;
        clinicRepository.Update(clinic);
        changeRepository.Update(change);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(true);
    }

    private sealed record ClinicProfilePayload(
        string Name, string Address, string? PhoneNumber, string? Website,
        bool? IsEmergency24h, string? EmergencyPhone, string? Description,
        string? Services, string? OpeningHours);
}