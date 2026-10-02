using FluentAssertions;
using NSubstitute;
using PawTrack.Application.AnimalWelfare.Routing;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.Allies;
using PawTrack.Domain.AnimalWelfare;
using PawTrack.Domain.Municipalities;

namespace PawTrack.UnitTests.AnimalWelfare;

public sealed class WelfareRoutingServiceTests
{
    [Fact]
    public async Task Candidates_include_only_verified_covered_allies_and_sort_by_distance()
    {
        var allies = Substitute.For<IAllyProfileRepository>();
        var municipalities = Substitute.For<IMunicipalProfileRepository>();
        var near = CreateVerifiedAlly("Refugio cercano", 9.93, -84.08, 5000);
        var far = CreateVerifiedAlly("Refugio lejano", 9.95, -84.1, 8000);
        allies.GetVerifiedCoveringPointAsync(9.93, -84.08, Arg.Any<CancellationToken>())
            .Returns(new[] { far, near });
        municipalities.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<MunicipalityProfile>());

        var candidates = await new WelfareRoutingService(allies, municipalities)
            .GetCandidatesAsync("Heredia", 9.93, -84.08, CancellationToken.None);

        candidates.Select(candidate => candidate.OrganizationName)
            .Should().ContainInOrder("Refugio cercano", "Refugio lejano");
        candidates.Should().OnlyContain(candidate => candidate.RecipientType == WelfareReferralRecipientType.Ally);
    }

    [Fact]
    public async Task Suggestion_prefers_nearest_verified_ally_and_never_assigns_case()
    {
        var allies = Substitute.For<IAllyProfileRepository>();
        var municipalities = Substitute.For<IMunicipalProfileRepository>();
        var ally = CreateVerifiedAlly("Refugio", 9.93, -84.08, 3000);
        allies.GetVerifiedCoveringPointAsync(9.93, -84.08, Arg.Any<CancellationToken>())
            .Returns(new[] { ally });
        municipalities.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<MunicipalityProfile>());
        var service = new WelfareRoutingService(allies, municipalities);

        var suggestion = await service.GetSuggestedCandidateAsync("Heredia", 9.93, -84.08, CancellationToken.None);

        suggestion.Should().NotBeNull();
        suggestion!.UserId.Should().Be(ally.UserId);
        suggestion.RecipientType.Should().Be(WelfareReferralRecipientType.Ally);
    }

    private static AllyProfile CreateVerifiedAlly(string name, double lat, double lng, int radius)
    {
        var profile = AllyProfile.Create(Guid.NewGuid(), name, AllyType.Shelter, "Heredia", lat, lng, radius);
        profile.Approve();
        return profile;
    }
}
