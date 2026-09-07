using PawTrack.Domain.AnimalWelfare;

namespace PawTrack.Application.AnimalWelfare.Dtos;

public sealed record AnimalWelfareCaseSummaryDto(
    Guid Id,
    string PublicCode,
    WelfareCaseType Type,
    WelfareCaseStatus Status,
    WelfareSeverity Severity,
    string Canton,
    Guid? AssignedOrganizationUserId,
    string? AssignedRole,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AnimalWelfareCaseDetailDto(
    Guid Id,
    string PublicCode,
    WelfareCaseType Type,
    WelfareCaseStatus Status,
    WelfareSeverity Severity,
    string Canton,
    double? ApproxLat,
    double? ApproxLng,
    string DescriptionSanitized,
    Guid? PetId,
    Guid? CapturedAnimalId,
    Guid? AssignedOrganizationUserId,
    string? AssignedRole,
    string? ClosureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ClosedAt,
    IReadOnlyList<AnimalWelfareEvidenceDto> Evidence,
    IReadOnlyList<AnimalWelfareNoteDto> Notes,
    IReadOnlyList<AnimalWelfareAuditDto> Audit);

public sealed record AnimalWelfareEvidenceDto(
    Guid Id,
    WelfareEvidenceKind EvidenceKind,
    string ContentType,
    long FileSizeBytes,
    bool IsSensitive,
    DateTimeOffset UploadedAt);

public sealed record AnimalWelfareNoteDto(Guid Id, Guid AuthorUserId, string Body, DateTimeOffset CreatedAt);

public sealed record AnimalWelfareAuditDto(Guid Id, WelfareAuditAction Action, Guid? ActorUserId, string? Details, DateTimeOffset CreatedAt);

public sealed record PublicAnimalWelfareCaseStatusDto(
    string PublicCode,
    WelfareCaseStatus Status,
    WelfareSeverity Severity,
    string Canton,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ClosedAt);

public sealed record PagedAnimalWelfareCasesDto(
    IReadOnlyList<AnimalWelfareCaseSummaryDto> Items,
    int Page,
    int PageSize);
