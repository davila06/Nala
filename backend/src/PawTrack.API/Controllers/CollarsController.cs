using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Collars.Commands.RegisterCollar;
using PawTrack.Application.Collars.Commands.GenerateCollarDeviceKey;
using PawTrack.Application.Collars.Commands.ActivateCollarLostMode;
using PawTrack.Application.Collars.Commands.CancelCollarHandoverCode;
using PawTrack.Application.Collars.Commands.CreateCollarSafeZone;
using PawTrack.Application.Collars.Commands.DeactivateCollarLostMode;
using PawTrack.Application.Collars.Commands.DeleteCollarSafeZone;
using PawTrack.Application.Collars.Commands.GenerateCollarHandoverCode;
using PawTrack.Application.Collars.Commands.RedeemCollarHandoverCode;
using PawTrack.Application.Collars.Commands.UpdateCollarNotificationPreferences;
using PawTrack.Application.Collars.Commands.UpdateCollarSafeZone;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Collars.Queries.GetCollarAuditLog;
using PawTrack.Application.Collars.Queries.GetCollarConnectivityStatus;
using PawTrack.Application.Collars.Queries.GetCollarLocationHistoryRange;
using PawTrack.Application.Collars.Queries.GetCollarLostModeStatus;
using PawTrack.Application.Collars.Queries.GetCollarSafeZones;
using PawTrack.Application.Collars.Queries.GetCollarStatus;
using PawTrack.Application.Collars.Queries.GetLocationHistory;
using PawTrack.Domain.Collars;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/collars")]
[Authorize]
public sealed class CollarsController(ISender sender) : ControllerBase
{
    // ── GET /api/collars/pet/{petId} ─────────────────────────────────────────
    [HttpGet("pet/{petId:guid}")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(Guid petId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetCollarStatusQuery(petId, userId), cancellationToken);
        if (result.IsFailure)
            return result.Errors.Contains("Access denied.") ? Forbid() : NotFound();
        return Ok(result.Value);
    }

    // ── POST /api/collars ────────────────────────────────────────────────────
    [HttpPost]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterCollarRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new RegisterCollarCommand(request.PetId, userId, request.Provider, request.ExternalDeviceId),
            cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Created($"api/collars/pet/{request.PetId}", result.Value);
    }

    // ── GET /api/collars/pet/{petId}/history?hours=24 ────────────────────────
    [HttpGet("pet/{petId:guid}/history")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        Guid petId,
        [FromQuery] int hours = 24,
        [FromQuery] int maxPoints = 500,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetLocationHistoryQuery(petId, userId, hours, maxPoints), cancellationToken);
        if (result.IsFailure)
            return result.Errors.Contains("Access denied.") ? Forbid() : BadRequest(result.Errors);
        return Ok(result.Value);
    }

    // ── POST /api/collars/pet/{petId}/location ────────────────────────────────
    /// <summary>Manual location record — for generic/own hardware via HTTP push.</summary>
    [HttpPost("pet/{petId:guid}/location")]
    [EnableRateLimiting("location-update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordLocation(
        Guid petId,
        [FromBody] RecordLocationRequest request,
        [FromServices] ICollarRepository collarRepository,
        [FromServices] PawTrack.Application.Common.Interfaces.ILostPetRepository lostPetRepository,
        [FromServices] PawTrack.Application.Collars.Services.CollarSafeZoneEvaluationService safeZoneEvaluationService,
        [FromServices] PawTrack.Application.Common.Interfaces.IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var collar = await collarRepository.GetActiveForPetAsync(petId, cancellationToken);
        if (collar is null) return NotFound();

        // BOLA guard: caller must be either the collar's own device key (X-Collar-Key)
        // or the pet's owner authenticated via JWT — never an arbitrary user/collar.
        var deviceCollarIdClaim = User.FindFirstValue("CollarId");
        if (deviceCollarIdClaim is not null)
        {
            if (!Guid.TryParse(deviceCollarIdClaim, out var deviceCollarId) || deviceCollarId != collar.Id)
                return Forbid();
        }
        else
        {
            if (!TryGetUserId(out var userId) || userId != collar.OwnerId)
                return Forbid();
        }

        collar.UpdateLocation(request.Lat, request.Lng, request.BatteryPercent);
        collarRepository.Update(collar);
        await collarRepository.AddLocationAsync(
            CollarLocation.Record(collar.Id, request.Lat, request.Lng, request.Accuracy),
            cancellationToken);

        if (collar.IsLost && collar.LostPetEventId is not null)
        {
            var lostPetEvent = await lostPetRepository.GetByIdAsync(collar.LostPetEventId.Value, cancellationToken);
            if (lostPetEvent is not null)
            {
                lostPetEvent.UpdateLastSeenLocation(request.Lat, request.Lng, DateTimeOffset.UtcNow);
                lostPetRepository.Update(lostPetEvent);
            }
        }

        await safeZoneEvaluationService.EvaluateAsync(collar, request.Lat, request.Lng, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // ── GET /api/collars/tractive/connect?petId={petId} ───────────────────────
    /// <summary>Initiates Tractive OAuth2 flow. Redirects to Tractive consent screen.</summary>
    [HttpGet("tractive/connect")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult ConnectTraactive([FromQuery] Guid petId, [FromServices] ITractiveService tractive)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var state = $"{userId}:{petId}";
        var authUrl = tractive.GetAuthorizationUrl(state);
        return Redirect(authUrl);
    }

    // ── GET /api/collars/tractive/callback ────────────────────────────────────
    [HttpGet("tractive/callback")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> TractiveCallback(
        [FromQuery] string code,
        [FromQuery] string state,
        [FromServices] ITractiveService tractive,
        [FromServices] ICollarRepository collarRepository,
        CancellationToken cancellationToken)
    {
        var parts = state.Split(':');
        if (parts.Length != 2
            || !Guid.TryParse(parts[0], out var userId)
            || !Guid.TryParse(parts[1], out var petId))
            return BadRequest("Invalid state.");

        var encryptedToken = await tractive.ExchangeCodeForTokenAsync(code, cancellationToken);

        var collar = await collarRepository.GetActiveForPetAsync(petId, cancellationToken);
        if (collar is not null && collar.Provider == Domain.Collars.CollarProvider.Tractive)
        {
            collar.SetToken(encryptedToken);
            collarRepository.Update(collar);
        }

        // Redirect to frontend GPS tab after OAuth
        return Redirect($"https://pawtrack.cr/pets/{petId}?tab=gps&connected=true");
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }

    // ── POST /api/collars/{collarId}/generate-key ─────────────────────────────
    /// <summary>Generates a device key for any active collar owned by the user (OEM/Generic push support).</summary>
    [HttpPost("{collarId:guid}/generate-key")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GenerateDeviceKey(Guid collarId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new GenerateCollarDeviceKeyCommand(collarId, userId), cancellationToken);

        if (result.IsFailure)
            return result.Errors.Contains("Access denied.")
                ? Forbid()
                : UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Ok(result.Value);
    }

    // ── GET /api/collars/{collarId}/connectivity-status ────────────────────────
    [HttpGet("{collarId:guid}/connectivity-status")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConnectivityStatus(Guid collarId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetCollarConnectivityStatusQuery(collarId, userId), cancellationToken);
        if (result.IsFailure)
            return result.Errors.Contains("Access denied.") ? Forbid() : NotFound();
        return Ok(result.Value);
    }

    // ── PUT /api/collars/{collarId}/notification-preferences ───────────────────
    [HttpPut("{collarId:guid}/notification-preferences")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateNotificationPreferences(
        Guid collarId,
        [FromBody] UpdateCollarNotificationPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new UpdateCollarNotificationPreferencesCommand(
                collarId, userId,
                request.OfflineAlertsEnabled, request.OfflineThresholdMinutes,
                request.BatteryAlertsEnabled, request.BatteryAlertThresholdPercent),
            cancellationToken);

        if (result.IsFailure)
            return result.Errors.Contains("Access denied.")
                ? Forbid()
                : UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Ok(result.Value);
    }

    // ── GET /api/collars/{collarId}/audit-log ────────────────────────────────────
    [HttpGet("{collarId:guid}/audit-log")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLog(
        Guid collarId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetCollarAuditLogQuery(collarId, userId, skip, take), cancellationToken);
        if (result.IsFailure)
            return result.Errors.Contains("Access denied.") ? Forbid() : NotFound();
        return Ok(result.Value);
    }

    // ── POST /api/collars/{collarId}/handover/generate ────────────────────────────
    /// <summary>Generates a one-time PIN to transfer ownership of a PawTrack collar with a physical serial.</summary>
    [HttpPost("{collarId:guid}/handover/generate")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GenerateHandoverCode(Guid collarId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new GenerateCollarHandoverCodeCommand(collarId, userId), cancellationToken);
        if (result.IsFailure)
            return result.Errors.Contains("Access denied.")
                ? Forbid()
                : UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Ok(result.Value);
    }

    // ── POST /api/collars/{collarId}/handover/cancel ──────────────────────────────
    [HttpPost("handover/{handoverCodeId:guid}/cancel")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CancelHandoverCode(Guid handoverCodeId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new CancelCollarHandoverCodeCommand(handoverCodeId, userId), cancellationToken);
        if (result.IsFailure)
            return result.Errors.Contains("Access denied.")
                ? Forbid()
                : UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return NoContent();
    }

    // ── POST /api/collars/handover/redeem ──────────────────────────────────
    /// <summary>New owner redeems the PIN to release the serial for reactivation.</summary>
    [HttpPost("handover/redeem")]
    [EnableRateLimiting("handover-verify")] // 5 attempts/min per IP — brute-force guard
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RedeemHandoverCode(
        [FromBody] RedeemCollarHandoverCodeRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new RedeemCollarHandoverCodeCommand(request.HandoverCodeId, request.Pin, userId), cancellationToken);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Ok(result.Value);
    }

    // ── POST /api/collars/{collarId}/lost-mode/activate ────────────────────────────
    [HttpPost("{collarId:guid}/lost-mode/activate")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ActivateLostMode(Guid collarId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new ActivateCollarLostModeCommand(collarId, userId), cancellationToken);
        if (result.IsFailure)
            return result.Errors.Contains("Access denied.")
                ? Forbid()
                : UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Ok(result.Value);
    }

    // ── POST /api/collars/{collarId}/lost-mode/deactivate ─────────────────────────
    [HttpPost("{collarId:guid}/lost-mode/deactivate")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeactivateLostMode(
        Guid collarId,
        [FromBody] DeactivateLostModeRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new DeactivateCollarLostModeCommand(collarId, userId, request?.Reason), cancellationToken);
        if (result.IsFailure)
            return result.Errors.Contains("Access denied.")
                ? Forbid()
                : UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return NoContent();
    }

    // ── GET /api/collars/{collarId}/lost-mode-status ───────────────────────────
    [HttpGet("{collarId:guid}/lost-mode-status")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLostModeStatus(Guid collarId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetCollarLostModeStatusQuery(collarId, userId), cancellationToken);
        if (result.IsFailure)
            return result.Errors.Contains("Access denied.") ? Forbid() : NotFound();
        return Ok(result.Value);
    }

    // ── POST /api/collars/{collarId}/safe-zones ───────────────────────────────────
    [HttpPost("{collarId:guid}/safe-zones")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateSafeZone(
        Guid collarId,
        [FromBody] CreateCollarSafeZoneRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new CreateCollarSafeZoneCommand(collarId, userId, request.Name, request.PolygonJson), cancellationToken);
        if (result.IsFailure)
            return result.Errors.Contains("Access denied.")
                ? Forbid()
                : UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Created($"api/collars/{collarId}/safe-zones/{result.Value!.Id}", result.Value);
    }

    // ── GET /api/collars/{collarId}/safe-zones ────────────────────────────────────
    [HttpGet("{collarId:guid}/safe-zones")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSafeZones(Guid collarId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await sender.Send(new GetCollarSafeZonesQuery(collarId, userId), cancellationToken);
        if (result.IsFailure)
            return result.Errors.Contains("Access denied.") ? Forbid() : NotFound();
        return Ok(result.Value);
    }

    // ── PUT /api/collars/safe-zones/{zoneId} ──────────────────────────────────
    [HttpPut("safe-zones/{zoneId:guid}")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateSafeZone(
        Guid zoneId,
        [FromBody] UpdateCollarSafeZoneRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new UpdateCollarSafeZoneCommand(zoneId, userId, request.Name, request.PolygonJson, request.Enabled),
            cancellationToken);
        if (result.IsFailure)
            return result.Errors.Contains("Access denied.")
                ? Forbid()
                : UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return Ok(result.Value);
    }

    // ── DELETE /api/collars/safe-zones/{zoneId} ──────────────────────────────
    [HttpDelete("safe-zones/{zoneId:guid}")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteSafeZone(Guid zoneId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(new DeleteCollarSafeZoneCommand(zoneId, userId), cancellationToken);
        if (result.IsFailure)
            return result.Errors.Contains("Access denied.")
                ? Forbid()
                : UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });

        return NoContent();
    }

    // ── GET /api/collars/{collarId}/location-history?from=&to=&maxPoints= ──────
    [HttpGet("{collarId:guid}/location-history")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCollarLocationHistory(
        Guid collarId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int maxPoints = 2000,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new GetCollarLocationHistoryRangeQuery(
                collarId, userId,
                from ?? DateTimeOffset.UtcNow.AddDays(-30), to ?? DateTimeOffset.UtcNow, maxPoints),
            cancellationToken);
        if (result.IsFailure)
            return result.Errors.Contains("Access denied.") ? Forbid() : NotFound();
        return Ok(result.Value);
    }

    // ── GET /api/collars/{collarId}/location-history/export.csv ────────────
    [HttpGet("{collarId:guid}/location-history/export.csv")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCollarLocationHistoryCsv(
        Guid collarId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new GetCollarLocationHistoryRangeQuery(
                collarId, userId,
                from ?? DateTimeOffset.UtcNow.AddDays(-30), to ?? DateTimeOffset.UtcNow, MaxPoints: 10_000),
            cancellationToken);
        if (result.IsFailure)
            return result.Errors.Contains("Access denied.") ? Forbid() : NotFound();

        var csv = new System.Text.StringBuilder("lat,lng,accuracy_m,recorded_at\n");
        foreach (var p in result.Value!)
            csv.Append(System.Globalization.CultureInfo.InvariantCulture, $"{p.Lat},{p.Lng},{p.Accuracy},{p.RecordedAt:o}\n");

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"collar-{collarId}-history.csv");
    }

    // ── GET /api/collars/{collarId}/location-heatmap?days=7 ──────────────────
    /// <summary>Convenience wrapper over the range query with heatmap-friendly defaults (wider window, more points).</summary>
    [HttpGet("{collarId:guid}/location-heatmap")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCollarLocationHeatmap(
        Guid collarId,
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await sender.Send(
            new GetCollarLocationHistoryRangeQuery(
                collarId, userId,
                DateTimeOffset.UtcNow.AddDays(-Math.Clamp(days, 1, 30)), DateTimeOffset.UtcNow, MaxPoints: 10_000),
            cancellationToken);
        if (result.IsFailure)
            return result.Errors.Contains("Access denied.") ? Forbid() : NotFound();
        return Ok(result.Value);
    }
}

public sealed record RegisterCollarRequest(Guid PetId, CollarProvider Provider, string? ExternalDeviceId);
public sealed record RecordLocationRequest(double Lat, double Lng, int? BatteryPercent, int? Accuracy);
public sealed record UpdateCollarNotificationPreferencesRequest(
    bool OfflineAlertsEnabled,
    int OfflineThresholdMinutes,
    bool BatteryAlertsEnabled,
    int BatteryAlertThresholdPercent);
public sealed record RedeemCollarHandoverCodeRequest(Guid HandoverCodeId, string Pin);
public sealed record DeactivateLostModeRequest(string? Reason);
public sealed record CreateCollarSafeZoneRequest(string Name, string PolygonJson);
public sealed record UpdateCollarSafeZoneRequest(string Name, string PolygonJson, bool Enabled);
