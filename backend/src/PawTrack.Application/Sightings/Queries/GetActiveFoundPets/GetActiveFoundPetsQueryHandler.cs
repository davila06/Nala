using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Sightings.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Sightings.Queries.GetActiveFoundPets;

public sealed class GetActiveFoundPetsQueryHandler(IFoundPetRepository foundPetRepository)
    : IRequestHandler<GetActiveFoundPetsQuery, Result<IReadOnlyList<FoundPetReportDto>>>
{
    public async Task<Result<IReadOnlyList<FoundPetReportDto>>> Handle(
        GetActiveFoundPetsQuery request, CancellationToken cancellationToken)
    {
        var maxResults = Math.Clamp(request.MaxResults, 1, 100);
        var reports = await foundPetRepository.GetOpenReportsAsync(maxResults, cancellationToken);

        var dtos = reports
            .Select(r => new FoundPetReportDto(
                r.Id,
                r.FoundSpecies.ToString(),
                r.BreedEstimate,
                r.ColorDescription,
                r.SizeEstimate,
                r.FoundLat,
                r.FoundLng,
                r.PhotoUrl,
                r.Note,
                r.Status.ToString(),
                r.MatchScore,
                r.ReportedAt))
            .ToList();

        return Result.Success<IReadOnlyList<FoundPetReportDto>>(dtos);
    }
}
