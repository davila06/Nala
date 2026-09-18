using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.CastrationCampaigns.Commands.CreateCastrationCampaign;
using PawTrack.Application.CastrationCampaigns.Commands.ManageCastrationCampaign;
using PawTrack.Application.CastrationCampaigns.Commands.OperateCastrationAppointment;
using PawTrack.Application.CastrationCampaigns.Commands.ReserveCastrationAppointment;
using PawTrack.Application.CastrationCampaigns.DTOs;
using PawTrack.Application.CastrationCampaigns.Queries.GetCastrationAppointments;
using PawTrack.Application.CastrationCampaigns.Queries.GetPublishedCastrationCampaigns;

namespace PawTrack.API.Controllers;

/// <summary>Manages public discovery and authorized creation of castration campaigns.</summary>
[ApiController]
[Route("api/castration-campaigns")]
public sealed class CastrationCampaignsController(ISender sender) : ControllerBase
{
    /// <summary>Returns upcoming published campaigns with available capacity.</summary>
    [HttpGet]
    [AllowAnonymous]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(typeof(CastrationCampaignPageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublished(
        [FromQuery] string? canton,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var result = await sender.Send(
            new GetPublishedCastrationCampaignsQuery(canton, page, pageSize),
            cancellationToken);

        return Ok(result.Value);
    }

    /// <summary>Creates a draft campaign for later administrative approval.</summary>
    [HttpPost]
    [Authorize(Roles = "Clinic,Municipality,Ally,Admin")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(typeof(CastrationCampaignDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCastrationCampaignRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new CreateCastrationCampaignCommand(
            userId,
            request.ExecutingClinicId,
            request.Title,
            request.VenueLabel,
            request.Canton,
            request.Latitude,
            request.Longitude,
            request.StartsAt,
            request.EndsAt,
            request.ReservationsOpenAt,
            request.ReservationsCloseAt,
            request.Capacity,
            request.BasePriceCrc,
            request.ConsentVersion), cancellationToken);

        if (result.IsFailure)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Detail = string.Join("; ", result.Errors),
                Status = StatusCodes.Status422UnprocessableEntity,
            });
        }

        return CreatedAtAction(nameof(GetPublished), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>Submits an organizer-owned draft for administrative approval.</summary>
    [HttpPost("{campaignId:guid}/submit")]
    [Authorize(Roles = "Clinic,Municipality,Ally,Admin")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(typeof(CastrationCampaignDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Submit(Guid campaignId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(
            new SubmitCastrationCampaignCommand(campaignId, userId), cancellationToken);
        return ToLifecycleResult(result);
    }

    /// <summary>Approves a pending campaign. Restricted to administrators.</summary>
    [HttpPost("{campaignId:guid}/approve")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(typeof(CastrationCampaignDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Approve(Guid campaignId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(
            new ApproveCastrationCampaignCommand(campaignId, userId), cancellationToken);
        return ToLifecycleResult(result);
    }

    /// <summary>Publishes an approved campaign for public discovery and reservations.</summary>
    [HttpPost("{campaignId:guid}/publish")]
    [Authorize(Roles = "Clinic,Municipality,Ally,Admin")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(typeof(CastrationCampaignDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Publish(Guid campaignId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(
            new PublishCastrationCampaignCommand(campaignId, userId), cancellationToken);
        return ToLifecycleResult(result);
    }

    /// <summary>Reserves one campaign appointment for a pet owned by the authenticated user.</summary>
    [HttpPost("{campaignId:guid}/appointments")]
    [Authorize(Roles = "Owner")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(typeof(CastrationAppointmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Reserve(
        Guid campaignId,
        [FromBody] ReserveCastrationAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new ReserveCastrationAppointmentCommand(
            userId,
            campaignId,
            request.PetId,
            request.ScheduledAt,
            request.WeightKg,
            request.ConfirmsFastingInstructions,
            request.IsPregnant,
            request.IsInHeat,
            request.ConsentAccepted,
            request.ConsentVersion,
            request.RequiresInvoice), cancellationToken);

        if (result.IsFailure)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Detail = string.Join("; ", result.Errors),
                Status = StatusCodes.Status422UnprocessableEntity,
            });
        }

        return Created($"/api/castration-campaigns/{campaignId}/appointments/{result.Value!.Id}", result.Value);
    }

    /// <summary>Returns appointments owned by the authenticated pet owner.</summary>
    [HttpGet("appointments/mine")]
    [Authorize(Roles = "Owner")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 50);
        var result = await sender.Send(new GetMyCastrationAppointmentsQuery(userId, page, pageSize), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Returns the operational agenda for the executing clinic or an administrator.</summary>
    [HttpGet("{campaignId:guid}/appointments")]
    [Authorize(Roles = "Clinic,Admin")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> GetAgenda(Guid campaignId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await sender.Send(new GetCampaignAppointmentsQuery(campaignId, userId, page, pageSize), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Forbid();
    }

    /// <summary>Applies an authorized clinical workflow transition to an appointment.</summary>
    [HttpPost("appointments/{appointmentId:guid}/operate")]
    [Authorize(Roles = "Clinic,Admin")]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> Operate(Guid appointmentId, [FromBody] OperateCastrationAppointmentRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new OperateCastrationAppointmentCommand(
            appointmentId, userId, request.Operation, request.VeterinarianId,
            request.ClinicalOutcome, request.PostOperativeInstructions), cancellationToken);
        if (result.IsFailure) return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
        return Ok(result.Value);
    }

    private IActionResult ToLifecycleResult(PawTrack.Domain.Common.Result<CastrationCampaignDto> result)
    {
        if (result.IsSuccess) return Ok(result.Value);
        return UnprocessableEntity(new ProblemDetails
        {
            Detail = string.Join("; ", result.Errors),
            Status = StatusCodes.Status422UnprocessableEntity,
        });
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}

/// <summary>Payload used by an authorized organization to create a campaign draft.</summary>
public sealed record CreateCastrationCampaignRequest(
    Guid ExecutingClinicId,
    string Title,
    string VenueLabel,
    string Canton,
    double Latitude,
    double Longitude,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    DateTimeOffset ReservationsOpenAt,
    DateTimeOffset ReservationsCloseAt,
    int Capacity,
    decimal BasePriceCrc,
    string ConsentVersion);

/// <summary>Payload used by a pet owner to reserve a campaign appointment.</summary>
public sealed record ReserveCastrationAppointmentRequest(
    Guid PetId,
    DateTimeOffset ScheduledAt,
    decimal WeightKg,
    bool ConfirmsFastingInstructions,
    bool IsPregnant,
    bool IsInHeat,
    bool ConsentAccepted,
    string ConsentVersion,
    bool RequiresInvoice = false);

/// <summary>Payload for an authorized clinical appointment transition.</summary>
public sealed record OperateCastrationAppointmentRequest(
    CastrationAppointmentOperation Operation,
    Guid? VeterinarianId = null,
    string? ClinicalOutcome = null,
    string? PostOperativeInstructions = null);
