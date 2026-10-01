using MediatR;
using Microsoft.AspNetCore.Mvc;
using PawTrack.Application.Subscriptions.Queries;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/catalog/subscription-plans")]
public sealed class PublicSubscriptionPlansController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetPublicSubscriptionPlansQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }
}
