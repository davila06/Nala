using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Payments.Commands.UpsertBillingProfile;
using PawTrack.Application.Payments.DTOs;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Application.Payments.Queries.GetBillingProfile;
using PawTrack.Application.Payments.Queries.GetInvoices;
using PawTrack.Domain.Payments;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/billing")]
[Authorize]
public sealed class BillingController(
    ISender sender,
    IElectronicBillingService billingService) : ControllerBase
{
    // ── GET /api/billing/profile — Get authenticated user's billing profile ────
    [HttpGet("profile")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new GetBillingProfileQuery(userId), cancellationToken);
        return Ok(result.Value);
    }

    // ── PUT /api/billing/profile — Upsert tax identification & billing data ───
    [HttpPut("profile")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpsertProfile(
        [FromBody] UpsertBillingProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new UpsertBillingProfileCommand(
                UserId: userId,
                IdentificationType: request.IdentificationType,
                IdentificationNumber: request.IdentificationNumber,
                LegalName: request.LegalName,
                BillingEmail: request.BillingEmail,
                RequiresInvoice: request.RequiresInvoice,
                Province: request.Province,
                Canton: request.Canton,
                District: request.District,
                AddressDetails: request.AddressDetails,
                PhoneNumber: request.PhoneNumber),
            cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Ok(result.Value);
    }

    // ── GET /api/billing/invoices — User's electronic invoices & tickets ──────
    [HttpGet("invoices")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoices(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new GetUserInvoicesQuery(userId), cancellationToken);
        return Ok(result.Value);
    }

    // ── GET /api/billing/invoices/{id}/pdf — Download invoice PDF ─────────────
    [HttpGet("invoices/{id:guid}/pdf")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadInvoicePdf(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        try
        {
            var pdfBytes = await billingService.GenerateInvoicePdfAsync(id, cancellationToken);
            return File(pdfBytes, "application/pdf", $"Factura-{id}.pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ProblemDetails { Detail = "Comprobante electrónico no encontrado." });
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}

public sealed record UpsertBillingProfileRequest(
    TaxIdentificationType IdentificationType,
    string IdentificationNumber,
    string LegalName,
    string BillingEmail,
    bool RequiresInvoice = true,
    string? Province = null,
    string? Canton = null,
    string? District = null,
    string? AddressDetails = null,
    string? PhoneNumber = null);
