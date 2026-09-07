using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.AnimalWelfare.Commands;
using PawTrack.Application.AnimalWelfare.Queries;
using PawTrack.Domain.AnimalWelfare;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/public/welfare-cases")]
public sealed class PublicWelfareCasesController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("welfare-report")]
    [RequestSizeLimit(16_384)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Report([FromBody] ReportWelfareCaseRequest request, CancellationToken cancellationToken)
    {
        var userId = TryGetUserId(out var parsedUserId) ? parsedUserId : (Guid?)null;
        var result = await sender.Send(new ReportAnimalWelfareCaseCommand(
            request.Type,
            request.Severity,
            request.Canton,
            request.Description,
            userId,
            request.ReporterIsAnonymous,
            request.ApproxLat,
            request.ApproxLng,
            request.PetId,
            request.LostPetEventId,
            request.SightingId,
            request.CapturedAnimalId,
            request.AdoptablePetId), cancellationToken);

        return result.IsSuccess ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpGet("{publicCode}")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicStatus(string publicCode, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPublicWelfareCaseStatusQuery(publicCode), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}

public sealed record ReportWelfareCaseRequest(
    WelfareCaseType Type,
    WelfareSeverity Severity,
    string Canton,
    string Description,
    bool ReporterIsAnonymous,
    double? ApproxLat,
    double? ApproxLng,
    Guid? PetId,
    Guid? LostPetEventId,
    Guid? SightingId,
    Guid? CapturedAnimalId,
    Guid? AdoptablePetId);
