using MediatR;
using PawTrack.Domain.Common;
using PawTrack.Domain.Pets;

namespace PawTrack.Application.Pets.Commands.UpdatePet;

public sealed record UpdatePetCommand(
    Guid PetId,
    Guid RequestingUserId,
    string Name,
    PetSpecies Species,
    string? Breed,
    DateOnly? BirthDate,
    byte[]? PhotoBytes,
    string? PhotoContentType,
    string? PhotoFileName) : IRequest<Result<PetId>>;

// Discriminated ID wrapper to clearly distinguish "updated pet ID" from raw Guid
public readonly record struct PetId(Guid Value);
