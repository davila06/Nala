using MediatR;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Certificates.Queries.GetClinicAgenda;

public sealed record ClinicAgendaItemDto(
    Guid AppointmentId,
    Guid ClinicId,
    Guid VeterinarianId,
    string VeterinarianName,
    Guid PetId,
    string PetName,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    VeterinarianAppointmentStatus Status);

public sealed record GetClinicAgendaQuery(
    Guid ClinicId,
    Guid RequestingUserId,
    DateTimeOffset From,
    DateTimeOffset To)
    : IRequest<Result<IReadOnlyList<ClinicAgendaItemDto>>>;

public sealed class GetClinicAgendaQueryHandler(
    IClinicRepository clinicRepository,
    IVeterinarianAppointmentRepository appointmentRepository,
    IPetRepository petRepository,
    IClinicVeterinarianRepository veterinarianRepository)
    : IRequestHandler<GetClinicAgendaQuery, Result<IReadOnlyList<ClinicAgendaItemDto>>>
{
    public async Task<Result<IReadOnlyList<ClinicAgendaItemDto>>> Handle(
        GetClinicAgendaQuery request,
        CancellationToken cancellationToken)
    {
        if (request.To <= request.From)
            return Result.Failure<IReadOnlyList<ClinicAgendaItemDto>>("El rango de agenda es inválido.");

        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.RequestingUserId)
            return Result.Failure<IReadOnlyList<ClinicAgendaItemDto>>("Acceso denegado.");

        var appointments = await appointmentRepository.GetForClinicAsync(
            request.ClinicId,
            request.From,
            request.To,
            cancellationToken);

        if (appointments.Count == 0)
            return Result.Success<IReadOnlyList<ClinicAgendaItemDto>>([]);

        var pets = (await petRepository.GetByIdsAsync(
                appointments.Select(appointment => appointment.PetId).Distinct(),
                cancellationToken))
            .ToDictionary(pet => pet.Id, pet => pet.Name);

        var veterinarians = (await veterinarianRepository.GetByClinicAsync(
                request.ClinicId,
                cancellationToken))
            .ToDictionary(veterinarian => veterinarian.Id, veterinarian => veterinarian.FullName);

        var result = appointments
            .Select(appointment => new ClinicAgendaItemDto(
                appointment.Id,
                appointment.ClinicId,
                appointment.VeterinarianId,
                veterinarians.GetValueOrDefault(appointment.VeterinarianId, "Veterinario"),
                appointment.PetId,
                pets.GetValueOrDefault(appointment.PetId, "Mascota"),
                appointment.StartsAt,
                appointment.EndsAt,
                appointment.Status))
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<ClinicAgendaItemDto>>(result);
    }
}
