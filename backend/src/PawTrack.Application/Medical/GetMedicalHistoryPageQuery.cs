using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Common;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Medical;

public sealed record MedicalHistoryPageDto(
    IReadOnlyList<MedicalRecordDto> Records, int TotalCount, string AccessTier,
    bool IsLimited, int? PreviewLimit, bool HasMore);

public sealed record GetMedicalHistoryPageQuery(Guid PetId, Guid RequestingUserId, int Page, int PageSize)
    : IRequest<Result<MedicalHistoryPageDto>>;

public sealed class GetMedicalHistoryPageQueryHandler(
    IPetRepository petRepository, IFamilyRepository familyRepository,
    IMedicalRepository medicalRepository, ISubscriptionService subscriptionService,
    IEntitlementService? entitlementService = null)
    : IRequestHandler<GetMedicalHistoryPageQuery, Result<MedicalHistoryPageDto>>
{
    public async Task<Result<MedicalHistoryPageDto>> Handle(GetMedicalHistoryPageQuery request, CancellationToken ct)
    {
        if (request.PetId == Guid.Empty || request.Page < 1 || request.PageSize is < 1 or > 50 ||
            (long)(request.Page - 1) * request.PageSize > int.MaxValue - request.PageSize - 1)
            return Result.Failure<MedicalHistoryPageDto>("Paginación inválida.");

        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null || pet.OwnerId != request.RequestingUserId &&
            !(await familyRepository.GetActiveMemberIdsAsync(pet.OwnerId, ct)).Contains(request.RequestingUserId))
            return Result.Failure<MedicalHistoryPageDto>("Acceso denegado.");

        var tier = await subscriptionService.GetActiveUserTierAsync(request.RequestingUserId, ct);
        var count = await medicalRepository.CountCurrentRecordsAsync(request.PetId, ct);
        if (tier is not (SubscriptionTier.UserFamilia or SubscriptionTier.UserPlus))
            return Result.Success(new MedicalHistoryPageDto([], count, "explorador", true, 0, false));

        var offset = (request.Page - 1) * request.PageSize;
        if (tier == SubscriptionTier.UserPlus)
        {
            var previewLimit = 3;
            if (entitlementService is not null)
            {
                var decision = await entitlementService.AuthorizeAsync(request.RequestingUserId,
                    "MedicalRecordsPreviewLimit", 1m, new EntitlementContext("medical-preview", request.PetId), ct);
                if (decision.Limit.HasValue) previewLimit = (int)decimal.Clamp(decision.Limit.Value, 0, 10_000);
            }
            if (offset >= previewLimit)
                return Result.Success(new MedicalHistoryPageDto([], count, "plus_preview", true, previewLimit, false));
            var preview = await medicalRepository.GetCurrentRecordsPageAsync(request.PetId, offset,
                Math.Min(request.PageSize, previewLimit - offset), ct);
            return Result.Success(new MedicalHistoryPageDto(preview.Select(record => MedicalRecordDto.FromDomain(record) with
            {
                DocumentUrl = null,
                DocumentKind = null,
                WeightKg = null,
                DosageDescription = null,
                Frequency = null,
                DurationDays = null,
                MedicationEndDate = null,
            }).ToList(), count, "plus_preview", true, previewLimit,
                offset + preview.Count < Math.Min(count, previewLimit)));
        }

        var rows = await medicalRepository.GetCurrentRecordsPageAsync(request.PetId, offset, request.PageSize + 1, ct);
        return Result.Success(new MedicalHistoryPageDto(rows.Take(request.PageSize).Select(MedicalRecordDto.FromDomain).ToList(),
            count, "familia", false, null, rows.Count > request.PageSize));
    }
}
