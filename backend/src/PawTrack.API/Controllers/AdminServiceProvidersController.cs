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

    [HttpPut("payments/{paymentId:guid}/confirm")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> ConfirmPayment(
        Guid paymentId,
        [FromBody] ConfirmProviderPaymentRequest request,
        CancellationToken ct)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var adminUserId)) return Unauthorized();
        var result = await sender.Send(new ConfirmProviderBookingPaymentCommand(
            adminUserId, paymentId, request.ExternalReference), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpGet("incidents")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetIncidents([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetProviderIncidentsForAdminQuery(page, pageSize), ct);
        return result.IsSuccess ? Ok(result.Value) : Ok(Array.Empty<object>());
    }

    [HttpPut("incidents/{incidentId:guid}/investigation")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> StartIncident(Guid incidentId, [FromBody] AssignProviderIncidentRequest request, CancellationToken ct)
    {
        if (!TryGetAdminId(out var adminUserId)) return Unauthorized();
        var result = await sender.Send(new StartProviderIncidentInvestigationCommand(adminUserId, incidentId, adminUserId), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPut("incidents/{incidentId:guid}/resolve")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> ResolveIncident(Guid incidentId, [FromBody] ResolveProviderIncidentRequest request, CancellationToken ct)
    {
        if (!TryGetAdminId(out var adminUserId)) return Unauthorized();
        var result = await sender.Send(new ResolveProviderIncidentCommand(adminUserId, incidentId, request.Resolution), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }

    [HttpPut("incidents/{incidentId:guid}/close")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> CloseIncident(Guid incidentId, CancellationToken ct)
    {
        if (!TryGetAdminId(out var adminUserId)) return Unauthorized();
        var result = await sender.Send(new CloseProviderIncidentCommand(adminUserId, incidentId), ct);
        return result.IsSuccess ? Ok(result.Value) : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
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

    private bool TryGetAdminId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out userId);
    }
}

public sealed record ReviewServiceProviderRequest(bool Approve);
public sealed record SetServiceProviderOperationalStatusRequest(bool Suspend, string? Reason);
public sealed record ReviewProviderVerificationRequest(bool Approve, DateOnly? ExpiresAt, string? Reason);
public sealed record ConfirmProviderPaymentRequest(string ExternalReference);
public sealed record AssignProviderIncidentRequest(Guid AssignedToUserId);
public sealed record ResolveProviderIncidentRequest(string Resolution);