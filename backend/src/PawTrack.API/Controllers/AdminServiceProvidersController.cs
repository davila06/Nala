using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.ServiceProviders;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/admin/service-providers")]
[Authorize(Roles = "Admin")]
public sealed class AdminServiceProvidersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetProviders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetServiceProvidersForAdminQuery(page, pageSize), ct);
        return result.IsSuccess ? Ok(result.Value) : Ok(Array.Empty<object>());
    }

    [HttpGet("verifications/pending")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetPendingVerifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetPendingProviderVerificationsQuery(page, pageSize), ct);
        return result.IsSuccess ? Ok(result.Value) : Ok(Array.Empty<object>());
    }

    [HttpPut("{serviceProviderId:guid}/review")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> Review(
        Guid serviceProviderId,
        [FromBody] ReviewServiceProviderRequest request,
        CancellationToken ct)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var adminUserId)) return Unauthorized();

        var result = await sender.Send(new ReviewServiceProviderCommand(adminUserId, serviceProviderId, request.Approve), ct);
        return result.IsSuccess
            ? NoContent()
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPut("{serviceProviderId:guid}/operational-status")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> SetOperationalStatus(
        Guid serviceProviderId,
        [FromBody] SetServiceProviderOperationalStatusRequest request,
        CancellationToken ct)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var adminUserId)) return Unauthorized();
        var result = await sender.Send(new SetServiceProviderOperationalStatusCommand(
            adminUserId, serviceProviderId, request.Suspend, request.Reason), ct);
        return result.IsSuccess
            ? NoContent()
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPut("verifications/{verificationId:guid}/review")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> ReviewVerification(
        Guid verificationId,
        [FromBody] ReviewProviderVerificationRequest request,
        CancellationToken ct)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var adminUserId)) return Unauthorized();
        var result = await sender.Send(new ReviewProviderVerificationCommand(
            verificationId, adminUserId, request.Approve, request.ExpiresAt, request.Reason), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpGet("verifications/{verificationId:guid}/document")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> DownloadVerificationDocument(Guid verificationId, CancellationToken ct)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var adminUserId)) return Unauthorized();
        var result = await sender.Send(new DownloadProviderVerificationDocumentQuery(verificationId, adminUserId, true), ct);
        return result.IsSuccess
            ? File(result.Value!.Bytes, result.Value.ContentType, result.Value.FileName)
            : NotFound(new ProblemDetails { Detail = "Documento no disponible.", Status = StatusCodes.Status404NotFound });
    }
}

public sealed record ReviewServiceProviderRequest(bool Approve);
public sealed record SetServiceProviderOperationalStatusRequest(bool Suspend, string? Reason);
public sealed record ReviewProviderVerificationRequest(bool Approve, DateOnly? ExpiresAt, string? Reason);