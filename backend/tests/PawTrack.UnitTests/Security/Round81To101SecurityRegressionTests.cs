using FluentValidation.TestHelper;
using PawTrack.Application.Allies.Queries.GetMyAllyAlerts;
using PawTrack.Application.Allies.Queries.GetMyAllyProfile;
using PawTrack.Application.Auth.Queries.GetMyProfile;
using PawTrack.Application.Broadcast.Queries.GetBroadcastStatus;
using PawTrack.Application.Chat.Queries.GetChatThreads;
using PawTrack.Application.Clinics.Queries.GetMyClinic;
using PawTrack.Application.Fosters.Queries.GetFosterSuggestions;
using PawTrack.Application.Fosters.Queries.GetMyFosterProfile;
using PawTrack.Application.Incentives.Queries.GetMyScore;
using PawTrack.Application.LostPets.Queries.GetActiveLostPetByPet;
using PawTrack.Application.LostPets.Queries.GetCaseRoom;
using PawTrack.Application.LostPets.Queries.GetLostPetEventById;
using PawTrack.Application.Sightings.Queries.GetMovementPrediction;
using PawTrack.Application.Notifications.Queries.GetMyNotifications;
using PawTrack.Application.Notifications.Queries.GetNotificationPreferences;
using PawTrack.Application.Pets.Queries.GetMyPets;
using PawTrack.Application.Pets.Queries.GetPetDetail;
using PawTrack.Application.Pets.Queries.GetPetScanHistory;
using PawTrack.Application.Pets.Queries.GetPublicPetProfile;
using PawTrack.Application.Sightings.Queries.GetSightingsByPet;
using PawTrack.Application.Sightings.VisualMatch;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// R81-R101 — FluentValidation validators for queries that carry Guid parameters.
/// Ensures Guid.Empty is rejected before reaching the handler.
/// </summary>
public sealed class Round81To101SecurityRegressionTests
{
    // ── R81: GetMyAllyAlertsQuery ────────────────────────────────────────────
    private readonly GetMyAllyAlertsQueryValidator _r81 = new();

    [Fact]
    public void R81_GetMyAllyAlerts_EmptyUserId_Fails()
        => _r81.TestValidate(new GetMyAllyAlertsQuery(Guid.Empty))
               .ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void R81_GetMyAllyAlerts_ValidUserId_Passes()
        => _r81.TestValidate(new GetMyAllyAlertsQuery(Guid.NewGuid()))
               .ShouldNotHaveAnyValidationErrors();

    // ── R82: GetMyAllyProfileQuery ───────────────────────────────────────────
    private readonly GetMyAllyProfileQueryValidator _r82 = new();

    [Fact]
    public void R82_GetMyAllyProfile_EmptyUserId_Fails()
        => _r82.TestValidate(new GetMyAllyProfileQuery(Guid.Empty))
               .ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void R82_GetMyAllyProfile_ValidUserId_Passes()
        => _r82.TestValidate(new GetMyAllyProfileQuery(Guid.NewGuid()))
               .ShouldNotHaveAnyValidationErrors();

    // ── R83: GetMyProfileQuery ───────────────────────────────────────────────
    private readonly GetMyProfileQueryValidator _r83 = new();

    [Fact]
    public void R83_GetMyProfile_EmptyUserId_Fails()
        => _r83.TestValidate(new GetMyProfileQuery(Guid.Empty))
               .ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void R83_GetMyProfile_ValidUserId_Passes()
        => _r83.TestValidate(new GetMyProfileQuery(Guid.NewGuid()))
               .ShouldNotHaveAnyValidationErrors();

    // ── R84: GetBroadcastStatusQuery ─────────────────────────────────────────
    private readonly GetBroadcastStatusQueryValidator _r84 = new();

    [Fact]
    public void R84_GetBroadcastStatus_EmptyLostPetEventId_Fails()
        => _r84.TestValidate(new GetBroadcastStatusQuery(Guid.Empty, Guid.NewGuid()))
               .ShouldHaveValidationErrorFor(x => x.LostPetEventId);

    [Fact]
    public void R84_GetBroadcastStatus_EmptyRequestingUserId_Fails()
        => _r84.TestValidate(new GetBroadcastStatusQuery(Guid.NewGuid(), Guid.Empty))
               .ShouldHaveValidationErrorFor(x => x.RequestingUserId);

    [Fact]
    public void R84_GetBroadcastStatus_BothValid_Passes()
        => _r84.TestValidate(new GetBroadcastStatusQuery(Guid.NewGuid(), Guid.NewGuid()))
               .ShouldNotHaveAnyValidationErrors();

    // ── R85: GetChatThreadsQuery ─────────────────────────────────────────────
    private readonly GetChatThreadsQueryValidator _r85 = new();

    [Fact]
    public void R85_GetChatThreads_EmptyLostPetEventId_Fails()
        => _r85.TestValidate(new GetChatThreadsQuery(Guid.Empty, Guid.NewGuid()))
               .ShouldHaveValidationErrorFor(x => x.LostPetEventId);

    [Fact]
    public void R85_GetChatThreads_EmptyRequestingUserId_Fails()
        => _r85.TestValidate(new GetChatThreadsQuery(Guid.NewGuid(), Guid.Empty))
               .ShouldHaveValidationErrorFor(x => x.RequestingUserId);

    [Fact]
    public void R85_GetChatThreads_BothValid_Passes()
        => _r85.TestValidate(new GetChatThreadsQuery(Guid.NewGuid(), Guid.NewGuid()))
               .ShouldNotHaveAnyValidationErrors();

    // ── R86: GetCaseRoomQuery ────────────────────────────────────────────────
    private readonly GetCaseRoomQueryValidator _r86 = new();

    [Fact]
    public void R86_GetCaseRoom_EmptyLostPetEventId_Fails()
        => _r86.TestValidate(new GetCaseRoomQuery(Guid.Empty, Guid.NewGuid()))
               .ShouldHaveValidationErrorFor(x => x.LostPetEventId);

    [Fact]
    public void R86_GetCaseRoom_EmptyRequestingUserId_Fails()
        => _r86.TestValidate(new GetCaseRoomQuery(Guid.NewGuid(), Guid.Empty))
               .ShouldHaveValidationErrorFor(x => x.RequestingUserId);

    [Fact]
    public void R86_GetCaseRoom_BothValid_Passes()
        => _r86.TestValidate(new GetCaseRoomQuery(Guid.NewGuid(), Guid.NewGuid()))
               .ShouldNotHaveAnyValidationErrors();

    // ── R87: GetLostPetEventByIdQuery ────────────────────────────────────────
    private readonly GetLostPetEventByIdQueryValidator _r87 = new();

    [Fact]
    public void R87_GetLostPetEventById_EmptyLostPetEventId_Fails()
        => _r87.TestValidate(new GetLostPetEventByIdQuery(Guid.Empty, Guid.NewGuid()))
               .ShouldHaveValidationErrorFor(x => x.LostPetEventId);

    [Fact]
    public void R87_GetLostPetEventById_EmptyRequestingUserId_Fails()
        => _r87.TestValidate(new GetLostPetEventByIdQuery(Guid.NewGuid(), Guid.Empty))
               .ShouldHaveValidationErrorFor(x => x.RequestingUserId);

    [Fact]
    public void R87_GetLostPetEventById_BothValid_Passes()
        => _r87.TestValidate(new GetLostPetEventByIdQuery(Guid.NewGuid(), Guid.NewGuid()))
               .ShouldNotHaveAnyValidationErrors();

    // ── R88: GetActiveLostPetByPetQuery ──────────────────────────────────────
    private readonly GetActiveLostPetByPetQueryValidator _r88 = new();

    [Fact]
    public void R88_GetActiveLostPetByPet_EmptyPetId_Fails()
        => _r88.TestValidate(new GetActiveLostPetByPetQuery(Guid.Empty, Guid.NewGuid()))
               .ShouldHaveValidationErrorFor(x => x.PetId);

    [Fact]
    public void R88_GetActiveLostPetByPet_EmptyRequestingUserId_Fails()
        => _r88.TestValidate(new GetActiveLostPetByPetQuery(Guid.NewGuid(), Guid.Empty))
               .ShouldHaveValidationErrorFor(x => x.RequestingUserId);

    [Fact]
    public void R88_GetActiveLostPetByPet_BothValid_Passes()
        => _r88.TestValidate(new GetActiveLostPetByPetQuery(Guid.NewGuid(), Guid.NewGuid()))
               .ShouldNotHaveAnyValidationErrors();

    // ── R89: GetPetDetailQuery ───────────────────────────────────────────────
    private readonly GetPetDetailQueryValidator _r89 = new();

    [Fact]
    public void R89_GetPetDetail_EmptyPetId_Fails()
        => _r89.TestValidate(new GetPetDetailQuery(Guid.Empty, Guid.NewGuid()))
               .ShouldHaveValidationErrorFor(x => x.PetId);

    [Fact]
    public void R89_GetPetDetail_EmptyRequestingUserId_Fails()
        => _r89.TestValidate(new GetPetDetailQuery(Guid.NewGuid(), Guid.Empty))
               .ShouldHaveValidationErrorFor(x => x.RequestingUserId);

    [Fact]
    public void R89_GetPetDetail_BothValid_Passes()
        => _r89.TestValidate(new GetPetDetailQuery(Guid.NewGuid(), Guid.NewGuid()))
               .ShouldNotHaveAnyValidationErrors();

    // ── R90: GetPetScanHistoryQuery ──────────────────────────────────────────
    private readonly GetPetScanHistoryQueryValidator _r90 = new();

    [Fact]
    public void R90_GetPetScanHistory_EmptyPetId_Fails()
        => _r90.TestValidate(new GetPetScanHistoryQuery(Guid.Empty, Guid.NewGuid()))
               .ShouldHaveValidationErrorFor(x => x.PetId);

    [Fact]
    public void R90_GetPetScanHistory_EmptyRequestingUserId_Fails()
        => _r90.TestValidate(new GetPetScanHistoryQuery(Guid.NewGuid(), Guid.Empty))
               .ShouldHaveValidationErrorFor(x => x.RequestingUserId);

    [Fact]
    public void R90_GetPetScanHistory_BothValid_Passes()
        => _r90.TestValidate(new GetPetScanHistoryQuery(Guid.NewGuid(), Guid.NewGuid()))
               .ShouldNotHaveAnyValidationErrors();

    // ── R91: MatchSightingByIdQuery ──────────────────────────────────────────
    private readonly MatchSightingByIdQueryValidator _r91 = new();

    [Fact]
    public void R91_MatchSightingById_EmptySightingId_Fails()
        => _r91.TestValidate(new MatchSightingByIdQuery(Guid.Empty))
               .ShouldHaveValidationErrorFor(x => x.SightingId);

    [Fact]
    public void R91_MatchSightingById_ValidSightingId_Passes()
        => _r91.TestValidate(new MatchSightingByIdQuery(Guid.NewGuid()))
               .ShouldNotHaveAnyValidationErrors();

    // ── R92: GetMyClinicQuery ────────────────────────────────────────────────
    private readonly GetMyClinicQueryValidator _r92 = new();

    [Fact]
    public void R92_GetMyClinic_EmptyUserId_Fails()
        => _r92.TestValidate(new GetMyClinicQuery(Guid.Empty))
               .ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void R92_GetMyClinic_ValidUserId_Passes()
        => _r92.TestValidate(new GetMyClinicQuery(Guid.NewGuid()))
               .ShouldNotHaveAnyValidationErrors();

    // ── R93: GetFosterSuggestionsQuery ───────────────────────────────────────
    private readonly GetFosterSuggestionsQueryValidator _r93 = new();

    [Fact]
    public void R93_GetFosterSuggestions_EmptyFoundPetReportId_Fails()
        => _r93.TestValidate(new GetFosterSuggestionsQuery(Guid.Empty))
               .ShouldHaveValidationErrorFor(x => x.FoundPetReportId);

    [Fact]
    public void R93_GetFosterSuggestions_ValidId_Passes()
        => _r93.TestValidate(new GetFosterSuggestionsQuery(Guid.NewGuid()))
               .ShouldNotHaveAnyValidationErrors();

    // ── R94: GetMyFosterProfileQuery ─────────────────────────────────────────
    private readonly GetMyFosterProfileQueryValidator _r94 = new();

    [Fact]
    public void R94_GetMyFosterProfile_EmptyUserId_Fails()
        => _r94.TestValidate(new GetMyFosterProfileQuery(Guid.Empty))
               .ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void R94_GetMyFosterProfile_ValidUserId_Passes()
        => _r94.TestValidate(new GetMyFosterProfileQuery(Guid.NewGuid()))
               .ShouldNotHaveAnyValidationErrors();

    // ── R95: GetMyScoreQuery ─────────────────────────────────────────────────
    private readonly GetMyScoreQueryValidator _r95 = new();

    [Fact]
    public void R95_GetMyScore_EmptyUserId_Fails()
        => _r95.TestValidate(new GetMyScoreQuery(Guid.Empty))
               .ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void R95_GetMyScore_ValidUserId_Passes()
        => _r95.TestValidate(new GetMyScoreQuery(Guid.NewGuid()))
               .ShouldNotHaveAnyValidationErrors();

    // ── R96: GetMyNotificationsQuery ─────────────────────────────────────────
    private readonly GetMyNotificationsQueryValidator _r96 = new();

    [Fact]
    public void R96_GetMyNotifications_EmptyUserId_Fails()
        => _r96.TestValidate(new GetMyNotificationsQuery(Guid.Empty))
               .ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void R96_GetMyNotifications_ValidUserId_Passes()
        => _r96.TestValidate(new GetMyNotificationsQuery(Guid.NewGuid()))
               .ShouldNotHaveAnyValidationErrors();

    // ── R97: GetNotificationPreferencesQuery ─────────────────────────────────
    private readonly GetNotificationPreferencesQueryValidator _r97 = new();

    [Fact]
    public void R97_GetNotificationPreferences_EmptyUserId_Fails()
        => _r97.TestValidate(new GetNotificationPreferencesQuery(Guid.Empty))
               .ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void R97_GetNotificationPreferences_ValidUserId_Passes()
        => _r97.TestValidate(new GetNotificationPreferencesQuery(Guid.NewGuid()))
               .ShouldNotHaveAnyValidationErrors();

    // ── R98: GetMyPetsQuery ──────────────────────────────────────────────────
    private readonly GetMyPetsQueryValidator _r98 = new();

    [Fact]
    public void R98_GetMyPets_EmptyOwnerId_Fails()
        => _r98.TestValidate(new GetMyPetsQuery(Guid.Empty))
               .ShouldHaveValidationErrorFor(x => x.OwnerId);

    [Fact]
    public void R98_GetMyPets_ValidOwnerId_Passes()
        => _r98.TestValidate(new GetMyPetsQuery(Guid.NewGuid()))
               .ShouldNotHaveAnyValidationErrors();

    // ── R99: GetPublicPetProfileQuery ────────────────────────────────────────
    private readonly GetPublicPetProfileQueryValidator _r99 = new();

    [Fact]
    public void R99_GetPublicPetProfile_EmptyPetId_Fails()
        => _r99.TestValidate(new GetPublicPetProfileQuery(Guid.Empty))
               .ShouldHaveValidationErrorFor(x => x.PetId);

    [Fact]
    public void R99_GetPublicPetProfile_ValidPetId_Passes()
        => _r99.TestValidate(new GetPublicPetProfileQuery(Guid.NewGuid()))
               .ShouldNotHaveAnyValidationErrors();

    // ── R100: GetSightingsByPetQuery ─────────────────────────────────────────
    private readonly GetSightingsByPetQueryValidator _r100 = new();

    [Fact]
    public void R100_GetSightingsByPet_EmptyPetId_Fails()
        => _r100.TestValidate(new GetSightingsByPetQuery(Guid.Empty, Guid.NewGuid()))
                .ShouldHaveValidationErrorFor(x => x.PetId);

    [Fact]
    public void R100_GetSightingsByPet_EmptyRequestingUserId_Fails()
        => _r100.TestValidate(new GetSightingsByPetQuery(Guid.NewGuid(), Guid.Empty))
                .ShouldHaveValidationErrorFor(x => x.RequestingUserId);

    [Fact]
    public void R100_GetSightingsByPet_BothValid_Passes()
        => _r100.TestValidate(new GetSightingsByPetQuery(Guid.NewGuid(), Guid.NewGuid()))
                .ShouldNotHaveAnyValidationErrors();

    // ── R101: GetMovementPredictionQuery ─────────────────────────────────────
    private readonly GetMovementPredictionQueryValidator _r101 = new();

    [Fact]
    public void R101_GetMovementPrediction_EmptyLostPetEventId_Fails()
        => _r101.TestValidate(new GetMovementPredictionQuery(Guid.Empty))
                .ShouldHaveValidationErrorFor(x => x.LostPetEventId);

    [Fact]
    public void R101_GetMovementPrediction_ValidId_Passes()
        => _r101.TestValidate(new GetMovementPredictionQuery(Guid.NewGuid()))
                .ShouldNotHaveAnyValidationErrors();
}
