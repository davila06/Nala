using FluentValidation;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Common;
using PawTrack.Domain.Stores;

namespace PawTrack.Application.Stores;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record PublicStoreDto(
    Guid Id,
    string Name,
    string Description,
    string Address,
    decimal Lat,
    decimal Lng,
    string? PhoneNumber,
    string? Website,
    string? LogoUrl,
    bool IsFeatured,
    string Status)
{
    public static PublicStoreDto FromDomain(Store s) => new(
        s.Id, s.Name, s.Description, s.Address,
        s.Lat, s.Lng, s.PhoneNumber, s.Website, s.LogoUrl,
        s.IsFeatured, s.Status.ToString());
}

public sealed record StoreProductDto(
    Guid Id,
    Guid StoreId,
    string Name,
    string? Description,
    string Category,
    decimal PriceCrc,
    string? ImageUrl,
    bool IsAvailable)
{
    public static StoreProductDto FromDomain(StoreProduct p) => new(
        p.Id, p.StoreId, p.Name, p.Description,
        p.Category.ToString(), p.PriceCrc, p.ImageUrl, p.IsAvailable);
}

// ── Register store ────────────────────────────────────────────────────────────

public sealed record RegisterStoreCommand(
    string Name,
    string Description,
    string Address,
    decimal Lat,
    decimal Lng,
    string ContactEmail,
    string Password) : IRequest<Result<PublicStoreDto>>;

public sealed class RegisterStoreCommandValidator : AbstractValidator<RegisterStoreCommand>
{
    public RegisterStoreCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ContactEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(8);
    }
}

public sealed class RegisterStoreCommandHandler(
    IUserRepository userRepository,
    IStoreRepository storeRepository,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterStoreCommand, Result<PublicStoreDto>>
{
    internal const string DuplicateEmailError = "duplicate_email";

    public async Task<Result<PublicStoreDto>> Handle(RegisterStoreCommand request, CancellationToken ct)
    {
        if (await userRepository.GetByEmailAsync(request.ContactEmail, ct) is not null)
            return Result.Failure<PublicStoreDto>(DuplicateEmailError);

        var hash = passwordHasher.Hash(request.Password);
        var (user, rawToken) = User.Create(request.ContactEmail, request.Name, hash);
        user.AssignStoreRole();
        await userRepository.AddAsync(user, ct);

        var store = Store.Create(
            user.Id, request.Name, request.Description, request.Address,
            request.Lat, request.Lng, request.ContactEmail);

        await storeRepository.AddAsync(store, ct);
        await unitOfWork.SaveChangesAsync(ct);

        _ = emailSender.SendEmailVerificationAsync(user.Email, user.Name, rawToken, ct);
        return Result.Success(PublicStoreDto.FromDomain(store));
    }
}

// ── Update store profile ──────────────────────────────────────────────────────

public sealed record UpdateStoreProfileCommand(
    Guid StoreOwnerUserId,
    string Name,
    string Description,
    string Address,
    decimal Lat,
    decimal Lng,
    string? PhoneNumber,
    string? Website) : IRequest<Result<PublicStoreDto>>;

public sealed class UpdateStoreProfileCommandValidator : AbstractValidator<UpdateStoreProfileCommand>
{
    public UpdateStoreProfileCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
    }
}

public sealed class UpdateStoreProfileCommandHandler(IStoreRepository repo, IUnitOfWork uow)
    : IRequestHandler<UpdateStoreProfileCommand, Result<PublicStoreDto>>
{
    public async Task<Result<PublicStoreDto>> Handle(UpdateStoreProfileCommand request, CancellationToken ct)
    {
        var store = await repo.GetByUserIdAsync(request.StoreOwnerUserId, ct);
        if (store is null) return Result.Failure<PublicStoreDto>("Tienda no encontrada.");

        store.UpdateProfile(
            request.Name, request.Description, request.Address,
            request.Lat, request.Lng, request.PhoneNumber, request.Website);
        repo.Update(store);
        await uow.SaveChangesAsync(ct);
        return Result.Success(PublicStoreDto.FromDomain(store));
    }
}

// ── Get public stores ─────────────────────────────────────────────────────────

public sealed record GetPublicStoresQuery : IRequest<Result<IReadOnlyList<PublicStoreDto>>>;

public sealed class GetPublicStoresQueryHandler(IStoreRepository repo)
    : IRequestHandler<GetPublicStoresQuery, Result<IReadOnlyList<PublicStoreDto>>>
{
    public async Task<Result<IReadOnlyList<PublicStoreDto>>> Handle(GetPublicStoresQuery request, CancellationToken ct)
    {
        var stores = await repo.GetAllActiveAsync(ct);
        return Result.Success<IReadOnlyList<PublicStoreDto>>(stores.Select(PublicStoreDto.FromDomain).ToList());
    }
}

// ── Get store by id (with products) ──────────────────────────────────────────

public sealed record StoreDetailDto(PublicStoreDto Store, IReadOnlyList<StoreProductDto> Products);

public sealed record GetStoreDetailQuery(Guid StoreId) : IRequest<Result<StoreDetailDto>>;

public sealed class GetStoreDetailQueryHandler(IStoreRepository repo)
    : IRequestHandler<GetStoreDetailQuery, Result<StoreDetailDto>>
{
    public async Task<Result<StoreDetailDto>> Handle(GetStoreDetailQuery request, CancellationToken ct)
    {
        var store = await repo.GetByIdAsync(request.StoreId, ct);
        if (store is null || store.Status != StoreStatus.Active)
            return Result.Failure<StoreDetailDto>("Tienda no encontrada.");

        var products = await repo.GetProductsByStoreAsync(request.StoreId, ct);
        var dto = new StoreDetailDto(
            PublicStoreDto.FromDomain(store),
            products.Where(p => p.IsAvailable).Select(StoreProductDto.FromDomain).ToList());
        return Result.Success(dto);
    }
}

// ── Get my store (owner) ──────────────────────────────────────────────────────

public sealed record GetMyStoreQuery(Guid UserId) : IRequest<Result<PublicStoreDto>>;

public sealed class GetMyStoreQueryHandler(IStoreRepository repo)
    : IRequestHandler<GetMyStoreQuery, Result<PublicStoreDto>>
{
    public async Task<Result<PublicStoreDto>> Handle(GetMyStoreQuery request, CancellationToken ct)
    {
        var store = await repo.GetByUserIdAsync(request.UserId, ct);
        if (store is null) return Result.Failure<PublicStoreDto>("Tienda no encontrada.");
        return Result.Success(PublicStoreDto.FromDomain(store));
    }
}

// ── Admin: review store ───────────────────────────────────────────────────────

public sealed record ReviewStoreCommand(Guid StoreId, bool Approve) : IRequest<Result<Unit>>;

public sealed class ReviewStoreCommandHandler(IStoreRepository repo, IUnitOfWork uow)
    : IRequestHandler<ReviewStoreCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(ReviewStoreCommand request, CancellationToken ct)
    {
        var store = await repo.GetByIdAsync(request.StoreId, ct);
        if (store is null) return Result.Failure<Unit>("Tienda no encontrada.");

        if (request.Approve) store.Activate(); else store.Suspend();
        repo.Update(store);
        await uow.SaveChangesAsync(ct);
        return Result.Success(Unit.Value);
    }
}

// ── Admin: list pending stores ────────────────────────────────────────────────

public sealed record GetPendingStoresQuery : IRequest<Result<IReadOnlyList<PublicStoreDto>>>;

public sealed class GetPendingStoresQueryHandler(IStoreRepository repo)
    : IRequestHandler<GetPendingStoresQuery, Result<IReadOnlyList<PublicStoreDto>>>
{
    public async Task<Result<IReadOnlyList<PublicStoreDto>>> Handle(GetPendingStoresQuery request, CancellationToken ct)
    {
        var stores = await repo.GetPendingAsync(ct);
        return Result.Success<IReadOnlyList<PublicStoreDto>>(stores.Select(PublicStoreDto.FromDomain).ToList());
    }
}
