using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Sightings.Commands.ReportSighting;

/// <summary>
/// Creates an anonymous sighting record for a pet.
/// The command is intentionally free of reporter PII — the API layer never
/// accepts contact details; the note is sanitised before reaching this command.
/// </summary>
public sealed record ReportSightingCommand(
    Guid PetId,
    double Lat,
    double Lng,

    /// <summary>Raw user note; will be run through <c>IPiiScrubber</c> in the handler.</summary>
    string? RawNote,

    /// <summary>Optional photo stream. Null when no photo was submitted.</summary>
    Stream? PhotoStream,
    string? PhotoContentType,

    DateTimeOffset SightedAt) : IRequest<Result<string>>;
