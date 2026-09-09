using FluentValidation;
using MediatR;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.ProductAnalytics;

namespace PawTrack.Application.ProductAnalytics;

public sealed record IngestProductEventCommand(
    Guid EventId,
    string EventName,
    string SchemaVersion,
    DateTimeOffset OccurredAt,
    string AnonymousId,
    string Source,
    Guid? UserId,
    Guid? PetId,
    string? Canton,
    string? CorrelationId) : IRequest<Result<bool>>;

public sealed class IngestProductEventCommandValidator : AbstractValidator<IngestProductEventCommand>
{
    private static readonly string[] AllowedEvents =
    [
        "PetRegistered", "PetProfileCompleted", "QrGenerated", "QrActivated", "QrScanned",
        "LostPetReported", "SightingCreated", "FoundPetReported", "FirstResponseRecorded",
        "HandoverStarted", "HandoverCompleted", "PetReunited",
    ];

    public IngestProductEventCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.EventName).Must(name => AllowedEvents.Contains(name, StringComparer.Ordinal))
            .WithMessage("Unsupported product event.");
        RuleFor(x => x.SchemaVersion).Equal("1");
        RuleFor(x => x.AnonymousId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Source).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Canton).MaximumLength(100);
        RuleFor(x => x.CorrelationId).MaximumLength(100);
        RuleFor(x => x.OccurredAt)
            .InclusiveBetween(DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow.AddMinutes(5))
            .WithMessage("Event timestamp is outside the accepted window.");
    }
}

public sealed class IngestProductEventCommandHandler(
    IProductEventRepository repository,
    IUnitOfWork unitOfWork,
    IWebhookFanout webhookFanout)
    : IRequestHandler<IngestProductEventCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(IngestProductEventCommand request, CancellationToken cancellationToken)
    {
        if (await repository.ExistsByEventIdAsync(request.EventId, cancellationToken))
            return Result.Success(false);

        var productEvent = ProductEvent.Create(
            request.EventId,
            request.EventName,
            request.SchemaVersion,
            request.OccurredAt,
            request.AnonymousId,
            request.Source,
            request.UserId,
            request.PetId,
            request.Canton,
            request.CorrelationId);

        await repository.AddAsync(productEvent, cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await webhookFanout.QueueAsync(request.EventName, System.Text.Json.JsonSerializer.Serialize(request), cancellationToken);
            return Result.Success(true);
        }
        catch (Exception ex) when (ex.Message.Contains("EventId", StringComparison.OrdinalIgnoreCase)
            || ex.InnerException?.Message.Contains("EventId", StringComparison.OrdinalIgnoreCase) == true)
        {
            // A concurrent retry is still idempotent when the unique index wins.
            return Result.Success(false);
        }
    }
}

public sealed record GetProductFunnelQuery(
    DateTimeOffset From,
    DateTimeOffset To,
    string? Canton = null) : IRequest<Result<ProductFunnelDto>>;

public sealed record ProductFunnelDto(
    DateTimeOffset From,
    DateTimeOffset To,
    string? Canton,
    IReadOnlyDictionary<string, int> Events);

public sealed class GetProductFunnelQueryHandler(IProductEventRepository repository)
    : IRequestHandler<GetProductFunnelQuery, Result<ProductFunnelDto>>
{
    public async Task<Result<ProductFunnelDto>> Handle(GetProductFunnelQuery request, CancellationToken cancellationToken)
    {
        var counts = await repository.CountByEventNameAsync(request.From, request.To, request.Canton, cancellationToken);
        return Result.Success(new ProductFunnelDto(
            request.From,
            request.To,
            request.Canton,
            counts.ToDictionary(x => x.EventName, x => x.Count, StringComparer.Ordinal)));
    }
}