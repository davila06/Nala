using MediatR;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Certificates.Commands.SetVeterinarianPermissions;

public sealed record SetVeterinarianPermissionsCommand(
    Guid ClinicId, Guid RequestingUserId, Guid VeterinarianId, IReadOnlyList<string> Permissions)
    : IRequest<Result<bool>>;

public sealed class SetVeterinarianPermissionsCommandHandler(
    IClinicRepository clinicRepository,
    IClinicVeterinarianRepository veterinarianRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SetVeterinarianPermissionsCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(SetVeterinarianPermissionsCommand request, CancellationToken ct)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, ct);
        if (clinic is null || clinic.UserId != request.RequestingUserId)
            return Result.Failure<bool>("Acceso denegado.");
        var veterinarian = await veterinarianRepository.GetByIdAsync(request.VeterinarianId, ct);
        if (veterinarian is null || veterinarian.ClinicId != request.ClinicId)
            return Result.Failure<bool>("Veterinario no encontrado.");
        var result = veterinarian.SetPermissions(request.RequestingUserId, request.Permissions);
        if (result.IsFailure) return result;
        veterinarianRepository.Update(veterinarian);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(true);
    }
}