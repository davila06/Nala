using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.AnimalWelfare.Queries;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/welfare-cases")]
[Authorize(Roles = "Admin,Municipality,Clinic,Ally,Shelter")]
public sealed class WelfareCasesController(ISender sender) : ControllerBase
{
    [HttpGet("assigned")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetAssigned(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetAssignedWelfareCasesQuery(userId, page, pageSize), cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("assigned/{caseId:guid}")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetAssignedDetail(Guid caseId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetAssignedWelfareCaseDetailQuery(caseId, userId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpGet("assigned/{caseId:guid}/evidence/{evidenceId:guid}/download")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> DownloadAssignedEvidence(
        Guid caseId,
        Guid evidenceId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(
            new DownloadWelfareEvidenceQuery(evidenceId, userId, CanAccessAll: false, ExpectedCaseId: caseId),
            cancellationToken);
        return result.IsSuccess
            ? File(result.Value!.Bytes, result.Value.ContentType, result.Value.FileName)
            : NotFound();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}
