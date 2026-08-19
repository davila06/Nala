using FluentValidation;
using MediatR;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Advertising;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Advertising;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record BillboardDto(
    Guid Id,
    string Title,
    string? Body,
    string? ImageUrl,
    string? CtaLabel,
    string? CtaUrl,
    string Placement,
    string Status,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int Priority,
    DateTimeOffset CreatedAt)
{
    public static BillboardDto FromDomain(Billboard b) => new(
        b.Id, b.Title, b.Body, b.ImageUrl, b.CtaLabel, b.CtaUrl,
        b.Placement.ToString(), b.Status.ToString(),
        b.StartsAt, b.EndsAt, b.Priority, b.CreatedAt);
}

// ── Get active billboards (public, by placement) ──────────────────────────────

public sealed record GetActiveBillboardsQuery(string Placement)
    : IRequest<IReadOnlyList<BillboardDto>>;

public sealed class GetActiveBillboardsQueryHandler(IBillboardRepository repo)
    : IRequestHandler<GetActiveBillboardsQuery, IReadOnlyList<BillboardDto>>
{
    public async Task<IReadOnlyList<BillboardDto>> Handle(
        GetActiveBillboardsQuery request, CancellationToken ct)
    {
        if (!Enum.TryParse<BillboardPlacement>(request.Placement, ignoreCase: true, out var placement))
            return [];

        var items = await repo.GetActiveByPlacementAsync(placement, ct);
        return items.Select(BillboardDto.FromDomain).ToList();
    }
}

// ── Get all billboards (Admin) ────────────────────────────────────────────────

public sealed record GetAllBillboardsQuery(int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<BillboardDto>>;

public sealed class GetAllBillboardsQueryHandler(IBillboardRepository repo)
    : IRequestHandler<GetAllBillboardsQuery, PagedResult<BillboardDto>>
{
    public async Task<PagedResult<BillboardDto>> Handle(
        GetAllBillboardsQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 50);
        var total = await repo.CountAllAsync(ct);
        var items = await repo.GetAllAsync((page - 1) * size, size, ct);
        return new PagedResult<BillboardDto>(
            items.Select(BillboardDto.FromDomain).ToList(), total, page, size);
    }
}

// ── Create billboard (Admin) ──────────────────────────────────────────────────

public sealed record CreateBillboardCommand(
    Guid RequestingUserId,
    string Title,
    string? Body,
    string Placement,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? CtaLabel,
    string? CtaUrl,
    int Priority = 0) : IRequest<Result<BillboardDto>>;

public sealed class CreateBillboardCommandValidator : AbstractValidator<CreateBillboardCommand>
{
    public CreateBillboardCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Body).MaximumLength(300);
        RuleFor(x => x.CtaLabel).MaximumLength(60);
        RuleFor(x => x.CtaUrl).Must(u => u is null || Uri.TryCreate(u, UriKind.Absolute, out _))
            .WithMessage("CtaUrl must be a valid absolute URL.");
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt)
            .WithMessage("EndsAt must be after StartsAt.");
        RuleFor(x => x.Placement)
            .Must(p => Enum.TryParse<BillboardPlacement>(p, ignoreCase: true, out _))
            .WithMessage("Invalid placement value.");
        RuleFor(x => x.Priority).InclusiveBetween(0, 100);
    }
}

public sealed class CreateBillboardCommandHandler(IBillboardRepository repo, IUnitOfWork uow)
    : IRequestHandler<CreateBillboardCommand, Result<BillboardDto>>
{
    public async Task<Result<BillboardDto>> Handle(CreateBillboardCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<BillboardPlacement>(request.Placement, ignoreCase: true, out var placement))
            return Result.Failure<BillboardDto>("Placement inválido.");

        var billboard = Billboard.Create(
            request.RequestingUserId, request.Title, request.Body, placement,
            request.StartsAt, request.EndsAt, request.CtaLabel, request.CtaUrl, request.Priority);

        await repo.AddAsync(billboard, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success(BillboardDto.FromDomain(billboard));
    }
}

// ── Update billboard (Admin) ──────────────────────────────────────────────────

public sealed record UpdateBillboardCommand(
    Guid BillboardId,
    string Title,
    string? Body,
    string? CtaLabel,
    string? CtaUrl,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int Priority) : IRequest<Result<BillboardDto>>;

public sealed class UpdateBillboardCommandHandler(IBillboardRepository repo, IUnitOfWork uow)
    : IRequestHandler<UpdateBillboardCommand, Result<BillboardDto>>
{
    public async Task<Result<BillboardDto>> Handle(UpdateBillboardCommand request, CancellationToken ct)
    {
        var b = await repo.GetByIdAsync(request.BillboardId, ct);
        if (b is null) return Result.Failure<BillboardDto>("Billboard no encontrado.");

        b.Update(request.Title, request.Body, request.CtaLabel, request.CtaUrl,
            request.StartsAt, request.EndsAt, request.Priority);
        repo.Update(b);
        await uow.SaveChangesAsync(ct);
        return Result.Success(BillboardDto.FromDomain(b));
    }
}

// ── Set status (Admin: activate / pause / expire) ─────────────────────────────

public sealed record SetBillboardStatusCommand(Guid BillboardId, string Status)
    : IRequest<Result<BillboardDto>>;

public sealed class SetBillboardStatusCommandHandler(IBillboardRepository repo, IUnitOfWork uow)
    : IRequestHandler<SetBillboardStatusCommand, Result<BillboardDto>>
{
    public async Task<Result<BillboardDto>> Handle(SetBillboardStatusCommand request, CancellationToken ct)
    {
        var b = await repo.GetByIdAsync(request.BillboardId, ct);
        if (b is null) return Result.Failure<BillboardDto>("Billboard no encontrado.");

        switch (request.Status.ToLowerInvariant())
        {
            case "active": b.Activate(); break;
            case "paused": b.Pause(); break;
            case "expired": b.Expire(); break;
            default: return Result.Failure<BillboardDto>($"Estado inválido: {request.Status}");
        }

        repo.Update(b);
        await uow.SaveChangesAsync(ct);
        return Result.Success(BillboardDto.FromDomain(b));
    }
}

// ── Upload billboard image (Admin) ────────────────────────────────────────────

public sealed record UploadBillboardImageCommand(
    Guid BillboardId, byte[] ImageBytes, string ContentType)
    : IRequest<Result<BillboardDto>>;

public sealed class UploadBillboardImageCommandHandler(
    IBillboardRepository repo,
    IBlobStorageService blobStorage,
    IImageProcessor imageProcessor,
    IUnitOfWork uow)
    : IRequestHandler<UploadBillboardImageCommand, Result<BillboardDto>>
{
    private const string Container = "billboard-images";

    public async Task<Result<BillboardDto>> Handle(UploadBillboardImageCommand request, CancellationToken ct)
    {
        var b = await repo.GetByIdAsync(request.BillboardId, ct);
        if (b is null) return Result.Failure<BillboardDto>("Billboard no encontrado.");

        if (!string.IsNullOrEmpty(b.ImageUrl))
            await blobStorage.DeleteAsync(b.ImageUrl, ct);

        var resized = await imageProcessor.ResizeAsync(request.ImageBytes, 1200, ct);
        var blobName = $"{b.Id}/{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.jpg";
        using var stream = new MemoryStream(resized);
        var url = await blobStorage.UploadAsync(Container, blobName, stream, "image/jpeg", ct);

        b.SetImageUrl(url);
        repo.Update(b);
        await uow.SaveChangesAsync(ct);
        return Result.Success(BillboardDto.FromDomain(b));
    }
}
