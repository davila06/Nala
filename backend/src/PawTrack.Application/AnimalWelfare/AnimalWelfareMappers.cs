using PawTrack.Application.AnimalWelfare.Dtos;
using PawTrack.Domain.AnimalWelfare;

namespace PawTrack.Application.AnimalWelfare;

internal static class AnimalWelfareMappers
{
    public static AnimalWelfareCaseSummaryDto ToSummary(this AnimalWelfareCase welfareCase) => new(
        welfareCase.Id,
        welfareCase.PublicCode,
        welfareCase.Type,
        welfareCase.Status,
        welfareCase.Severity,
        welfareCase.Canton,
        welfareCase.AssignedOrganizationUserId,
        welfareCase.AssignedRole,
        welfareCase.CreatedAt,
        welfareCase.UpdatedAt);

    public static PublicAnimalWelfareCaseStatusDto ToPublicStatus(this AnimalWelfareCase welfareCase) => new(
        welfareCase.PublicCode,
        welfareCase.Status,
        welfareCase.Severity,
        welfareCase.Canton,
        welfareCase.CreatedAt,
        welfareCase.UpdatedAt,
        welfareCase.ClosedAt);
}
