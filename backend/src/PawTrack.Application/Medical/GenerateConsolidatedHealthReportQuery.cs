using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Medical;

public sealed record ConsolidatedHealthReportData(string PetName, DateTimeOffset GeneratedAt,
    IReadOnlyList<HealthTimelineItemDto> Items);

public interface IConsolidatedHealthPdfGenerator
{
    Task<byte[]> GenerateAsync(ConsolidatedHealthReportData data, CancellationToken cancellationToken);
}

public sealed record GenerateConsolidatedHealthReportQuery(Guid PetId, Guid RequestingUserId)
    : IRequest<Result<byte[]>>;

public sealed class GenerateConsolidatedHealthReportQueryHandler(
    IPetRepository petRepository,
    IFamilyRepository familyRepository,
    ISubscriptionService subscriptionService,
    IHealthTimelineReadRepository timelineRepository,
    IConsolidatedHealthPdfGenerator pdfGenerator)
    : IRequestHandler<GenerateConsolidatedHealthReportQuery, Result<byte[]>>
{
    public async Task<Result<byte[]>> Handle(GenerateConsolidatedHealthReportQuery request, CancellationToken ct)
    {
        if (!await subscriptionService.IsFamiliaAsync(request.RequestingUserId, ct))
            return Result.Failure<byte[]>("El reporte consolidado requiere el plan Familia.");

        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null || pet.OwnerId != request.RequestingUserId &&
            !(await familyRepository.GetActiveMemberIdsAsync(pet.OwnerId, ct)).Contains(request.RequestingUserId))
            return Result.Failure<byte[]>("Acceso denegado.");

        const int pageSize = 100;
        const int maxItems = 5_000;
        var items = new List<HealthTimelineItemDto>();
        for (var offset = 0; offset <= maxItems; offset += pageSize)
        {
            var page = await timelineRepository.GetPageAsync(pet.Id, offset, pageSize, ct);
            if (items.Count + page.Items.Count > maxItems || offset == maxItems && page.Items.Count > 0)
                return Result.Failure<byte[]>("El reporte supera 5000 eventos; solicite una exportación asistida.");
            items.AddRange(page.Items);
            if (!page.HasMore) break;
            if (offset + pageSize >= maxItems)
                return Result.Failure<byte[]>("El reporte supera 5000 eventos; solicite una exportación asistida.");
        }

        return Result.Success(await pdfGenerator.GenerateAsync(
            new ConsolidatedHealthReportData(pet.Name, DateTimeOffset.UtcNow, items), ct));
    }
}