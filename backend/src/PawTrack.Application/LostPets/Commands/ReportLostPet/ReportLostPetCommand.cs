using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.LostPets.Commands.ReportLostPet;

/// <summary>
/// Photo is passed as raw bytes to keep Application layer independent of ASP.NET Core (IFormFile).
/// The API controller reads IFormFile → byte[] before dispatching the command.
/// </summary>
public sealed record ReportLostPetCommand(
    Guid PetId,
    Guid RequestingUserId,
    string? Description,
    string? PublicMessage,
    double? LastSeenLat,
    double? LastSeenLng,
    DateTimeOffset LastSeenAt,
    byte[]? PhotoBytes,
    string? PhotoContentType,
    string? PhotoFileName,
    string? ContactName,
    string? ContactPhone,
    decimal? RewardAmount,
    string? RewardNote) : IRequest<Result<string>>;
