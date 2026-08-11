using FluentValidation;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Promotions.Interfaces;
using PawTrack.Application.Subscriptions.DTOs;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Promotions;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Promotions;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record PromotionCodeDto(
    Guid Id,
    string Code,
    string Type,
    int? DiscountPercent,
    int? FreeMonths,
    string? TargetTier,
    int MaxRedemptions,
    int RedeemedCount,
    DateTimeOffset? ExpiresAt,
    bool IsActive,
    bool CanRedeem,
    string? AdminNote,
    DateTimeOffset CreatedAt)
{
    public static PromotionCodeDto FromDomain(PromotionCode c) => new(
        c.Id, c.Code, c.Type.ToString(),
        c.DiscountPercent, c.FreeMonths,
        c.TargetTier?.ToString(),
        c.MaxRedemptions, c.RedeemedCount,
        c.ExpiresAt, c.IsActive, c.CanRedeem,
        c.AdminNote, c.CreatedAt);
}

public sealed record PromotionValidationDto(
    string Code,
    string Type,
    string BenefitDescription,
    int? DiscountPercent,
    int? FreeMonths,
    string? TargetTier,
    bool IsFullyFree,
    bool RequiresPayment);

// ── Admin: batch creation ─────────────────────────────────────────────────────

/// <summary>Spec for one "line" in a batch. Quantity controls how many distinct codes are generated.</summary>
public sealed record PromotionCodeSpec(
    PromotionType Type,
    int? DiscountPercent,
    int? FreeMonths,
    SubscriptionTier? TargetTier,
    int MaxRedemptions,
    DateTimeOffset? ExpiresAt,
    string? AdminNote,
    int Quantity);

public sealed record CreatePromotionBatchCommand(
    Guid AdminId,
    IReadOnlyList<PromotionCodeSpec> Specs)
    : IRequest<Result<IReadOnlyList<PromotionCodeDto>>>;

public sealed class CreatePromotionBatchCommandValidator : AbstractValidator<CreatePromotionBatchCommand>
{
    public CreatePromotionBatchCommandValidator()
    {
        RuleFor(x => x.Specs).NotEmpty().WithMessage("At least one spec is required.");
        RuleFor(x => x.Specs).Must(s => s.Sum(spec => spec.Quantity) <= 500)
            .WithMessage("Batch cannot exceed 500 codes total.");

        RuleForEach(x => x.Specs).ChildRules(spec =>
        {
            spec.RuleFor(s => s.Quantity).InclusiveBetween(1, 100);
            spec.RuleFor(s => s.MaxRedemptions)
                .Must(v => v == -1 || v >= 1)
                .WithMessage("MaxRedemptions must be -1 (unlimited) or a positive integer.");

            spec.When(s => s.Type == PromotionType.PercentageDiscount, () =>
            {
                spec.RuleFor(s => s.DiscountPercent)
                    .NotNull()
                    .Must(v => v is 10 or 15 or 100)
                    .WithMessage("Discount must be 10, 15, or 100.");
            });

            spec.When(s => s.Type == PromotionType.FreeTier, () =>
            {
                spec.RuleFor(s => s.TargetTier)
                    .NotNull()
                    .Must(t => t is SubscriptionTier.UserPlus or SubscriptionTier.UserFamilia)
                    .WithMessage("FreeTier requires UserPlus or UserFamilia.");
            });

            spec.When(s => s.Type == PromotionType.FreeMonths, () =>
            {
                spec.RuleFor(s => s.FreeMonths)
                    .NotNull()
                    .Must(v => v is 1 or 3 or 6)
                    .WithMessage("FreeMonths must be 1, 3, or 6.");
                spec.RuleFor(s => s.TargetTier)
                    .NotNull()
                    .Must(t => t is SubscriptionTier.UserPlus or SubscriptionTier.UserFamilia)
                    .WithMessage("FreeMonths requires a target tier.");
            });
        });
    }
}

public sealed class CreatePromotionBatchCommandHandler(
    IPromotionCodeRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreatePromotionBatchCommand, Result<IReadOnlyList<PromotionCodeDto>>>
{
    public async Task<Result<IReadOnlyList<PromotionCodeDto>>> Handle(
        CreatePromotionBatchCommand request, CancellationToken ct)
    {
        var codes = new List<PromotionCode>();

        foreach (var spec in request.Specs)
        {
            for (var i = 0; i < spec.Quantity; i++)
            {
                var code = spec.Type switch
                {
                    PromotionType.PercentageDiscount =>
                        PromotionCode.CreateDiscount(spec.DiscountPercent!.Value, spec.TargetTier,
                            spec.MaxRedemptions, spec.ExpiresAt, request.AdminId, spec.AdminNote),
                    PromotionType.FreeTier =>
                        PromotionCode.CreateFreeTier(spec.TargetTier!.Value,
                            spec.MaxRedemptions, spec.ExpiresAt, request.AdminId, spec.AdminNote),
                    PromotionType.FreeMonths =>
                        PromotionCode.CreateFreeMonths(spec.FreeMonths!.Value, spec.TargetTier!.Value,
                            spec.MaxRedemptions, spec.ExpiresAt, request.AdminId, spec.AdminNote),
                    _ => throw new ArgumentOutOfRangeException(),
                };
                codes.Add(code);
            }
        }

        await repository.AddRangeAsync(codes, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success<IReadOnlyList<PromotionCodeDto>>(
            codes.Select(PromotionCodeDto.FromDomain).ToList());
    }
}

// ── Admin: toggle active ──────────────────────────────────────────────────────

public sealed record TogglePromotionCodeCommand(Guid CodeId, bool Activate, Guid AdminId)
    : IRequest<Result<PromotionCodeDto>>;

public sealed class TogglePromotionCodeCommandHandler(
    IPromotionCodeRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<TogglePromotionCodeCommand, Result<PromotionCodeDto>>
{
    public async Task<Result<PromotionCodeDto>> Handle(
        TogglePromotionCodeCommand request, CancellationToken ct)
    {
        // Fetch by Id via GetAllAsync is not ideal but avoids a new interface method for now
        var all = await repository.GetAllAsync(ct);
        var code = all.FirstOrDefault(c => c.Id == request.CodeId);
        if (code is null) return Result.Failure<PromotionCodeDto>("Código no encontrado.");

        if (request.Activate) code.Reactivate(); else code.Deactivate();
        repository.Update(code);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(PromotionCodeDto.FromDomain(code));
    }
}

// ── Admin: list codes ─────────────────────────────────────────────────────────

public sealed record GetAllPromotionCodesQuery : IRequest<Result<IReadOnlyList<PromotionCodeDto>>>;

public sealed class GetAllPromotionCodesQueryHandler(IPromotionCodeRepository repository)
    : IRequestHandler<GetAllPromotionCodesQuery, Result<IReadOnlyList<PromotionCodeDto>>>
{
    public async Task<Result<IReadOnlyList<PromotionCodeDto>>> Handle(
        GetAllPromotionCodesQuery request, CancellationToken ct)
    {
        var codes = await repository.GetAllAsync(ct);
        return Result.Success<IReadOnlyList<PromotionCodeDto>>(
            codes.Select(PromotionCodeDto.FromDomain).ToList());
    }
}

// ── User: validate without redeeming ─────────────────────────────────────────

public sealed record ValidatePromotionCodeQuery(string Code)
    : IRequest<Result<PromotionValidationDto>>;

public sealed class ValidatePromotionCodeQueryHandler(IPromotionCodeRepository repository)
    : IRequestHandler<ValidatePromotionCodeQuery, Result<PromotionValidationDto>>
{
    public async Task<Result<PromotionValidationDto>> Handle(
        ValidatePromotionCodeQuery request, CancellationToken ct)
    {
        var code = await repository.GetByCodeAsync(request.Code.Trim().ToUpperInvariant(), ct);

        // Always return the same message for invalid/expired/exhausted — anti-enumeration
        if (code is null || !code.CanRedeem)
            return Result.Failure<PromotionValidationDto>("Código no válido o expirado.");

        return Result.Success(new PromotionValidationDto(
            code.Code,
            code.Type.ToString(),
            BuildDescription(code),
            code.DiscountPercent,
            code.FreeMonths,
            code.TargetTier?.ToString(),
            code.IsFullyFree,
            !code.IsFullyFree));
    }

    private static string BuildDescription(PromotionCode c) => c.Type switch
    {
        PromotionType.PercentageDiscount when c.DiscountPercent == 100 =>
            $"1 mes gratis de {TierLabel(c.TargetTier)}",
        PromotionType.PercentageDiscount =>
            $"{c.DiscountPercent}% de descuento{(c.TargetTier is null ? "" : $" en {TierLabel(c.TargetTier)}")}",
        PromotionType.FreeTier =>
            $"1 mes gratis de {TierLabel(c.TargetTier)}",
        PromotionType.FreeMonths =>
            $"{c.FreeMonths} mes{(c.FreeMonths > 1 ? "es" : "")} gratis de {TierLabel(c.TargetTier)}",
        _ => "Beneficio especial",
    };

    private static string TierLabel(SubscriptionTier? tier) => tier switch
    {
        SubscriptionTier.UserPlus => "Plan Plus",
        SubscriptionTier.UserFamilia => "Plan Familia",
        _ => "suscripción",
    };
}

// ── User: redeem code ─────────────────────────────────────────────────────────

public sealed record RedeemPromotionCodeCommand(
    string Code,
    Guid UserId,
    /// <summary>Required only when the code is a PercentageDiscount with no fixed TargetTier.</summary>
    SubscriptionTier? SelectedTier = null)
    : IRequest<Result<SubscriptionDto>>;

public sealed class RedeemPromotionCodeCommandValidator : AbstractValidator<RedeemPromotionCodeCommand>
{
    public RedeemPromotionCodeCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(8);
    }
}

public sealed class RedeemPromotionCodeCommandHandler(
    IPromotionCodeRepository promoRepository,
    ISubscriptionRepository subscriptionRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RedeemPromotionCodeCommand, Result<SubscriptionDto>>
{
    private static readonly string GenericInvalidMsg = "Código no válido o expirado.";

    public async Task<Result<SubscriptionDto>> Handle(
        RedeemPromotionCodeCommand request, CancellationToken ct)
    {
        var code = await promoRepository.GetByCodeAsync(
            request.Code.Trim().ToUpperInvariant(), ct);

        // ── Security validations (order matters) ───────────────────────────────

        // 1. Anti-enumeration: same message for all "bad code" cases
        if (code is null || !code.CanRedeem)
            return Result.Failure<SubscriptionDto>(GenericInvalidMsg);

        // 2. Self-redemption guard
        if (code.CreatedByAdminId == request.UserId)
            return Result.Failure<SubscriptionDto>("No podés redimir un código que vos generaste.");

        // 3. Per-user per-code once
        if (await promoRepository.HasUserRedeemedAsync(request.UserId, code.Id, ct))
            return Result.Failure<SubscriptionDto>("Este código ya fue utilizado por tu cuenta.");

        // 4. No stacking of free promotions
        if (code.IsFullyFree &&
            await promoRepository.HasUserActivePromoSubscriptionAsync(request.UserId, ct))
            return Result.Failure<SubscriptionDto>(
                "Ya tenés una suscripción activa por código de promoción. No es posible acumular beneficios gratuitos.");

        // 5. Resolve target tier
        var tier = code.TargetTier ?? request.SelectedTier;
        if (tier is null)
            return Result.Failure<SubscriptionDto>("Especificá el plan al que querés aplicar el descuento.");

        // 6. If fully paid sub active and trying to apply free code → block
        var existing = await subscriptionRepository.GetActiveForUserAsync(request.UserId, ct);
        if (existing is { IsActive: true } && code.IsFullyFree)
            return Result.Failure<SubscriptionDto>(
                "No podés aplicar un código gratuito mientras tenés una suscripción paga activa.");

        // ── Apply promotion ────────────────────────────────────────────────────

        Subscription newSub;

        if (code.IsFullyFree)
        {
            // Cancel any pending sub before creating the free one
            if (existing is { Status: SubscriptionStatus.PendingPayment })
            {
                existing.Cancel();
                subscriptionRepository.Update(existing);
            }

            var months = code.FreeMonths ?? 1;
            newSub = Subscription.CreateFromPromotion(request.UserId, tier.Value, months, code.Id);
        }
        else
        {
            // Partial discount — create sub with reduced amount, still needs SINPE
            var prices = new Dictionary<SubscriptionTier, decimal>
            {
                [SubscriptionTier.UserPlus] = 2_990m,
                [SubscriptionTier.UserFamilia] = 4_990m,
                [SubscriptionTier.ClinicPlus] = 15_000m,
                [SubscriptionTier.ClinicPartner] = 35_000m,
            };
            if (!prices.TryGetValue(tier.Value, out var basePrice))
                return Result.Failure<SubscriptionDto>("Tier no válido para este código.");

            var discountFactor = 1m - (code.DiscountPercent!.Value / 100m);
            var discountedAmount = Math.Round(basePrice * discountFactor, 0);
            var reference = GeneratePaymentReference();
            newSub = Subscription.CreateForUser(request.UserId, tier.Value, reference, discountedAmount);
        }

        // ── Persist atomically ─────────────────────────────────────────────────

        // Optimistic concurrency on RedeemedCount prevents over-redemption under load
        code.IncrementRedeemed();
        promoRepository.Update(code);

        await subscriptionRepository.AddAsync(newSub, ct);

        var redemption = PromotionCodeRedemption.Create(code.Id, request.UserId, newSub.Id);
        await promoRepository.AddRedemptionAsync(redemption, ct);

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(SubscriptionDto.FromDomain(newSub));
    }

    private static string GeneratePaymentReference()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        var bytes = new byte[8];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }
}
