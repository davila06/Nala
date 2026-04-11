using MediatR;
using PawTrack.Application.Pets.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Pets.Queries.GetMyPets;

public sealed record GetMyPetsQuery(Guid OwnerId) : IRequest<Result<IReadOnlyList<PetSummaryDto>>>;
