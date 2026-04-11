using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Incentives.Queries.GetLeaderboard;
using PawTrack.Application.Incentives.Queries.GetMyScore;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/incentives")]
public sealed class IncentivesController(ISender sender) : ControllerBase
{
    // ── GET /api/incentives/leaderboard?take=10 ───────────────────────────────
    [HttpGet("leaderboard")]
    [AllowAnonymous]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard(
        [FromQuery] int take = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetLeaderboardQuery(take), cancellationToken);
        return Ok(result.Value);
    }

    // ── GET /api/incentives/my-score ──────────────────────────────────────────
    [HttpGet("my-score")]
    [Authorize]    [EnableRateLimiting("public-api")] // 30/min — each call issues GetMyScoreQuery (DB SELECT); sibling GetLeaderboard already throttled    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyScore(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await sender.Send(new GetMyScoreQuery(userId), cancellationToken);
        return Ok(result.Value);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out userId);
    }
}
