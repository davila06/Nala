using MediatR;
using PawTrack.Application.Sightings.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Sightings.Queries.GetActiveFoundPets;

/// <summary>
/// Returns the most-recent open found-pet reports for the public map.
/// </summary>
public sealed record GetActiveFoundPetsQuery(int MaxResults = 50)
    : IRequest<Result<IReadOnlyList<FoundPetReportDto>>>;
