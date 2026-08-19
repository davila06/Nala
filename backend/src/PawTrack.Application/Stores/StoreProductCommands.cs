using FluentValidation;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Stores;

namespace PawTrack.Application.Stores;

// ── Add product ───────────────────────────────────────────────────────────────

public sealed record AddStoreProductCommand(
    Guid StoreOwnerUserId,
    string Name,
    string? Description,
    ProductCategory Category,
    decimal PriceCrc) : IRequest<Result<StoreProductDto>>;

public sealed class AddStoreProductCommandValidator : AbstractValidator<AddStoreProductCommand>
{
    public AddStoreProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.PriceCrc).GreaterThan(0);
    }
}

public sealed class AddStoreProductCommandHandler(IStoreRepository repo, IUnitOfWork uow)
    : IRequestHandler<AddStoreProductCommand, Result<StoreProductDto>>
{
    public async Task<Result<StoreProductDto>> Handle(AddStoreProductCommand request, CancellationToken ct)
    {
        var store = await repo.GetByUserIdAsync(request.StoreOwnerUserId, ct);
        if (store is null) return Result.Failure<StoreProductDto>("Tienda no encontrada.");
        if (store.Status != StoreStatus.Active) return Result.Failure<StoreProductDto>("La tienda no está activa.");

        var product = StoreProduct.Create(store.Id, request.Name, request.Description, request.Category, request.PriceCrc);
        await repo.AddProductAsync(product, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success(StoreProductDto.FromDomain(product));
    }
}

// ── Update product ────────────────────────────────────────────────────────────

public sealed record UpdateStoreProductCommand(
    Guid StoreOwnerUserId,
    Guid ProductId,
    string Name,
    string? Description,
    ProductCategory Category,
    decimal PriceCrc,
    bool IsAvailable) : IRequest<Result<StoreProductDto>>;

public sealed class UpdateStoreProductCommandHandler(IStoreRepository repo, IUnitOfWork uow)
    : IRequestHandler<UpdateStoreProductCommand, Result<StoreProductDto>>
{
    public async Task<Result<StoreProductDto>> Handle(UpdateStoreProductCommand request, CancellationToken ct)
    {
        var store = await repo.GetByUserIdAsync(request.StoreOwnerUserId, ct);
        if (store is null) return Result.Failure<StoreProductDto>("Tienda no encontrada.");

        var product = await repo.GetProductByIdAsync(request.ProductId, ct);
        if (product is null || product.StoreId != store.Id)
            return Result.Failure<StoreProductDto>("Producto no encontrado.");

        product.Update(request.Name, request.Description, request.Category, request.PriceCrc);
        product.SetAvailable(request.IsAvailable);
        repo.UpdateProduct(product);
        await uow.SaveChangesAsync(ct);
        return Result.Success(StoreProductDto.FromDomain(product));
    }
}

// ── Delete product ────────────────────────────────────────────────────────────

public sealed record DeleteStoreProductCommand(Guid StoreOwnerUserId, Guid ProductId) : IRequest<Result<Unit>>;

public sealed class DeleteStoreProductCommandHandler(IStoreRepository repo, IUnitOfWork uow)
    : IRequestHandler<DeleteStoreProductCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(DeleteStoreProductCommand request, CancellationToken ct)
    {
        var store = await repo.GetByUserIdAsync(request.StoreOwnerUserId, ct);
        if (store is null) return Result.Failure<Unit>("Tienda no encontrada.");

        var product = await repo.GetProductByIdAsync(request.ProductId, ct);
        if (product is null || product.StoreId != store.Id)
            return Result.Failure<Unit>("Producto no encontrado.");

        repo.DeleteProduct(product);
        await uow.SaveChangesAsync(ct);
        return Result.Success(Unit.Value);
    }
}

// ── Get products (owner view — all, including unavailable) ────────────────────

public sealed record GetMyStoreProductsQuery(Guid StoreOwnerUserId) : IRequest<Result<IReadOnlyList<StoreProductDto>>>;

public sealed class GetMyStoreProductsQueryHandler(IStoreRepository repo)
    : IRequestHandler<GetMyStoreProductsQuery, Result<IReadOnlyList<StoreProductDto>>>
{
    public async Task<Result<IReadOnlyList<StoreProductDto>>> Handle(GetMyStoreProductsQuery request, CancellationToken ct)
    {
        var store = await repo.GetByUserIdAsync(request.StoreOwnerUserId, ct);
        if (store is null) return Result.Failure<IReadOnlyList<StoreProductDto>>("Tienda no encontrada.");

        var products = await repo.GetProductsByStoreAsync(store.Id, ct);
        return Result.Success<IReadOnlyList<StoreProductDto>>(products.Select(StoreProductDto.FromDomain).ToList());
    }
}

// ── Upload product image ──────────────────────────────────────────────────────

public sealed record UploadProductImageCommand(
    Guid StoreOwnerUserId,
    Guid ProductId,
    byte[] ImageBytes,
    string ContentType) : IRequest<Result<StoreProductDto>>;

public sealed class UploadProductImageCommandHandler(
    IStoreRepository repo,
    IBlobStorageService blobStorage,
    IImageProcessor imageProcessor,
    IUnitOfWork uow)
    : IRequestHandler<UploadProductImageCommand, Result<StoreProductDto>>
{
    private const string Container = "store-product-images";

    public async Task<Result<StoreProductDto>> Handle(UploadProductImageCommand request, CancellationToken ct)
    {
        var store = await repo.GetByUserIdAsync(request.StoreOwnerUserId, ct);
        if (store is null) return Result.Failure<StoreProductDto>("Tienda no encontrada.");

        var product = await repo.GetProductByIdAsync(request.ProductId, ct);
        if (product is null || product.StoreId != store.Id)
            return Result.Failure<StoreProductDto>("Producto no encontrado.");

        var resized = await imageProcessor.ResizeAsync(request.ImageBytes, 800, ct);
        var blobName = $"{store.Id}/{product.Id}/{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.jpg";

        using var stream = new MemoryStream(resized);
        var url = await blobStorage.UploadAsync(Container, blobName, stream, "image/jpeg", ct);

        product.SetImageUrl(url);
        repo.UpdateProduct(product);
        await uow.SaveChangesAsync(ct);
        return Result.Success(StoreProductDto.FromDomain(product));
    }
}
