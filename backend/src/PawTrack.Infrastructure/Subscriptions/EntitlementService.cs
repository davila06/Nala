using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;

namespace PawTrack.Infrastructure.Subscriptions;

public sealed class EntitlementService(
    ISubscriptionRepository subscriptionRepository,
    ISubscriptionPlanRepository planRepository,
    IEntitlementRepository entitlementRepository,
    IUnitOfWork unitOfWork,
    ILogger<EntitlementService>? logger = null,
    TelemetryClient? telemetryClient = null) : IEntitlementService
{
    public async Task<EntitlementSnapshot> GetSnapshotAsync(
        Guid subjectId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await subscriptionRepository.GetActiveForSubjectAsync(subjectId, cancellationToken);
        if (subscription is null)
            return new EntitlementSnapshot(subjectId, null, SubscriptionTier.Free, null, null,
                FreeEntitlements());

        var plan = await planRepository.GetByTierAsync(subscription.Tier, cancellationToken);
        if (plan is null)
            return new EntitlementSnapshot(subjectId, subscription.Id, subscription.Tier,
                subscription.StartsAt ?? subscription.ActivatedAt,
                subscription.ExpiresAt,
                new Dictionary<string, EntitlementValue>(StringComparer.Ordinal));

        var definitions = await entitlementRepository.GetActiveForPlanAsync(plan.Id, cancellationToken);
        var addons = await entitlementRepository.GetActiveAddonsAsync(subscription.Id, DateTimeOffset.UtcNow, cancellationToken) ?? [];
        var cycleStart = definitions.Any(x => string.Equals(x.ResetPeriod, "monthly", StringComparison.OrdinalIgnoreCase))
            ? new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero)
            : subscription.StartsAt ?? subscription.ActivatedAt;
        var cycleEnd = definitions.Any(x => string.Equals(x.ResetPeriod, "monthly", StringComparison.OrdinalIgnoreCase))
            ? cycleStart?.AddMonths(1)
            : subscription.ExpiresAt;
        var consumed = cycleStart.HasValue && cycleEnd.HasValue
            ? await entitlementRepository.GetConsumedBySubjectAsync(subjectId, cycleStart.Value, cycleEnd.Value, cancellationToken)
            : new Dictionary<string, decimal>(StringComparer.Ordinal);
        var values = definitions.ToDictionary(
            x => x.EntitlementKey,
            x => new EntitlementValue(x.EntitlementKey, x.ValueType, x.NumericValue,
                x.BooleanValue, x.TextValue, x.Unit, x.ResetPeriod,
                consumed.GetValueOrDefault(x.EntitlementKey)),
            StringComparer.Ordinal);

        foreach (var addon in addons)
        {
            if (values.TryGetValue(addon.EntitlementKey, out var current)
                && current.NumericValue is not null)
            {
                values[addon.EntitlementKey] = current with
                {
                    NumericValue = current.NumericValue.Value + addon.Units,
                };
            }
        }

        return new EntitlementSnapshot(subjectId, subscription.Id, subscription.Tier,
            cycleStart, cycleEnd, values);
    }

    public async Task<EntitlementDecision> AuthorizeAsync(
        Guid subjectId,
        string entitlement,
        decimal requestedUnits,
        EntitlementContext context,
        CancellationToken cancellationToken = default)
    {
        if (requestedUnits <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestedUnits));

        var subscription = await subscriptionRepository.GetActiveForSubjectAsync(subjectId, cancellationToken);
        if (subscription is null)
        {
            var freeEntitlements = FreeEntitlements();
            if (!freeEntitlements.TryGetValue(entitlement, out var freeDefinition))
                return new EntitlementDecision(false, false, null, 0m, 0m, null, SubscriptionTier.Free);
            if (freeDefinition.ValueType == EntitlementValueType.Boolean)
                return new EntitlementDecision(freeDefinition.BooleanValue == true, true, null, 0m, 0m, null, SubscriptionTier.Free);

            var freeStart = string.Equals(freeDefinition.ResetPeriod, "monthly", StringComparison.OrdinalIgnoreCase)
                ? new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero)
                : DateTimeOffset.UtcNow.Date;
            var freeEnd = string.Equals(freeDefinition.ResetPeriod, "monthly", StringComparison.OrdinalIgnoreCase)
                ? freeStart.AddMonths(1)
                : freeStart.AddYears(100);
            var freeConsumed = await entitlementRepository.GetConsumedAsync(
                subjectId, entitlement, freeStart, freeEnd, context.ContextType, context.ContextId, cancellationToken);
            var freeRemaining = Math.Max(0m, (freeDefinition.NumericValue ?? 0m) - freeConsumed);
            return new EntitlementDecision(requestedUnits <= freeRemaining, true,
                freeDefinition.NumericValue, freeConsumed, freeRemaining,
                freeDefinition.ResetPeriod is null ? null : freeEnd, SubscriptionTier.Free);
        }

        var plan = await planRepository.GetByTierAsync(subscription.Tier, cancellationToken);
        var definition = plan is null
            ? null
            : (await entitlementRepository.GetActiveForPlanAsync(plan.Id, cancellationToken))
                .FirstOrDefault(x => x.EntitlementKey == entitlement);

        if (definition is null)
        {
            var fallback = GetLegacyEntitlement(subscription.Tier, entitlement);
            if (fallback is null)
                return new EntitlementDecision(false, false, null, 0m, 0m, null, subscription.Tier);

            var fallbackCycle = ResolveCycle(subscription, fallback.ResetPeriod);
            var fallbackConsumed = await entitlementRepository.GetConsumedAsync(
                subjectId, entitlement, fallbackCycle.Start, fallbackCycle.End, context.ContextType, context.ContextId, cancellationToken);
            var fallbackRemaining = Math.Max(0m, fallback.NumericValue!.Value - fallbackConsumed);
            return new EntitlementDecision(requestedUnits <= fallbackRemaining, true,
                fallback.NumericValue, fallbackConsumed, fallbackRemaining,
                fallback.ResetPeriod is null ? null : fallbackCycle.End, subscription.Tier);
        }

        var activeAddons = await entitlementRepository.GetActiveAddonsAsync(
            subscription.Id, DateTimeOffset.UtcNow, cancellationToken) ?? [];
        var addonUnits = activeAddons
            .Where(addon => addon.EntitlementKey == entitlement)
            .Sum(addon => addon.Units);
        if (addonUnits > 0 && definition.NumericValue is not null)
            definition = PlanEntitlementValueWithLimit(definition, definition.NumericValue.Value + addonUnits);

        if (definition.ValueType == EntitlementValueType.Boolean)
            return new EntitlementDecision(definition.BooleanValue == true, true, null, 0m, 0m, null, subscription.Tier);

        if (definition.ValueType != EntitlementValueType.Numeric || definition.NumericValue is null)
            return new EntitlementDecision(true, true, null, 0m, 0m, null, subscription.Tier);

        var cycle = ResolveCycle(subscription, definition.ResetPeriod);
        var consumed = await entitlementRepository.GetConsumedAsync(
            subjectId, entitlement, cycle.Start, cycle.End, context.ContextType, context.ContextId, cancellationToken);
        var remaining = Math.Max(0m, definition.NumericValue.Value - consumed);
        return new EntitlementDecision(
            requestedUnits <= remaining,
            true,
            definition.NumericValue,
            consumed,
            remaining,
            definition.ResetPeriod is null ? null : cycle.End,
            subscription.Tier);
    }

    public async Task<ConsumptionResult> ConsumeAsync(
        Guid subjectId,
        string entitlement,
        decimal units,
        string idempotencyKey,
        EntitlementContext context,
        CancellationToken cancellationToken = default)
    {
        if (units <= 0) throw new ArgumentOutOfRangeException(nameof(units));
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));

        var existing = await entitlementRepository.FindByIdempotencyKeyAsync(
            subjectId, entitlement, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            var existingSubscription = await subscriptionRepository.GetActiveForSubjectAsync(subjectId, cancellationToken);
            var consumed = await entitlementRepository.GetConsumedAsync(
                subjectId, entitlement, existing.CycleStart, existing.CycleEnd,
                existing.ContextType, existing.ContextId, cancellationToken);
            var existingDecision = await AuthorizeAsync(subjectId, entitlement, 1m, context, cancellationToken);
            return new ConsumptionResult(true, true, consumed, existingDecision.Remaining,
                existing.CycleStart, existing.CycleEnd, existing.CycleEnd,
                existingSubscription?.Tier ?? SubscriptionTier.Free);
        }

        var decision = await AuthorizeAsync(subjectId, entitlement, units, context, cancellationToken);
        if (!decision.Allowed)
        {
            logger?.LogInformation("Entitlement denied for {Entitlement} on tier {Tier}", entitlement, decision.Tier);
            telemetryClient?.TrackEvent("EntitlementDenied", new Dictionary<string, string>
            {
                ["entitlement"] = entitlement,
                ["tier"] = decision.Tier.ToString(),
                ["contextType"] = context.ContextType ?? "unknown",
            });
            return new ConsumptionResult(false, false, decision.Consumed, decision.Remaining,
                null, null, decision.ResetsAt, decision.Tier);
        }

        var subscription = await subscriptionRepository.GetActiveForSubjectAsync(subjectId, cancellationToken);
        var cycleStart = decision.ResetsAt is null
            ? subscription?.StartsAt ?? subscription?.ActivatedAt ?? DateTimeOffset.UtcNow
            : new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var cycleEnd = decision.ResetsAt ?? subscription?.ExpiresAt ?? cycleStart.AddMonths(1);
        var pending = EntitlementConsumption.Create(subjectId, subscription?.Id, entitlement, units,
            idempotencyKey, cycleStart, cycleEnd, context.ContextType, context.ContextId);
        await entitlementRepository.AddAsync(pending, cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            entitlementRepository.Detach(pending);
            var raced = await entitlementRepository.FindByIdempotencyKeyAsync(
                subjectId, entitlement, idempotencyKey, cancellationToken);
            if (raced is null) throw;

            var racedConsumed = await entitlementRepository.GetConsumedAsync(
                subjectId, entitlement, raced.CycleStart, raced.CycleEnd,
                raced.ContextType, raced.ContextId, cancellationToken);
            return new ConsumptionResult(true, true, racedConsumed, 0m,
                raced.CycleStart, raced.CycleEnd, raced.CycleEnd, subscription?.Tier ?? SubscriptionTier.Free);
        }

        telemetryClient?.TrackEvent("EntitlementConsumed", new Dictionary<string, string>
        {
            ["entitlement"] = entitlement,
            ["tier"] = decision.Tier.ToString(),
            ["contextType"] = context.ContextType ?? "unknown",
        }, new Dictionary<string, double> { ["units"] = (double)units });

        return new ConsumptionResult(true, false, decision.Consumed + units,
            Math.Max(0m, decision.Remaining - units), cycleStart, cycleEnd,
            decision.ResetsAt, decision.Tier);
    }

    private static (DateTimeOffset Start, DateTimeOffset End) ResolveCycle(
        Subscription subscription,
        string? resetPeriod)
    {
        if (string.Equals(resetPeriod, "daily", StringComparison.OrdinalIgnoreCase))
        {
            var startOfDay = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero);
            return (startOfDay, startOfDay.AddDays(1));
        }

        if (!string.Equals(resetPeriod, "monthly", StringComparison.OrdinalIgnoreCase))
        {
            var start = subscription.StartsAt ?? subscription.ActivatedAt ?? DateTimeOffset.UtcNow;
            return (start, subscription.ExpiresAt ?? start.AddMonths(1));
        }

        var startOfMonth = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        return (startOfMonth, startOfMonth.AddMonths(1));
    }

    private static IReadOnlyDictionary<string, EntitlementValue> FreeEntitlements() =>
        new Dictionary<string, EntitlementValue>(StringComparer.Ordinal)
        {
            ["MaxPets"] = new("MaxPets", EntitlementValueType.Numeric, 1m, null, null, "account", null, 0m),
            ["MaxClinicScansPerCycle"] = new("MaxClinicScansPerCycle", EntitlementValueType.Numeric, 25m, null, null, "cycle", "monthly", 0m),
            ["PromotionRedemptionsPerCycle"] = new("PromotionRedemptionsPerCycle", EntitlementValueType.Numeric, 1m, null, null, "cycle", "monthly", 0m),
            ["MaxActiveProducts"] = new("MaxActiveProducts", EntitlementValueType.Numeric, 10m, null, null, "account", null, 0m),
            ["MaxActiveAdoptablePets"] = new("MaxActiveAdoptablePets", EntitlementValueType.Numeric, 5m, null, null, "account", null, 0m),
            ["MaxCapturesPerYear"] = new("MaxCapturesPerYear", EntitlementValueType.Numeric, 500m, null, null, "cycle", "yearly", 0m),
            ["MaxActiveServices"] = new("MaxActiveServices", EntitlementValueType.Numeric, 3m, null, null, "account", null, 0m),
            ["MaxScheduleBlocks"] = new("MaxScheduleBlocks", EntitlementValueType.Numeric, 0m, null, null, "account", null, 0m),
            ["MaxOrdersPerCycle"] = new("MaxOrdersPerCycle", EntitlementValueType.Numeric, 0m, null, null, "cycle", "monthly", 0m),
            ["MaxLocations"] = new("MaxLocations", EntitlementValueType.Numeric, 1m, null, null, "account", null, 0m),
            ["MaxAnalyticsExportsPerCycle"] = new("MaxAnalyticsExportsPerCycle", EntitlementValueType.Numeric, 0m, null, null, "cycle", "monthly", 0m),
            ["BulkImportLimit"] = new("BulkImportLimit", EntitlementValueType.Numeric, 0m, null, null, "operation", null, 0m),
            ["MedicalRecordsPreviewLimit"] = new("MedicalRecordsPreviewLimit", EntitlementValueType.Numeric, 3m, null, null, "account", null, 0m),
            ["MaxActivePromotions"] = new("MaxActivePromotions", EntitlementValueType.Numeric, 0m, null, null, "account", null, 0m),
            ["ClinicMedicalExportsPerCycle"] = new("ClinicMedicalExportsPerCycle", EntitlementValueType.Numeric, 20m, null, null, "cycle", "monthly", 0m),
            ["MaxFamilyMembers"] = new("MaxFamilyMembers", EntitlementValueType.Numeric, 5m, null, null, "account", null, 0m),
            ["MaxActiveVetReminders"] = new("MaxActiveVetReminders", EntitlementValueType.Numeric, 50m, null, null, "account", null, 0m),
            ["BroadcastsPerCasePerDay"] = new("BroadcastsPerCasePerDay", EntitlementValueType.Numeric, 1m, null, null, "case", "daily", 0m),
            ["BulkUpdateLimit"] = new("BulkUpdateLimit", EntitlementValueType.Numeric, 0m, null, null, "operation", null, 0m),
        };

    private static EntitlementValue? GetLegacyEntitlement(SubscriptionTier tier, string key) =>
        (tier, key) switch
        {
            (SubscriptionTier.UserPlus, "MaxPets") => new(key, EntitlementValueType.Numeric, 3m, null, null, "account", null, 0m),
            (SubscriptionTier.UserPlus, "MaxGpsCollars") => new(key, EntitlementValueType.Numeric, 1m, null, null, "account", null, 0m),
            (SubscriptionTier.UserFamilia, "MaxPets") => new(key, EntitlementValueType.Numeric, 25m, null, null, "account", null, 0m),
            (SubscriptionTier.UserFamilia, "MaxGpsCollars") => new(key, EntitlementValueType.Numeric, 5m, null, null, "account", null, 0m),
            (SubscriptionTier.UserPlus, "AiMatchesPerCycle") => new(key, EntitlementValueType.Numeric, 10m, null, null, "cycle", "monthly", 0m),
            (SubscriptionTier.UserFamilia, "AiMatchesPerCycle") => new(key, EntitlementValueType.Numeric, 30m, null, null, "cycle", "monthly", 0m),
            (SubscriptionTier.ClinicPlus, "MaxClinicScansPerCycle") => new(key, EntitlementValueType.Numeric, 500m, null, null, "cycle", "monthly", 0m),
            (SubscriptionTier.ClinicPartner, "MaxClinicScansPerCycle") => new(key, EntitlementValueType.Numeric, 5000m, null, null, "cycle", "monthly", 0m),
            (SubscriptionTier.StorePlus, "MaxActiveProducts") => new(key, EntitlementValueType.Numeric, 100m, null, null, "account", null, 0m),
            (SubscriptionTier.StorePartner, "MaxActiveProducts") => new(key, EntitlementValueType.Numeric, 1000m, null, null, "account", null, 0m),
            (SubscriptionTier.ShelterPlus, "MaxActiveAdoptablePets") => new(key, EntitlementValueType.Numeric, 500m, null, null, "account", null, 0m),
            (SubscriptionTier.MuniBasica, "MaxCapturesPerYear") => new(key, EntitlementValueType.Numeric, 500m, null, null, "cycle", "yearly", 0m),
            (SubscriptionTier.MuniFull, "MaxCapturesPerYear") => new(key, EntitlementValueType.Numeric, 5000m, null, null, "cycle", "yearly", 0m),
            (SubscriptionTier.MuniRedRegional, "MaxCapturesPerYear") => new(key, EntitlementValueType.Numeric, 30000m, null, null, "cycle", "yearly", 0m),
            (SubscriptionTier.UserPlus, "MaxActiveServices") => new(key, EntitlementValueType.Numeric, 25m, null, null, "account", null, 0m),
            (SubscriptionTier.UserPlus, "MaxScheduleBlocks") => new(key, EntitlementValueType.Numeric, 20m, null, null, "account", null, 0m),
            (SubscriptionTier.ClinicPartner, "ClinicMedicalExportsPerCycle") => new(key, EntitlementValueType.Numeric, 20m, null, null, "cycle", "monthly", 0m),
            (SubscriptionTier.StorePlus, "MaxOrdersPerCycle") => new(key, EntitlementValueType.Numeric, 250m, null, null, "cycle", "monthly", 0m),
            (SubscriptionTier.StorePartner, "MaxOrdersPerCycle") => new(key, EntitlementValueType.Numeric, 2500m, null, null, "cycle", "monthly", 0m),
            (SubscriptionTier.StorePartner, "MaxLocations") => new(key, EntitlementValueType.Numeric, 5m, null, null, "account", null, 0m),
            (SubscriptionTier.StorePlus, "MaxAnalyticsExportsPerCycle") => new(key, EntitlementValueType.Numeric, 0m, null, null, "cycle", "monthly", 0m),
            (SubscriptionTier.StorePartner, "MaxAnalyticsExportsPerCycle") => new(key, EntitlementValueType.Numeric, 20m, null, null, "cycle", "monthly", 0m),
            (SubscriptionTier.StorePartner, "BulkImportLimit") => new(key, EntitlementValueType.Numeric, 1000m, null, null, "operation", null, 0m),
            (SubscriptionTier.UserPlus, "MedicalRecordsPreviewLimit") => new(key, EntitlementValueType.Numeric, 3m, null, null, "account", null, 0m),
            (SubscriptionTier.UserPlus, "BroadcastsPerCasePerDay") => new(key, EntitlementValueType.Numeric, 5m, null, null, "case", "daily", 0m),
            (SubscriptionTier.UserFamilia, "BroadcastsPerCasePerDay") => new(key, EntitlementValueType.Numeric, 10m, null, null, "case", "daily", 0m),
            (SubscriptionTier.MuniFull, "BulkUpdateLimit") => new(key, EntitlementValueType.Numeric, 500m, null, null, "operation", null, 0m),
            (SubscriptionTier.MuniRedRegional, "BulkUpdateLimit") => new(key, EntitlementValueType.Numeric, 2000m, null, null, "operation", null, 0m),
            (SubscriptionTier.UserFamilia, "MaxFamilyMembers") => new(key, EntitlementValueType.Numeric, 5m, null, null, "account", null, 0m),
            (SubscriptionTier.UserFamilia, "MaxActiveVetReminders") => new(key, EntitlementValueType.Numeric, 50m, null, null, "account", null, 0m),
            _ => null,
        };

    private static PlanEntitlement PlanEntitlementValueWithLimit(PlanEntitlement source, decimal limit)
    {
        return PlanEntitlement.Create(source.PlanId, source.EntitlementKey, source.ValueType,
            limit, source.BooleanValue, source.TextValue, source.Unit, source.ResetPeriod);
    }
}
