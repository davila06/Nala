using FluentAssertions;
using PawTrack.Infrastructure.Storage;

namespace PawTrack.UnitTests.Storage;

public sealed class PublicMediaPolicyTests
{
    [Theory]
    [InlineData("pet-photos")]
    [InlineData("sighting-photos")]
    [InlineData("found-pet-photos")]
    [InlineData("lost-pet-photos")]
    [InlineData("adoption-photos")]
    [InlineData("billboard-images")]
    [InlineData("clinic-logos")]
    [InlineData("store-product-images")]
    public void IsPublicContainer_AllowsOnlyPublicPetMedia(string containerName)
    {
        PublicMediaPolicy.IsPublicContainer(containerName).Should().BeTrue();
    }

    [Theory]
    [InlineData("clinic-medical-exports")]
    [InlineData("verification-documents")]
    [InlineData("regulatory-exports")]
    [InlineData("welfare-evidence")]
    public void IsPublicContainer_RejectsSensitiveMedia(string containerName)
    {
        PublicMediaPolicy.IsPublicContainer(containerName).Should().BeFalse();
    }

    [Fact]
    public void BuildPublicUrl_EncodesEveryPathSegment()
    {
        var result = PublicMediaPolicy.BuildPublicUrl(
            "https://api.pawtrack.cr/",
            "pet-photos",
            "pet one/photo #1.jpg");

        result.Should().Be("https://api.pawtrack.cr/api/public/media/pet-photos/pet%20one/photo%20%231.jpg");
    }

    [Theory]
    [InlineData("../secret.pdf")]
    [InlineData("folder/../../secret.pdf")]
    [InlineData("/absolute.jpg")]
    public void BuildPublicUrl_RejectsUnsafeBlobPaths(string blobName)
    {
        var act = () => PublicMediaPolicy.BuildPublicUrl(
            "https://api.pawtrack.cr",
            "pet-photos",
            blobName);

        act.Should().Throw<ArgumentException>();
    }
}
