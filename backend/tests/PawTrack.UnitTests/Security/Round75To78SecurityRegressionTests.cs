using FluentAssertions;
using FluentValidation;
using PawTrack.Application.Chat.Queries.GetChatMessages;
using PawTrack.Application.LostPets.Queries.GetLostPetContact;
using PawTrack.Application.LostPets.Queries.GetSearchZones;
using PawTrack.Application.LostPets.Queries.IsSearchParticipant;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-75 through Round-78 security regression tests.
///
/// Gaps: four queries lack FluentValidation validators.
///
///   R75 — <c>IsSearchParticipantQuery(Guid LostEventId, Guid UserId)</c>
///     This query IS the security gate used by SearchCoordinationHub to decide whether
///     a user may join/claim/clear/release/update zones.  Allowing Guid.Empty to query
///     this gate corrupts the gate's reliability — a zero-GUID lookup can never be a
///     real event, so it should fail immediately at the validation layer rather than
///     reaching the repository and returning false through a DB round-trip.
///
///   R76 — <c>GetLostPetContactQuery(Guid LostEventId)</c>
///     Returns PII (ContactPhone).  Guid.Empty causes an unnecessary DB query; with a
///     validator the pipeline rejects the request before any DB I/O with a clean 422.
///
///   R77 — <c>GetChatMessagesQuery(Guid ThreadId, Guid RequestingUserId)</c>
///     The handler marks unread messages as read (a write operation).  A zero GUID would
///     query a non-existent thread and silently succeed with an empty list, but wastes
///     two DB round-trips (select + ownership check) per call.
///
///   R78 — <c>GetSearchZonesQuery(Guid LostPetEventId)</c>
///     Already protected against BOLA via the R57 controller gate; adding a validator
///     completes the defence-in-depth so the repository never receives a zero GUID.
///
/// Fix:
///   Create one <c>*QueryValidator.cs</c> per query with <c>NotEmpty()</c> on all GUID
///   properties (note the naming convention: <c>*QueryValidator.cs</c>, not
///   <c>*CommandValidator.cs</c>).
/// </summary>
public sealed class Round75To78SecurityRegressionTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // R75 — IsSearchParticipantQuery
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsSearchParticipantQueryValidator_MustExist()
    {
        FindValidator(typeof(IsSearchParticipantQuery)).Should().NotBeNull(
            "IsSearchParticipantQuery is the security gate for all hub methods; " +
            "it must have an IValidator<T> registration for defence-in-depth");
    }

    [Fact]
    public void IsSearchParticipant_RejectsEmptyLostEventId()
    {
        var result = CreateValidator<IsSearchParticipantQuery>()
            .Validate(new IsSearchParticipantQuery(Guid.Empty, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(IsSearchParticipantQuery.LostEventId));
    }

    [Fact]
    public void IsSearchParticipant_RejectsEmptyUserId()
    {
        var result = CreateValidator<IsSearchParticipantQuery>()
            .Validate(new IsSearchParticipantQuery(Guid.NewGuid(), Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(IsSearchParticipantQuery.UserId));
    }

    [Fact]
    public void IsSearchParticipant_AcceptsWellFormedQuery()
    {
        var result = CreateValidator<IsSearchParticipantQuery>()
            .Validate(new IsSearchParticipantQuery(Guid.NewGuid(), Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // R76 — GetLostPetContactQuery
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetLostPetContactQueryValidator_MustExist()
    {
        FindValidator(typeof(GetLostPetContactQuery)).Should().NotBeNull(
            "GetLostPetContactQuery returns PII (ContactPhone) and must have an " +
            "IValidator<T> registration to reject zero-GUID requests before DB I/O");
    }

    [Fact]
    public void GetLostPetContact_RejectsEmptyLostEventId()
    {
        var result = CreateValidator<GetLostPetContactQuery>()
            .Validate(new GetLostPetContactQuery(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(GetLostPetContactQuery.LostEventId));
    }

    [Fact]
    public void GetLostPetContact_AcceptsValidLostEventId()
    {
        var result = CreateValidator<GetLostPetContactQuery>()
            .Validate(new GetLostPetContactQuery(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // R77 — GetChatMessagesQuery
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetChatMessagesQueryValidator_MustExist()
    {
        FindValidator(typeof(GetChatMessagesQuery)).Should().NotBeNull(
            "GetChatMessagesQuery performs a write (marks messages read) and must " +
            "have an IValidator<T> registration");
    }

    [Fact]
    public void GetChatMessages_RejectsEmptyThreadId()
    {
        var result = CreateValidator<GetChatMessagesQuery>()
            .Validate(new GetChatMessagesQuery(Guid.Empty, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(GetChatMessagesQuery.ThreadId));
    }

    [Fact]
    public void GetChatMessages_RejectsEmptyRequestingUserId()
    {
        var result = CreateValidator<GetChatMessagesQuery>()
            .Validate(new GetChatMessagesQuery(Guid.NewGuid(), Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(GetChatMessagesQuery.RequestingUserId));
    }

    [Fact]
    public void GetChatMessages_AcceptsWellFormedQuery()
    {
        var result = CreateValidator<GetChatMessagesQuery>()
            .Validate(new GetChatMessagesQuery(Guid.NewGuid(), Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // R78 — GetSearchZonesQuery
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetSearchZonesQueryValidator_MustExist()
    {
        FindValidator(typeof(GetSearchZonesQuery)).Should().NotBeNull(
            "GetSearchZonesQuery must have an IValidator<T> registration for " +
            "defence-in-depth (the controller already gates on IsSearchParticipant; " +
            "the validator ensures the repository never receives a zero GUID)");
    }

    [Fact]
    public void GetSearchZones_RejectsEmptyLostPetEventId()
    {
        var result = CreateValidator<GetSearchZonesQuery>()
            .Validate(new GetSearchZonesQuery(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(GetSearchZonesQuery.LostPetEventId));
    }

    [Fact]
    public void GetSearchZones_AcceptsValidLostPetEventId()
    {
        var result = CreateValidator<GetSearchZonesQuery>()
            .Validate(new GetSearchZonesQuery(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Type? FindValidator(Type queryType) =>
        queryType.Assembly.GetTypes()
            .FirstOrDefault(t =>
                !t.IsAbstract && t.IsClass &&
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IValidator<>) &&
                    i.GetGenericArguments()[0] == queryType));

    private static IValidator<T> CreateValidator<T>()
    {
        var type = FindValidator(typeof(T))
            ?? throw new InvalidOperationException($"No IValidator<{typeof(T).Name}> found.");
        return (IValidator<T>)Activator.CreateInstance(type)!;
    }
}
