using MediatR;
using Microsoft.AspNetCore.Mvc;
using PawTrack.Application.Subscriptions.Queries.GetAdminSubscriptionPlans;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/catalog/subscription-plans")]
public sealed class PublicSubscriptionPlansController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetAdminSubscriptionPlansQuery(false, 0, 100), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }
}
