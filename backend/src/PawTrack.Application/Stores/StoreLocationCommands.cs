using FluentValidation;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Common;
using PawTrack.Domain.Stores;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Stores;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record StoreLocationDto(
    Guid Id,
    Guid StoreId,
    string Name,
    string Address,
    decimal Lat,
    decimal Lng,
    string? PhoneNumber,
    bool IsPrimary,
    bool IsActive)
{
    public static StoreLocationDto FromDomain(StoreLocation l) => new(
        l.Id, l.StoreId, l.Name, l.Address, l.Lat, l.Lng, l.PhoneNumber, l.IsPrimary, l.IsActive);
}

internal static class StoreLocationGate
{
    public const string RequiresPartnerMessage = "Las sedes múltiples requieren el plan Tienda Partner.";

    public static async Task<Result<Store>> ResolvePartnerStoreAsync(
        Guid storeOwnerUserId,
        IStoreRepository storeRepo,
        ISubscriptionService subscriptionService,
        CancellationToken ct)
    {
        var store = await storeRepo.GetByUserIdAsync(storeOwnerUserId, ct);
        if (store is null) return Result.Failure<Store>("Tienda no encontrada.");

        var tier = await subscriptionService.GetActiveUserTierAsync(storeOwnerUserId, ct);
        if (tier != SubscriptionTier.StorePartner)
            return Result.Failure<Store>(RequiresPartnerMessage);

        return Result.Success(store);
    }
}

// ── Create location ───────────────────────────────────────────────────────────

public sealed record CreateStoreLocationCommand(
    Guid StoreOwnerUserId,
    string Name,
    string Address,
    decimal Lat,
    decimal Lng,
    string? PhoneNumber) : IRequest<Result<StoreLocationDto>>;

public sealed class CreateStoreLocationCommandValidator : AbstractValidator<CreateStoreLocationCommand>
{
    public CreateStoreLocationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Lat).InclusiveBetween(-90m, 90m);
        RuleFor(x => x.Lng).InclusiveBetween(-180m, 180m);
        RuleFor(x => x.PhoneNumber).MaximumLength(30);
    }
}

public sealed class CreateStoreLocationCommandHandler(
    IStoreRepository storeRepo,
    ISubscriptionService subscriptionService,
    IUnitOfWork uow)
    : IRequestHandler<CreateStoreLocationCommand, Result<StoreLocationDto>>
{
    public async Task<Result<StoreLocationDto>> Handle(CreateStoreLocationCommand request, CancellationToken ct)
    {
        var storeResult = await StoreLocationGate.ResolvePartnerStoreAsync(
            request.StoreOwnerUserId, storeRepo, subscriptionService, ct);
        if (storeResult.IsFailure) return Result.Failure<StoreLocationDto>(storeResult.Errors);
        var store = storeResult.Value!;

        var existing = await storeRepo.GetLocationsByStoreAsync(store.Id, ct);
        var isFirst = existing.Count == 0;

        var location = StoreLocation.Create(
            store.Id, request.Name, request.Address, request.Lat, request.Lng,
            request.PhoneNumber, isPrimary: isFirst);

        await storeRepo.AddLocationAsync(location, ct);
        await uow.SaveChangesAsync(ct);

        return Result.Success(StoreLocationDto.FromDomain(location));
    }
}

// ── Update location ───────────────────────────────────────────────────────────

public sealed record UpdateStoreLocationCommand(
    Guid StoreOwnerUserId,
    Guid LocationId,
    string Name,
    string Address,
    decimal Lat,
    decimal Lng,
    string? PhoneNumber) : IRequest<Result<StoreLocationDto>>;

public sealed class UpdateStoreLocationCommandValidator : AbstractValidator<UpdateStoreLocationCommand>
{
    public UpdateStoreLocationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Lat).InclusiveBetween(-90m, 90m);
        RuleFor(x => x.Lng).InclusiveBetween(-180m, 180m);
        RuleFor(x => x.PhoneNumber).MaximumLength(30);
    }
}

public sealed class UpdateStoreLocationCommandHandler(
    IStoreRepository storeRepo,
    ISubscriptionService subscriptionService,
    IUnitOfWork uow)
    : IRequestHandler<UpdateStoreLocationCommand, Result<StoreLocationDto>>
{
    public async Task<Result<StoreLocationDto>> Handle(UpdateStoreLocationCommand request, CancellationToken ct)
    {
        var storeResult = await StoreLocationGate.ResolvePartnerStoreAsync(
            request.StoreOwnerUserId, storeRepo, subscriptionService, ct);
        if (storeResult.IsFailure) return Result.Failure<StoreLocationDto>(storeResult.Errors);
        var store = storeResult.Value!;

        var location = await storeRepo.GetLocationByIdAsync(request.LocationId, ct);
        if (location is null || location.StoreId != store.Id)
            return Result.Failure<StoreLocationDto>("Sede no encontrada.");

        location.UpdateDetails(request.Name, request.Address, request.Lat, request.Lng, request.PhoneNumber);
        storeRepo.UpdateLocation(location);
        await uow.SaveChangesAsync(ct);

        return Result.Success(StoreLocationDto.FromDomain(location));
    }
}

// ── Deactivate / reactivate location ──────────────────────────────────────────

public sealed record SetStoreLocationActiveCommand(
    Guid StoreOwnerUserId, Guid LocationId, bool Active) : IRequest<Result<StoreLocationDto>>;

public sealed class SetStoreLocationActiveCommandHandler(
    IStoreRepository storeRepo,
    ISubscriptionService subscriptionService,
    IUnitOfWork uow)
    : IRequestHandler<SetStoreLocationActiveCommand, Result<StoreLocationDto>>
{
    public async Task<Result<StoreLocationDto>> Handle(SetStoreLocationActiveCommand request, CancellationToken ct)
    {
        var storeResult = await StoreLocationGate.ResolvePartnerStoreAsync(
            request.StoreOwnerUserId, storeRepo, subscriptionService, ct);
        if (storeResult.IsFailure) return Result.Failure<StoreLocationDto>(storeResult.Errors);
        var store = storeResult.Value!;

        var location = await storeRepo.GetLocationByIdAsync(request.LocationId, ct);
        if (location is null || location.StoreId != store.Id)
            return Result.Failure<StoreLocationDto>("Sede no encontrada.");

        try
        {
            if (request.Active) location.Reactivate(); else location.Deactivate();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<StoreLocationDto>(ex.Message);
        }

        storeRepo.UpdateLocation(location);
        await uow.SaveChangesAsync(ct);

        return Result.Success(StoreLocationDto.FromDomain(location));
    }
}

// ── Get my locations ───────────────────────────────────────────────────────────

public sealed record GetMyStoreLocationsQuery(Guid StoreOwnerUserId) : IRequest<Result<IReadOnlyList<StoreLocationDto>>>;

public sealed class GetMyStoreLocationsQueryHandler(
    IStoreRepository storeRepo,
    ISubscriptionService subscriptionService)
    : IRequestHandler<GetMyStoreLocationsQuery, Result<IReadOnlyList<StoreLocationDto>>>
{
    public async Task<Result<IReadOnlyList<StoreLocationDto>>> Handle(GetMyStoreLocationsQuery request, CancellationToken ct)
    {
        var storeResult = await StoreLocationGate.ResolvePartnerStoreAsync(
            request.StoreOwnerUserId, storeRepo, subscriptionService, ct);
        if (storeResult.IsFailure) return Result.Failure<IReadOnlyList<StoreLocationDto>>(storeResult.Errors);
        var store = storeResult.Value!;

        var locations = await storeRepo.GetLocationsByStoreAsync(store.Id, ct);
        return Result.Success<IReadOnlyList<StoreLocationDto>>(
            locations.Select(StoreLocationDto.FromDomain).ToList());
    }
}
