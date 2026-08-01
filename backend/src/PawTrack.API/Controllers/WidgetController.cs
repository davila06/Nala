using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Clinics.Queries.GetMyClinic;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.API.Controllers;

/// <summary>
/// Public widget config endpoint for Clínica Partner embeddable widget.
/// Open CORS — called by external clinic websites.
/// </summary>
[ApiController]
[Route("api/widget")]
[AllowAnonymous]
public sealed class WidgetController(ISender sender, ISubscriptionRepository subscriptionRepository) : ControllerBase
{
    [HttpGet("clinic/{clinicId:guid}/config")]
    [EnableRateLimiting("public-api")]
    [ResponseCache(Duration = 300)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfig(Guid clinicId, CancellationToken cancellationToken)
    {
        // Only Partner clinics may embed the widget
        var sub = await subscriptionRepository.GetActiveForClinicAsync(clinicId, cancellationToken);
        if (sub is null || sub.Tier < SubscriptionTier.ClinicPartner)
            return NotFound(new ProblemDetails { Detail = "Widget not available for this clinic.", Status = 404 });

        // Reuse GetMyClinic query via UserId lookup — not ideal but avoids a new query
        // Just return the tier confirmation; real config is minimal for now
        return Ok(new
        {
            clinicId,
            isVerified = true,
            tier = sub.Tier.ToString(),
            widgetApiUrl = $"{Request.Scheme}://{Request.Host}/api/v1/pets/lookup",
        });
    }
}
