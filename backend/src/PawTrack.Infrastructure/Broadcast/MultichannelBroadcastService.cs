using Microsoft.Extensions.Logging;
using PawTrack.Application.Broadcast.DTOs;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Broadcast;

namespace PawTrack.Infrastructure.Broadcast;

/// <summary>
/// Fans out a lost-pet broadcast to all registered <see cref="IChannelBroadcaster"/>
/// implementations in parallel. Each channel is isolated: a failure in one never
/// causes another channel to be skipped.
///
/// Per-attempt persistence happens here so the orchestrator owns the full
/// lifecycle: create pending → dispatch → mark sent/failed/skipped.
/// </summary>
public sealed class MultichannelBroadcastService(
    IEnumerable<IChannelBroadcaster> channelBroadcasters,
    IBroadcastAttemptRepository attemptRepository,
    ITrackingLinkService trackingLinkService,
    IUnitOfWork unitOfWork,
    ILogger<MultichannelBroadcastService> logger)
    : IMultichannelBroadcastService
{
    public async Task<IReadOnlyList<BroadcastAttemptDto>> BroadcastAsync(
        BroadcastMessageContext context,
        CancellationToken cancellationToken = default)
    {
        var broadcasters = channelBroadcasters.ToList();

        if (broadcasters.Count == 0)
        {
            logger.LogWarning(
                "No channel broadcasters are registered. Broadcast for event {EventId} produced no output.",
                context.LostPetEventId);
            return [];
        }

        // ── Create pending attempts up front ─────────────────────────────────
        // Persisting before sending ensures we have a record even if the process
        // crashes mid-fan-out.
        var attempts = new List<(BroadcastAttempt Attempt, IChannelBroadcaster Broadcaster)>();

        foreach (var broadcaster in broadcasters)
        {
            var trackingUrl = trackingLinkService.Generate(
                context.LostPetEventId,
                broadcaster.Channel.ToString().ToLowerInvariant());

            var attempt = BroadcastAttempt.CreatePending(
                context.LostPetEventId,
                broadcaster.Channel,
                trackingUrl);

            await attemptRepository.AddAsync(attempt, cancellationToken);
            attempts.Add((attempt, broadcaster));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // ── Dispatch in parallel — each channel isolated ──────────────────────
        var tasks = attempts.Select(pair => DispatchChannelAsync(pair.Attempt, pair.Broadcaster, context, cancellationToken));
        await Task.WhenAll(tasks);

        // ── Persist final statuses ────────────────────────────────────────────
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return attempts
            .Select(p => BroadcastAttemptDto.FromDomain(p.Attempt))
            .ToList()
            .AsReadOnly();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task DispatchChannelAsync(
        BroadcastAttempt attempt,
        IChannelBroadcaster broadcaster,
        BroadcastMessageContext context,
        CancellationToken cancellationToken)
    {
        if (!broadcaster.IsEnabled)
        {
            attempt.MarkSkipped($"Channel {broadcaster.Channel} is not enabled in the current configuration.");
            logger.LogDebug("Channel {Channel} skipped (not enabled) for event {EventId}",
                broadcaster.Channel, context.LostPetEventId);
            return;
        }

        try
        {
            // Inject the per-channel tracking URL into the context
            var channelContext = context with { TrackingUrl = attempt.TrackingUrl ?? context.TrackingUrl };

            var externalId = await broadcaster.SendAsync(channelContext, cancellationToken);
            attempt.MarkSent(externalId);

            logger.LogInformation(
                "Channel {Channel} broadcast succeeded for event {EventId}. ExternalId={ExternalId}",
                broadcaster.Channel, context.LostPetEventId, externalId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var safeMessage = $"{ex.GetType().Name}: {ex.Message}";
            attempt.MarkFailed(safeMessage);

            // Log as Warning, not Error — the overall broadcast may still succeed
            // via other channels. A full Error-level alert fires only if ALL channels fail
            // (checked at the caller / monitoring layer).
            logger.LogWarning(ex,
                "Channel {Channel} broadcast failed for event {EventId}: {Error}",
                broadcaster.Channel, context.LostPetEventId, safeMessage);
        }
    }
}
