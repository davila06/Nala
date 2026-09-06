using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PawTrack.Application.Subscriptions.Commands.CreateSubscriptionPlan;
using PawTrack.Application.Subscriptions.Commands.DeleteSubscriptionPlan;
using PawTrack.Application.Subscriptions.Commands.UpdateSubscriptionPlan;
using PawTrack.Application.Subscriptions.Queries.GetAdminSubscriptionPlans;
using PawTrack.Application.Subscriptions.Queries.GetAdminSubscriptionPlan;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.API.Controllers;

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