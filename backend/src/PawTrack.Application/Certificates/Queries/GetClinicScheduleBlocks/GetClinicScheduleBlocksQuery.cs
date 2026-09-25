using MediatR;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Certificates.Queries.GetClinicScheduleBlocks;

public sealed record ClinicScheduleBlockDto(
    Guid BlockId,
    Guid ClinicId,
    Guid VeterinarianId,
    string VeterinarianName,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Reason);

public sealed record GetClinicScheduleBlocksQuery(
    Guid ClinicId,
    Guid RequestingUserId,
    DateTimeOffset From,
    DateTimeOffset To)
    : IRequest<Result<IReadOnlyList<ClinicScheduleBlockDto>>>;

public sealed class GetClinicScheduleBlocksQueryHandler(
    IClinicRepository clinicRepository,
    IVeterinarianScheduleBlockRepository blockRepository,
    IClinicVeterinarianRepository veterinarianRepository)
    : IRequestHandler<GetClinicScheduleBlocksQuery, Result<IReadOnlyList<ClinicScheduleBlockDto>>>
{
    public async Task<Result<IReadOnlyList<ClinicScheduleBlockDto>>> Handle(
        GetClinicScheduleBlocksQuery request,
        CancellationToken cancellationToken)
    {
        if (request.To <= request.From)
            return Result.Failure<IReadOnlyList<ClinicScheduleBlockDto>>("El rango de agenda es inválido.");

        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.RequestingUserId)
            return Result.Failure<IReadOnlyList<ClinicScheduleBlockDto>>("Acceso denegado.");

        var blocks = await blockRepository.GetForClinicAsync(request.ClinicId, request.From, request.To, cancellationToken);
        if (blocks.Count == 0)
            return Result.Success<IReadOnlyList<ClinicScheduleBlockDto>>([]);

        var veterinarians = (await veterinarianRepository.GetByClinicAsync(request.ClinicId, cancellationToken))
            .ToDictionary(veterinarian => veterinarian.Id, veterinarian => veterinarian.FullName);

        return Result.Success<IReadOnlyList<ClinicScheduleBlockDto>>(
            blocks.Select(block => new ClinicScheduleBlockDto(
                    block.Id,
                    block.ClinicId,
                    block.VeterinarianId,
                    veterinarians.GetValueOrDefault(block.VeterinarianId, "Veterinario"),
                    block.StartsAt,
                    block.EndsAt,
                    block.Reason))
                .ToList()
                .AsReadOnly());
    }
}
