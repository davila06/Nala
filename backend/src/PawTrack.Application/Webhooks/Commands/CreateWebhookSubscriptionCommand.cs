using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Webhooks;

namespace PawTrack.Application.Webhooks.Commands;

public sealed record CreateWebhookSubscriptionCommand(
    Guid OwnerUserId, string EndpointUrl, string Secret, IReadOnlyList<string> EventTypes)
    : IRequest<Result<Guid>>;

public sealed class CreateWebhookSubscriptionCommandHandler(
    IDataProtectionService dataProtectionService,
    IWebhookRepository webhookRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateWebhookSubscriptionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateWebhookSubscriptionCommand request, CancellationToken ct)
    {
        if (!Uri.TryCreate(request.EndpointUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            return Result.Failure<Guid>("El endpoint debe usar HTTPS.");
        if (request.EventTypes.Count == 0 || request.EventTypes.Count > 50)
            return Result.Failure<Guid>("Debe indicar entre 1 y 50 eventos.");
        var protectedSecret = dataProtectionService.Protect(request.Secret);
        var subscription = WebhookSubscription.Create(request.OwnerUserId, uri.ToString(), protectedSecret, request.EventTypes);
        await webhookRepository.AddSubscriptionAsync(subscription, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(subscription.Id);
    }
}