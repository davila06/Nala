using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PawTrack.Application.Clinics.Commands.ManageWidgetDomain;
using PawTrack.Application.Clinics.Interfaces;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/admin/clinics/{clinicId:guid}/widget-domains")]
[Authorize(Roles = "Admin")]
public sealed class AdminClinicWidgetDomainsController(
    ISender sender,
    IClinicWidgetDomainRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid clinicId, CancellationToken cancellationToken) =>
        Ok((await repository.GetForClinicAsync(clinicId, cancellationToken))
            .Select(ClinicWidgetDomainDto.FromDomain));

    [HttpPost]
    public async Task<IActionResult> Add(
        Guid clinicId,
        [FromBody] AddWidgetDomainRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddClinicWidgetDomainCommand(clinicId, request.Domain, GetActorId()), cancellationToken);
        if (result.IsFailure)
            return UnprocessableEntity(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
        return Ok(result.Value);
    }

    [HttpDelete("{domainId:guid}")]
    public async Task<IActionResult> Remove(Guid domainId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveClinicWidgetDomainCommand(domainId, GetActorId()), cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Detail = string.Join(", ", result.Errors) });
    }

    private Guid GetActorId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)
            ? actorId
            : Guid.Empty;
}

public sealed record AddWidgetDomainRequest(string Domain);
