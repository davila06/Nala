using MediatR;
using PawTrack.Application.AnimalWelfare.Dtos;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.AnimalWelfare;
using PawTrack.Domain.Common;

namespace PawTrack.Application.AnimalWelfare.Commands;

public sealed record ConvertCapturedAnimalToWelfareCaseCommand(
    Guid CapturedAnimalId,
    Guid RequestingUserId,
    WelfareSeverity Severity,
    string? Description) : IRequest<Result<PublicAnimalWelfareCaseStatusDto>>;

public sealed class ConvertCapturedAnimalToWelfareCaseCommandHandler(
    ICapturedAnimalRepository capturedAnimalRepository,
    ISender sender)
    : IRequestHandler<ConvertCapturedAnimalToWelfareCaseCommand, Result<PublicAnimalWelfareCaseStatusDto>>
{
    public async Task<Result<PublicAnimalWelfareCaseStatusDto>> Handle(ConvertCapturedAnimalToWelfareCaseCommand request, CancellationToken ct)
    {
        var capture = await capturedAnimalRepository.GetByIdAsync(request.CapturedAnimalId, ct);
        if (capture is null) return Result.Failure<PublicAnimalWelfareCaseStatusDto>("Registro municipal no encontrado.");

        var description = string.IsNullOrWhiteSpace(request.Description)
            ? $"Caso originado desde captura municipal. Especie: {capture.Species}; color: {capture.Color}; notas: {capture.Notes}"
            : request.Description;

        return await sender.Send(new ReportAnimalWelfareCaseCommand(
            WelfareCaseType.MunicipalCapture,
            request.Severity,
            capture.Canton,
            description,
            request.RequestingUserId,
            ReporterIsAnonymous: false,
            ApproxLat: null,
            ApproxLng: null,
            PetId: capture.MatchedPetId,
            CapturedAnimalId: capture.Id), ct);
    }
}
