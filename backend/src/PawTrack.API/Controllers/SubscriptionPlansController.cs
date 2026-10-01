using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PawTrack.Application.Subscriptions.Commands.CreateSubscriptionPlan;
using PawTrack.Application.Subscriptions.Commands.DeleteSubscriptionPlan;
using PawTrack.Application.Subscriptions.Commands.UpdateSubscriptionPlan;
using PawTrack.Application.Subscriptions.Queries.GetAdminSubscriptionPlans;
using PawTrack.Application.Subscriptions.Queries.GetAdminSubscriptionPlan;
using PawTrack.Application.Subscriptions.Commands.SetSubscriptionPlanCommercialApproval;
using PawTrack.Domain.Subscriptions;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

/// <summary>Admin-only subscription plan catalog and commercial publication approvals.</summary>
[ApiController]
[Route("api/admin/subscription-plans")]
[Authorize(Roles = "Admin")]
public sealed class SubscriptionPlansController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeInactive = false,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetAdminSubscriptionPlansQuery(includeInactive, skip, take), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateSubscriptionPlanCommand(
                request.Tier,
                request.DisplayName,
                request.Description,
                request.MonthlyPriceCrc,
                request.AnnualPriceCrc),
            cancellationToken);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetAdminSubscriptionPlanQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateSubscriptionPlanCommand(
                id,
                request.Version,
                request.DisplayName,
                request.Description,
                request.MonthlyPriceCrc,
                request.AnnualPriceCrc),
            cancellationToken);
        if (result.IsFailure)
            return result.Errors.Contains("Subscription plan not found.")
                ? NotFound()
                : Conflict(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromBody] DeleteSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new DeleteSubscriptionPlanCommand(id, request.Version), cancellationToken);
        if (result.IsFailure)
            return result.Errors.Contains("Subscription plan not found.")
                ? NotFound()
                : Conflict(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/commercial-approval")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ApproveForCommercialPublication(
        Guid id,
        [FromBody] CommercialApprovalRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized();

        var result = await sender.Send(new SetSubscriptionPlanCommercialApprovalCommand(
            id, request.Version, adminId, true, request.ApprovalReference), cancellationToken);
        if (result.IsFailure)
        {
            if (result.Errors.Contains("Subscription plan not found.")) return NotFound();
            if (result.Errors.Contains("The subscription plan was modified by another administrator."))
                return Conflict(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}/commercial-approval")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RevokeCommercialApproval(
        Guid id,
        [FromBody] RevokeCommercialApprovalRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAdminId(out var adminId)) return Unauthorized();

        var result = await sender.Send(new SetSubscriptionPlanCommercialApprovalCommand(
            id, request.Version, adminId, false, null), cancellationToken);
        if (result.IsFailure)
        {
            if (result.Errors.Contains("Subscription plan not found.")) return NotFound();
            if (result.Errors.Contains("The subscription plan was modified by another administrator."))
                return Conflict(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
        }

        return Ok(result.Value);
    }

    private bool TryGetAdminId(out Guid adminId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out adminId);
    }
}

public sealed record SubscriptionPlanRequest(
    SubscriptionTier Tier,
    string DisplayName,
    string Description,
    decimal? MonthlyPriceCrc,
    decimal? AnnualPriceCrc);

public sealed record UpdateSubscriptionPlanRequest(
    Guid Version,
    string DisplayName,
    string Description,
    decimal? MonthlyPriceCrc,
    decimal? AnnualPriceCrc);

public sealed record DeleteSubscriptionPlanRequest(Guid Version);
/// <summary>Request to publish an active plan using an auditable approval reference.</summary>
public sealed record CommercialApprovalRequest(Guid Version, string? ApprovalReference);

/// <summary>Request to revoke commercial publication using the current plan version.</summary>
public sealed record RevokeCommercialApprovalRequest(Guid Version);
