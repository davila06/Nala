using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.AuthorizationTests.Infrastructure;

namespace PawTrack.AuthorizationTests.Discovery;

[Collection("Authorization")]
public sealed class EndpointAuthorizationTests
{
    private readonly AuthorizationTestFactory factory;

    public EndpointAuthorizationTests(AuthorizationTestFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task AdminAuditEndpoint_RejectsAnonymousRequest()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/admin/audit");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PublicMapEndpoint_IsExplicitlyAnonymousOrHasNoAuthorizationMetadata()
    {
        using var scope = factory.Services.CreateScope();
        var descriptions = scope.ServiceProvider
            .GetRequiredService<IApiDescriptionGroupCollectionProvider>()
            .ApiDescriptionGroups
            .Items
            .SelectMany(group => group.Items)
            .Where(description =>
                description.HttpMethod == HttpMethod.Get.Method &&
                description.RelativePath?.StartsWith("api/public/map", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        descriptions.Should().NotBeEmpty();
        descriptions.SelectMany(description => description.ActionDescriptor.EndpointMetadata)
            .OfType<IAuthorizeData>()
            .Should().BeEmpty("the public map is an intentional public exception");
    }
}
