using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.AnimalWelfare.Commands;
using PawTrack.Application.AnimalWelfare.Queries;
using PawTrack.Domain.AnimalWelfare;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/admin/welfare-cases")]
[Authorize(Roles = "Admin,Support")]
public sealed class AdminWelfareCasesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetQueue(
        [FromQuery] WelfareCaseStatus? status,
        [FromQuery] WelfareSeverity? severity,
        [FromQuery] string? canton,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetWelfareCaseQueueQuery(status, severity, canton, page, pageSize), cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{caseId:guid}")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetDetail(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetWelfareCaseDetailQuery(caseId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpPost("{caseId:guid}/triage")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(512)]
    public async Task<IActionResult> StartTriage(Guid caseId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new StartWelfareCaseTriageCommand(caseId, userId), cancellationToken);
        return ToMutationResult(result);
    }

    [HttpPost("{caseId:guid}/severity")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(512)]
    public async Task<IActionResult> SetSeverity(Guid caseId, [FromBody] SetSeverityRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new SetWelfareCaseSeverityCommand(caseId, userId, request.Severity), cancellationToken);
        return ToMutationResult(result);
    }

    [HttpPost("{caseId:guid}/assign")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(1024)]
    public async Task<IActionResult> Assign(Guid caseId, [FromBody] AssignWelfareCaseRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new AssignWelfareCaseCommand(caseId, userId, request.OrganizationUserId, request.Role), cancellationToken);
        return ToMutationResult(result);
    }

    [HttpPost("{caseId:guid}/refer")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(2048)]
    public async Task<IActionResult> Refer(Guid caseId, [FromBody] ReferWelfareCaseRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new ReferWelfareCaseCommand(caseId, userId, request.Destination, request.Reason), cancellationToken);
        return ToMutationResult(result);
    }

    [HttpPost("{caseId:guid}/resolve")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(2048)]
    public async Task<IActionResult> Resolve(Guid caseId, [FromBody] WelfareReasonRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new ResolveWelfareCaseCommand(caseId, userId, request.Reason), cancellationToken);
        return ToMutationResult(result);
    }

    [HttpPost("{caseId:guid}/dismiss")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(2048)]
    public async Task<IActionResult> Dismiss(Guid caseId, [FromBody] WelfareReasonRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new DismissWelfareCaseCommand(caseId, userId, request.Reason), cancellationToken);
        return ToMutationResult(result);
    }

    [HttpPost("{caseId:guid}/close-no-action")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(2048)]
    public async Task<IActionResult> CloseNoAction(Guid caseId, [FromBody] WelfareReasonRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new CloseWelfareCaseNoActionCommand(caseId, userId, request.Reason), cancellationToken);
        return ToMutationResult(result);
    }

    [HttpPost("{caseId:guid}/notes")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(4096)]
    public async Task<IActionResult> AddNote(Guid caseId, [FromBody] NoteRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new AddWelfareCaseNoteCommand(caseId, userId, request.Body), cancellationToken);
        return ToMutationResult(result);
    }

    [HttpPost("{caseId:guid}/evidence")]
    [EnableRateLimiting("public-api")]
    [RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> UploadEvidence(
        Guid caseId,
        IFormFile file,
        [FromForm] string evidenceKind = "Photo",
        [FromForm] bool isSensitive = true,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (!Enum.TryParse<WelfareEvidenceKind>(evidenceKind, true, out var parsedEvidenceKind))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid evidence kind",
                Detail = $"Unsupported evidence kind '{evidenceKind}'. Valid values: {string.Join(", ", Enum.GetNames<WelfareEvidenceKind>())}",
                Status = 400,
            });
        }

        await using var stream = file.OpenReadStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        var result = await sender.Send(new UploadAnimalWelfareEvidenceCommand(
            caseId,
            userId,
            ms.ToArray(),
            file.FileName,
            file.ContentType,
            parsedEvidenceKind,
            isSensitive), cancellationToken);

        return result.IsSuccess ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpGet("evidence/{evidenceId:guid}/download")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> DownloadEvidence(Guid evidenceId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new DownloadWelfareEvidenceQuery(evidenceId, userId), cancellationToken);
        return result.IsSuccess
            ? File(result.Value!.Bytes, result.Value.ContentType, result.Value.FileName)
            : NotFound();
    }

    private IActionResult ToMutationResult(PawTrack.Domain.Common.Result<bool> result) =>
        result.IsSuccess ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}

public sealed record SetSeverityRequest(WelfareSeverity Severity);
public sealed record AssignWelfareCaseRequest(Guid OrganizationUserId, string Role);
public sealed record ReferWelfareCaseRequest(string Destination, string Reason);
public sealed record WelfareReasonRequest(string Reason);
public sealed record NoteRequest(string Body);

