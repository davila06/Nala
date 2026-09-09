using FluentValidation;
using MediatR;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Advertising;
using PawTrack.Domain.Audit;
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
    DateTimeOffset CreatedAt,
    string AdvertiserName,
    string Category,
    string? TargetCanton,
    string? ContractReference,
    decimal BudgetCrc,
    int FrequencyCapPerDay,
    bool IsCategoryExclusive,
    bool IsVip,
    string CampaignStatus,
    string? ReviewNote)
{
    public static BillboardDto FromDomain(Billboard b) => new(
        b.Id, b.Title, b.Body, b.ImageUrl, b.CtaLabel, b.CtaUrl,
        b.Placement.ToString(), b.Status.ToString(),
        b.StartsAt, b.EndsAt, b.Priority, b.CreatedAt, b.AdvertiserName,
        b.Category.ToString(), b.TargetCanton, b.ContractReference, b.BudgetCrc,
        b.FrequencyCapPerDay, b.IsCategoryExclusive, b.IsVip, b.CampaignStatus.ToString(), b.ReviewNote);
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
    int Priority = 0,
    string AdvertiserName = "Pendiente de anunciante",
    string Category = "PetCare",
    string? TargetCanton = null,
    string? ContractReference = null,
    decimal BudgetCrc = 0,
    int FrequencyCapPerDay = 1,
    bool IsCategoryExclusive = false,
    bool IsVip = false) : IRequest<Result<BillboardDto>>;

public sealed class CreateBillboardCommandValidator : AbstractValidator<CreateBillboardCommand>
{
    public CreateBillboardCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Body).MaximumLength(300);
        RuleFor(x => x.CtaLabel).MaximumLength(60);
        RuleFor(x => x.CtaUrl).Must(u => u is null ||
                (Uri.TryCreate(u, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("CtaUrl must be a valid HTTPS URL.");
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt)
            .WithMessage("EndsAt must be after StartsAt.");
        RuleFor(x => x.Placement)
            .Must(p => Enum.TryParse<BillboardPlacement>(p, ignoreCase: true, out _))
            .WithMessage("Invalid placement value.");
        RuleFor(x => x.Priority).InclusiveBetween(0, 100);
        RuleFor(x => x.AdvertiserName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Category).Must(c => Enum.TryParse<BillboardCategory>(c, true, out _));
        RuleFor(x => x.TargetCanton).MaximumLength(100);
        RuleFor(x => x.ContractReference).MaximumLength(100);
        RuleFor(x => x.BudgetCrc).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FrequencyCapPerDay).InclusiveBetween(1, 10);
    }
}

public sealed class CreateBillboardCommandHandler(IBillboardRepository repo, IUnitOfWork uow)
    : IRequestHandler<CreateBillboardCommand, Result<BillboardDto>>
{
    public async Task<Result<BillboardDto>> Handle(CreateBillboardCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<BillboardPlacement>(request.Placement, ignoreCase: true, out var placement))
            return Result.Failure<BillboardDto>("Placement inválido.");
        if (!Enum.TryParse<BillboardCategory>(request.Category, ignoreCase: true, out var category))
            return Result.Failure<BillboardDto>("Categoría inválida.");

        var billboard = Billboard.Create(
            request.RequestingUserId, request.Title, request.Body, placement,
            request.StartsAt, request.EndsAt, request.CtaLabel, request.CtaUrl, request.Priority,
            request.AdvertiserName, category, request.TargetCanton, request.ContractReference,
            request.BudgetCrc, request.FrequencyCapPerDay, request.IsCategoryExclusive, request.IsVip);

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
    int Priority,
    string AdvertiserName,
    string Category,
    string? TargetCanton,
    string? ContractReference,
    decimal BudgetCrc,
    int FrequencyCapPerDay,
    bool IsCategoryExclusive,
    bool IsVip) : IRequest<Result<BillboardDto>>;

public sealed class UpdateBillboardCommandHandler(IBillboardRepository repo, IUnitOfWork uow)
    : IRequestHandler<UpdateBillboardCommand, Result<BillboardDto>>
{
    public async Task<Result<BillboardDto>> Handle(UpdateBillboardCommand request, CancellationToken ct)
    {
        var b = await repo.GetByIdAsync(request.BillboardId, ct);
        if (b is null) return Result.Failure<BillboardDto>("Billboard no encontrado.");
        if (!Enum.TryParse<BillboardCategory>(request.Category, true, out var category))
            return Result.Failure<BillboardDto>("Categoría inválida.");
        if (request.BudgetCrc < 0 || request.FrequencyCapPerDay is < 1 or > 10)
            return Result.Failure<BillboardDto>("Los términos comerciales son inválidos.");

        b.UpdateCampaign(request.Title, request.Body, request.CtaLabel, request.CtaUrl,
            request.StartsAt, request.EndsAt, request.Priority, request.AdvertiserName,
            category, request.TargetCanton, request.ContractReference, request.BudgetCrc,
            request.FrequencyCapPerDay, request.IsCategoryExclusive, request.IsVip);
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

public sealed record DeleteBillboardCommand(Guid BillboardId, Guid ActorUserId) : IRequest<Result<bool>>;

public sealed class DeleteBillboardCommandHandler(
    IBillboardRepository repo, IBlobStorageService blobStorage, IAuditLogRepository auditLog, IUnitOfWork uow)
    : IRequestHandler<DeleteBillboardCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteBillboardCommand request, CancellationToken ct)
    {
        var billboard = await repo.GetByIdAsync(request.BillboardId, ct);
        if (billboard is null) return Result.Failure<bool>("Billboard no encontrado.");
        if (!string.IsNullOrWhiteSpace(billboard.ImageUrl)) await blobStorage.DeleteAsync(billboard.ImageUrl, ct);
        await repo.DeleteAsync(billboard, ct);
        await auditLog.AddAsync(AuditLogEntry.Create(request.ActorUserId, AuditAction.BillboardDeleted, "Billboard", billboard.Id.ToString()), ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success(true);
    }
}

public sealed record ReviewBillboardCampaignCommand(Guid BillboardId, Guid ReviewerUserId, bool Approve, string? Note)
    : IRequest<Result<BillboardDto>>;

public sealed class ReviewBillboardCampaignCommandHandler(
    IBillboardRepository repo, IAuditLogRepository auditLog, IUnitOfWork uow)
    : IRequestHandler<ReviewBillboardCampaignCommand, Result<BillboardDto>>
{
    public async Task<Result<BillboardDto>> Handle(ReviewBillboardCampaignCommand request, CancellationToken ct)
    {
        var billboard = await repo.GetByIdAsync(request.BillboardId, ct);
        if (billboard is null) return Result.Failure<BillboardDto>("Billboard no encontrado.");

        if (request.Approve)
        {
            if (!billboard.Approve(request.ReviewerUserId, request.Note))
                return Result.Failure<BillboardDto>(billboard.ReviewNote ?? "No se pudo aprobar la campaña.");
            await auditLog.AddAsync(AuditLogEntry.Create(request.ReviewerUserId, AuditAction.BillboardApproved, "Billboard", billboard.Id.ToString(), request.Note), ct);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Note)) return Result.Failure<BillboardDto>("El rechazo requiere una razón.");
            billboard.Reject(request.ReviewerUserId, request.Note);
            await auditLog.AddAsync(AuditLogEntry.Create(request.ReviewerUserId, AuditAction.BillboardRejected, "Billboard", billboard.Id.ToString(), request.Note), ct);
        }

        repo.Update(billboard);
        await uow.SaveChangesAsync(ct);
        return Result.Success(BillboardDto.FromDomain(billboard));
    }
}

public sealed record SubmitBillboardCampaignCommand(Guid BillboardId, Guid ActorUserId)
    : IRequest<Result<BillboardDto>>;

public sealed class SubmitBillboardCampaignCommandHandler(
    IBillboardRepository repo, IAuditLogRepository auditLog, IUnitOfWork uow)
    : IRequestHandler<SubmitBillboardCampaignCommand, Result<BillboardDto>>
{
    public async Task<Result<BillboardDto>> Handle(SubmitBillboardCampaignCommand request, CancellationToken ct)
    {
        var billboard = await repo.GetByIdAsync(request.BillboardId, ct);
        if (billboard is null) return Result.Failure<BillboardDto>("Billboard no encontrado.");
        if (!billboard.SubmitForReview()) return Result.Failure<BillboardDto>("La campaña requiere anunciante e imagen antes de revisión.");
        repo.Update(billboard);
        await auditLog.AddAsync(AuditLogEntry.Create(request.ActorUserId, AuditAction.BillboardSubmittedForReview, "Billboard", billboard.Id.ToString()), ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success(BillboardDto.FromDomain(billboard));
    }
}

public sealed record TrackBillboardDeliveryCommand(Guid BillboardId, string EventType, string EventKeyHash, string VisitorHash, string? Canton)
    : IRequest<Result<bool>>;

public sealed class TrackBillboardDeliveryCommandHandler(IBillboardRepository repo, IUnitOfWork uow)
    : IRequestHandler<TrackBillboardDeliveryCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(TrackBillboardDeliveryCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<BillboardDeliveryEventType>(request.EventType, true, out var eventType))
            return Result.Failure<bool>("Tipo de evento inválido.");
        var billboard = await repo.GetByIdAsync(request.BillboardId, ct);
        if (billboard is null || !billboard.IsCurrentlyActive) return Result.Failure<bool>("Campaña no activa.");
        if (await repo.HasDeliveryEventAsync(billboard.Id, eventType, request.EventKeyHash, ct)) return Result.Success(false);
        if (eventType == BillboardDeliveryEventType.Impression &&
            await repo.CountImpressionsByVisitorTodayAsync(billboard.Id, request.VisitorHash, ct) >= billboard.FrequencyCapPerDay)
            return Result.Failure<bool>("Límite diario de impresiones alcanzado.");

        await repo.AddDeliveryEventAsync(BillboardDeliveryEvent.Record(billboard.Id, eventType, request.EventKeyHash, request.VisitorHash, request.Canton), ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success(true);
    }
}

public sealed record GetBillboardCampaignMetricsQuery(Guid BillboardId, DateOnly From, DateOnly To)
    : IRequest<BillboardCampaignMetrics>;

public sealed class GetBillboardCampaignMetricsQueryHandler(IBillboardRepository repo)
    : IRequestHandler<GetBillboardCampaignMetricsQuery, BillboardCampaignMetrics>
{
    public Task<BillboardCampaignMetrics> Handle(GetBillboardCampaignMetricsQuery request, CancellationToken ct) =>
        repo.GetMetricsAsync(request.BillboardId, request.From, request.To, ct);
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
