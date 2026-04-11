using MediatR;
using PawTrack.Application.Pets.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Pets.Queries.GetPublicPetProfile;

public sealed record GetPublicPetProfileQuery(Guid PetId) : IRequest<Result<PublicPetProfileDto>>;
