using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Pets.Queries.DownloadPetIdCard;

public sealed record DownloadPetIdCardQuery(Guid PetId, Guid RequestingUserId) : IRequest<Result<byte[]>>;

public sealed class DownloadPetIdCardQueryHandler(
    IPetRepository petRepository,
    IUserRepository userRepository,
    IPetIdCardService idCardService,
    IPublicAppUrlProvider urlProvider)
    : IRequestHandler<DownloadPetIdCardQuery, Result<byte[]>>
{
    public async Task<Result<byte[]>> Handle(DownloadPetIdCardQuery request, CancellationToken ct)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null || pet.OwnerId != request.RequestingUserId)
            return Result.Failure<byte[]>("Mascota no encontrada o acceso denegado.");

        var owner = await userRepository.GetByIdAsync(request.RequestingUserId, ct);
        if (owner is null)
            return Result.Failure<byte[]>("Usuario no encontrado.");

        var publicUrl = $"{urlProvider.GetBaseUrl()}/p/{pet.Id}";

        var data = new PetIdCardData(
            pet.Name,
            pet.Species.ToString(),
            pet.Breed,
            pet.PhotoUrl,
            owner.Name,
            publicUrl);

        var pdfBytes = idCardService.Generate(data);
        return Result.Success(pdfBytes);
    }
}
