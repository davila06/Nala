using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PawTrack.Application.Auth.Commands.AssignSupportRole;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/admin/support")]
[Authorize(Roles = "Admin")]
public sealed class AdminSupportController(ISender sender) : ControllerBase
{
    [HttpPut("users/{userId:guid}/role")]
    public async Task<IActionResult> AssignSupportRole(Guid userId, CancellationToken ct)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var adminUserId)) return Unauthorized();
        var result = await sender.Send(new AssignSupportRoleCommand(adminUserId, userId), ct);
        return result.IsSuccess
            ? NoContent()
            : UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors), Status = 422 });
    }
}
