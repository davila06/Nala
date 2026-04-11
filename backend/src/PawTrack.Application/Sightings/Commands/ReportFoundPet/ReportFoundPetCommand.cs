using MediatR;
using PawTrack.Application.Sightings.DTOs;
using PawTrack.Domain.Common;
using PawTrack.Domain.Pets;

namespace PawTrack.Application.Sightings.Commands.ReportFoundPet;

public sealed record ReportFoundPetCommand(
    PetSpecies FoundSpecies,
    string? BreedEstimate,
    string? ColorDescription,
    string? SizeEstimate,
    double FoundLat,
    double FoundLng,
    string ContactName,
    string ContactPhone,
    string? Note,
    Stream? PhotoStream,
    string? PhotoContentType) : IRequest<Result<ReportFoundPetResult>>;

public sealed record ReportFoundPetResult(
    Guid ReportId,
    IReadOnlyList<MatchCandidateDto> Candidates);
