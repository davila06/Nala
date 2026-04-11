using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Fosters.DTOs;
using PawTrack.Domain.Common;
using PawTrack.Domain.Pets;
using System.Globalization;

namespace PawTrack.Application.Fosters.Queries.GetFosterSuggestions;

public sealed record GetFosterSuggestionsQuery(
    Guid FoundPetReportId,
    int MaxResults = 3)
    : IRequest<Result<IReadOnlyList<FosterSuggestionDto>>>;

public sealed class GetFosterSuggestionsQueryHandler(
    IFosterVolunteerRepository fosterVolunteerRepository,
    IFoundPetRepository foundPetRepository)
    : IRequestHandler<GetFosterSuggestionsQuery, Result<IReadOnlyList<FosterSuggestionDto>>>
{
    private const int RadiusMetres = 3000;

    public async Task<Result<IReadOnlyList<FosterSuggestionDto>>> Handle(
        GetFosterSuggestionsQuery request,
        CancellationToken cancellationToken)
    {
        var foundReport = await foundPetRepository.GetByIdAsync(request.FoundPetReportId, cancellationToken);
        if (foundReport is null)
            return Result.Failure<IReadOnlyList<FosterSuggestionDto>>("Found pet report not found.");

        var candidates = await fosterVolunteerRepository.GetNearbyAvailableAsync(
            foundReport.FoundLat,
            foundReport.FoundLng,
            foundReport.FoundSpecies,
            RadiusMetres,
            cancellationToken);

        var ordered = candidates
            .OrderByDescending(c => c.SpeciesMatch)
            .ThenBy(c => c.DistanceMetres)
            .ThenByDescending(c => c.MaxDays)
            .Take(Math.Clamp(request.MaxResults, 1, 10))
            .Select(MapToDto)
            .ToList();

        return Result.Success<IReadOnlyList<FosterSuggestionDto>>(ordered);
    }

    private static FosterSuggestionDto MapToDto(FosterVolunteerSuggestion suggestion)
    {
        // Round to the nearest 100 m to prevent GPS triangulation of volunteer home addresses.
        // The public GET /api/found-pets/active endpoint exposes GPS coordinates and report IDs;
        // returning precise distances would allow any authenticated user to use 3+ reference
        // points to triangulate a foster volunteer's home address to within metres.
        var roundedMetres = Math.Round(suggestion.DistanceMetres / 100.0) * 100.0;

        var distanceLabel = roundedMetres < 1000
            ? $"{roundedMetres:0} m"
            : $"{(roundedMetres / 1000.0).ToString("0.0", CultureInfo.InvariantCulture)} km";

        return new FosterSuggestionDto(
            suggestion.VolunteerName,
            roundedMetres,
            distanceLabel,
            suggestion.SizePreference,
            suggestion.MaxDays,
            suggestion.SpeciesMatch);
    }
}
