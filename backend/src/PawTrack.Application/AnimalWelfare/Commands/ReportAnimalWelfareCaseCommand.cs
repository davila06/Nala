using MediatR;
using PawTrack.Application.AnimalWelfare.Dtos;
using PawTrack.Application.AnimalWelfare.Interfaces;
using PawTrack.Application.AnimalWelfare.Routing;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.AnimalWelfare;
using PawTrack.Domain.Common;

namespace PawTrack.Application.AnimalWelfare.Commands;

public sealed record ReportAnimalWelfareCaseCommand(
    WelfareCaseType Type,
    WelfareSeverity Severity,
    string Canton,
    string Description,
    Guid? ReporterUserId,
    bool ReporterIsAnonymous,
    double? ApproxLat,
    double? ApproxLng,
    Guid? PetId = null,
    Guid? LostPetEventId = null,
    Guid? SightingId = null,
    Guid? CapturedAnimalId = null,
    Guid? AdoptablePetId = null,
    bool AutoRoutingRequested = false) : IRequest<Result<PublicAnimalWelfareCaseStatusDto>>;

public sealed class ReportAnimalWelfareCaseCommandHandler(
    IAnimalWelfareCaseRepository caseRepository,
    IAnimalWelfareAuditRepository auditRepository,
    WelfareRoutingService routingService,
    IPiiScrubber piiScrubber,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReportAnimalWelfareCaseCommand, Result<PublicAnimalWelfareCaseStatusDto>>
{
    public async Task<Result<PublicAnimalWelfareCaseStatusDto>> Handle(ReportAnimalWelfareCaseCommand request, CancellationToken ct)
    {
        var description = piiScrubber.Scrub(request.Description);
        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure<PublicAnimalWelfareCaseStatusDto>("La descripción del caso es requerida.");

        if (string.IsNullOrWhiteSpace(request.Canton))
            return Result.Failure<PublicAnimalWelfareCaseStatusDto>("El cantón es requerido.");

        var welfareCase = AnimalWelfareCase.Create(
            request.Type,
            request.Severity,
            request.Canton,
            description,
            request.ReporterUserId,
            request.ReporterIsAnonymous,
            request.ApproxLat,
            request.ApproxLng,
            request.PetId,
            request.LostPetEventId,
            request.SightingId,
            request.CapturedAnimalId,
            request.AdoptablePetId,
            request.AutoRoutingRequested);

        if (request.AutoRoutingRequested)
        {
            var suggestion = await routingService.GetSuggestedCandidateAsync(
                request.Canton,
                request.ApproxLat,
                request.ApproxLng,
                ct);
            if (suggestion is not null)
                welfareCase.SetRoutingSuggestion(suggestion.UserId, suggestion.RecipientType.ToString(), suggestion.DistanceMetres);
        }

        await caseRepository.AddAsync(welfareCase, ct);
        await auditRepository.AddAsync(AnimalWelfareCaseAuditLog.Create(
            welfareCase.Id,
            WelfareAuditAction.CaseReported,
            request.ReporterIsAnonymous ? null : request.ReporterUserId,
            "SENASA-ready welfare case reported."), ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(welfareCase.ToPublicStatus());
    }
}
