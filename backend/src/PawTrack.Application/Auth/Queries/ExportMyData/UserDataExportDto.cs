using PawTrack.Application.Auth.DTOs;
using PawTrack.Application.LostPets.DTOs;
using PawTrack.Application.Medical;
using PawTrack.Application.Pets.DTOs;

namespace PawTrack.Application.Auth.Queries.ExportMyData;

/// <summary>A single chat message authored by the exporting user (recipient messages are excluded).</summary>
public sealed record ChatMessageExportDto(Guid ThreadId, DateTimeOffset SentAt, string Body);

public sealed record NotificationExportDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    bool IsRead,
    DateTimeOffset CreatedAt);

/// <summary>
/// Full self-service export of a user's personal data, satisfying the data portability
/// expectation under Ley 8968 / international best practice (GDPR Art. 20 as reference).
/// </summary>
public sealed record UserDataExportDto(
    UserProfileDto Profile,
    IReadOnlyList<PetSummaryDto> Pets,
    IReadOnlyList<LostPetEventDto> LostPetReports,
    IReadOnlyList<MedicalRecordDto> MedicalRecords,
    IReadOnlyList<ChatMessageExportDto> ChatMessages,
    IReadOnlyList<NotificationExportDto> Notifications,
    DateTimeOffset ExportedAt);
