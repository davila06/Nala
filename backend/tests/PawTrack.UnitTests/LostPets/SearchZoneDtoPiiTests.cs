using FluentAssertions;
using PawTrack.Application.LostPets.DTOs;
using PawTrack.Domain.LostPets;

namespace PawTrack.UnitTests.LostPets;

/// <summary>
/// Round-13 security: <see cref="SearchZoneDto"/> must NOT expose the
/// <see cref="SearchZone.AssignedToUserId"/> GUID in its public projection.
///
/// <para>
/// <b>Attack:</b> Any authenticated PawTrack user can call
/// <c>GET /api/search-coordination/{id}/zones</c> and receive all zone data,
/// including the <c>AssignedToUserId</c> GUID of every volunteer currently
/// searching. They can also call <c>JoinSearch</c> via the SignalR hub and
/// receive real-time <c>ZoneClaimed</c> / <c>ZoneReleased</c> broadcasts that
/// include the same GUID. Cross-referencing these GUIDs across lost-pet events
/// allows an attacker to build a profile of which users regularly volunteer —
/// making them targets for social-engineering or stalking attacks.
/// </para>
///
/// <para>
/// <b>Fix:</b> Replace <c>Guid? AssignedToUserId</c> with <c>bool IsAssigned</c>
/// in the DTO. The frontend derives "is this zone mine?" from local state (it
/// knows which zones the current user has claimed).
/// </para>
/// </summary>
public sealed class SearchZoneDtoPiiTests
{
    private static SearchZone MakeClaimedZone(Guid claimedBy)
    {
        var zone = SearchZone.Create(Guid.NewGuid(), "Zona A1", """{"type":"Polygon"}""");
        zone.TryClaim(claimedBy);
        return zone;
    }

    // ── SECURITY: no GUID in projection ──────────────────────────────────────

    /// <summary>
    /// SECURITY: Before the fix, this test FAILS because <c>AssignedToUserId</c>
    /// is a public property on <c>SearchZoneDto</c> that exposes the volunteer GUID.
    /// After the fix, the property is removed from the DTO.
    /// </summary>
    [Fact]
    public void SearchZoneDto_HasNoAssignedToUserIdProperty()
    {
        var dtoType = typeof(SearchZoneDto);
        var hasGuidProp = dtoType.GetProperties()
            .Any(p => p.Name == "AssignedToUserId" && p.PropertyType == typeof(Guid?));

        hasGuidProp.Should().BeFalse(
            because: "SearchZoneDto must not expose volunteer GUIDs — " +
                     "use IsAssigned (bool) instead to avoid volunteer identity leakage");
    }

    [Fact]
    public void SearchZoneDto_HasIsAssignedProperty()
    {
        // The replacement property must exist
        var dtoType = typeof(SearchZoneDto);
        var hasBoolProp = dtoType.GetProperties()
            .Any(p => p.Name == "IsAssigned" && p.PropertyType == typeof(bool));

        hasBoolProp.Should().BeTrue(
            because: "SearchZoneDto must have an IsAssigned bool property");
    }

    // ── Correct mapping ───────────────────────────────────────────────────────

    [Fact]
    public void FromDomain_ClaimedZone_IsAssignedTrue()
    {
        var zone = MakeClaimedZone(Guid.NewGuid());
        var dto  = SearchZoneDto.FromDomain(zone);

        dto.IsAssigned.Should().BeTrue();
    }

    [Fact]
    public void FromDomain_FreeZone_IsAssignedFalse()
    {
        var zone = SearchZone.Create(Guid.NewGuid(), "Zona B2", """{"type":"Polygon"}""");
        var dto  = SearchZoneDto.FromDomain(zone);

        dto.IsAssigned.Should().BeFalse();
    }

    [Fact]
    public void FromDomain_ClearedZone_IsAssignedFalse()
    {
        // A zone that's been cleared should show IsAssigned = false
        var claimerId = Guid.NewGuid();
        var zone = MakeClaimedZone(claimerId);
        zone.TryClear(claimerId);
        var dto = SearchZoneDto.FromDomain(zone);

        dto.IsAssigned.Should().BeFalse(
            because: "a cleared zone has no current assignee");
    }

    // ── Other DTO fields are preserved ───────────────────────────────────────

    [Fact]
    public void FromDomain_PreservesAllNonSensitiveFields()
    {
        var lostEventId = Guid.NewGuid();
        var zone = SearchZone.Create(lostEventId, "Zona C3", """{"type":"Polygon","coordinates":[]}""");

        var dto = SearchZoneDto.FromDomain(zone);

        dto.Id.Should().Be(zone.Id);
        dto.LostPetEventId.Should().Be(lostEventId);
        dto.Label.Should().Be("Zona C3");
        dto.GeoJsonPolygon.Should().Be("""{"type":"Polygon","coordinates":[]}""");
        dto.Status.Should().Be(SearchZoneStatus.Free.ToString());
    }
}
