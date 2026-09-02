using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Infrastructure.Broadcast.Channels;

namespace PawTrack.UnitTests.Broadcast;

/// <summary>
/// Regression coverage: <see cref="EmailChannelBroadcaster"/> must forward
/// <see cref="BroadcastMessageContext.NearbyFeaturedClinics"/> (including LogoUrl)
/// through to <see cref="IEmailSender.SendBroadcastLostPetAsync"/> so the HTML email
/// can render a real clinic logo image.
/// </summary>
public sealed class EmailChannelBroadcasterTests
{
    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:BaseUrl"] = "https://pawtrack.cr",
            })
            .Build();

    private static BroadcastMessageContext MakeContext(IReadOnlyList<NearbyClinicRef>? nearbyClinics) =>
        new(
            LostPetEventId: Guid.NewGuid(),
            PetName: "Firulais",
            PetSpecies: "Dog",
            PetBreed: "Mestizo",
            OwnerEmail: "owner@example.com",
            OwnerContactPhone: null,
            OwnerContactName: "María",
            PetProfileUrl: "https://pawtrack.cr/p/firulais",
            TrackingUrl: "https://pawtrack.cr/t/abc123",
            RecentPhotoUrl: null,
            LastSeenAt: DateTimeOffset.UtcNow,
            LastSeenDescription: "Cerca del parque",
            RestrictToPaidChannels: false,
            NearbyFeaturedClinics: nearbyClinics);

    [Fact]
    public async Task SendAsync_ForwardsNearbyFeaturedClinicsWithLogoUrl_ToEmailSender()
    {
        var emailSender = Substitute.For<IEmailSender>();
        var clinic = new NearbyClinicRef("Clínica San Rafael", "+50622223333", "San José", "https://cdn.pawtrack.cr/logos/sanrafael.png");
        var sut = new EmailChannelBroadcaster(emailSender, BuildConfig(), NullLogger<EmailChannelBroadcaster>.Instance);

        await sut.SendAsync(MakeContext([clinic]));

        await emailSender.Received(1).SendBroadcastLostPetAsync(
            to: "owner@example.com",
            ownerContactName: "María",
            petName: "Firulais",
            petProfileUrl: Arg.Any<string>(),
            trackingUrl: Arg.Any<string>(),
            recentPhotoUrl: Arg.Any<string?>(),
            lastSeenAt: Arg.Any<DateTimeOffset>(),
            nearbyFeaturedClinics: Arg.Is<IReadOnlyList<NearbyClinicRef>?>(
                list => list != null && list.Count == 1 && list[0].LogoUrl == clinic.LogoUrl),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithNoFeaturedClinics_PassesNullThrough()
    {
        var emailSender = Substitute.For<IEmailSender>();
        var sut = new EmailChannelBroadcaster(emailSender, BuildConfig(), NullLogger<EmailChannelBroadcaster>.Instance);

        await sut.SendAsync(MakeContext(null));

        await emailSender.Received(1).SendBroadcastLostPetAsync(
            to: Arg.Any<string>(),
            ownerContactName: Arg.Any<string>(),
            petName: Arg.Any<string>(),
            petProfileUrl: Arg.Any<string>(),
            trackingUrl: Arg.Any<string>(),
            recentPhotoUrl: Arg.Any<string?>(),
            lastSeenAt: Arg.Any<DateTimeOffset>(),
            nearbyFeaturedClinics: null,
            cancellationToken: Arg.Any<CancellationToken>());
    }
}
