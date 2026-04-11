using MediatR;
using PawTrack.Domain.Common;
using PawTrack.Domain.Pets;

namespace PawTrack.Application.Pets.Commands.CreatePet;

/// <summary>
/// Photo is passed as raw bytes to keep Application layer independent of ASP.NET Core (IFormFile).
/// The API controller reads IFormFile → byte[] before dispatching the command.
/// </summary>
public sealed record CreatePetCommand(
    Guid OwnerId,
    string Name,
    PetSpecies Species,
    string? Breed,
    DateOnly? BirthDate,
    byte[]? PhotoBytes,
    string? PhotoContentType,
    string? PhotoFileName) : IRequest<Result<string>>;
