using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PawTrack.Application.Subscriptions.Commands.ManageSubscriptionAddon;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/admin/subscription-addons")]
[Authorize(Roles = "Admin")]
public sealed class SubscriptionAddonsController(
    ISender sender,
    ISubscriptionAddonRepository addonRepository) : ControllerBase
{
    [HttpGet("subscription/{subscriptionId:guid}")]
    public async Task<IActionResult> GetForSubscription(Guid subscriptionId, CancellationToken cancellationToken)
    {
        var addons = await addonRepository.GetForSubscriptionAsync(subscriptionId, cancellationToken);
        return Ok(addons.Select(SubscriptionAddonDto.FromDomain));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAddonRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateSubscriptionAddonCommand(
            request.SubscriptionId, request.EntitlementKey, request.Units,
            request.StartsAt, request.ExpiresAt, request.PriceCrc, GetActorId()), cancellationToken);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
        return CreatedAtAction(nameof(GetForSubscription),
            new { subscriptionId = result.Value.SubscriptionId }, result.Value);
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivateSubscriptionAddonCommand(id, GetActorId()), cancellationToken);
        if (result.IsFailure)
            return NotFound(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/replace")]
    public async Task<IActionResult> Replace(
        Guid id,
        [FromBody] ReplaceAddonRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReplaceSubscriptionAddonCommand(
            id, request.EntitlementKey, request.Units, request.PriceCrc,
            request.ExpiresAt, GetActorId()), cancellationToken);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
        return Ok(result.Value);
    }

    private Guid GetActorId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)
            ? actorId
            : Guid.Empty;
}

public sealed record CreateAddonRequest(
    Guid SubscriptionId,
    string EntitlementKey,
    decimal Units,
    DateTimeOffset StartsAt,
    DateTimeOffset ExpiresAt,
    decimal? PriceCrc = null);

public sealed record ReplaceAddonRequest(
    string EntitlementKey,
    decimal Units,
    decimal PriceCrc,
    DateTimeOffset ExpiresAt);
