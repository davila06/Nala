using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using PawTrack.Application.AnimalWelfare.Commands;
using PawTrack.Application.AnimalWelfare.Queries;
using PawTrack.Domain.AnimalWelfare;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/admin/welfare-cases")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class AdminWelfareRoutingController(ISender sender) : ControllerBase
{
    [HttpGet("{caseId:guid}/routing-candidates")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetCandidates(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetWelfareRoutingCandidatesQuery(caseId), cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 404 });
    }

    [HttpPost("{caseId:guid}/routing/confirm")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(2048)]
    public async Task<IActionResult> ConfirmRouting(
        Guid caseId,
        [FromBody] ConfirmWelfareRoutingRequest request,
        CancellationToken cancellationToken)
    {
        var rawActorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawActorUserId, out var actorUserId)) return Unauthorized();

        var result = await sender.Send(new ConfirmWelfareCaseRoutingCommand(
            caseId,
            actorUserId,
            request.RecipientUserId,
            request.RecipientType,
            request.Reason), cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }
}

public sealed record ConfirmWelfareRoutingRequest(
    Guid RecipientUserId,
    WelfareReferralRecipientType RecipientType,
    string Reason);
