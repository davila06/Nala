using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using PawTrack.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using PawTrack.Domain.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace PawTrack.IntegrationTests.Imports;

[Collection("Integration")]
public sealed class ImportJobsEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    [Fact]
    public async Task CreateStoreImport_WithSameIdempotencyKey_ReturnsSameJob()
    {
        var email = $"import_{Guid.NewGuid():N}@pawtrack.cr";
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory, email);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var user = await db.Users.SingleAsync(x => x.Email == email);
            var subscription = Subscription.CreateForUser(
                user.Id, SubscriptionTier.StorePartner, "IMPORT-TEST", 25000m);
            subscription.Activate();
            await db.Subscriptions.AddAsync(subscription);
            await db.SaveChangesAsync();
        }
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("StoreProducts"), "resourceType");
        var csv = new ByteArrayContent(Encoding.UTF8.GetBytes("name,description,category,priceCrc\nCollar,Azul,Accessories,12000"));
        csv.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(csv, "file", "products.csv");
        content.Headers.Add("Idempotency-Key", "integration-import-1");

        var first = await client.PostAsync("/api/imports", content);
        first.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var firstBody = await first.Content.ReadFromJsonAsync<ImportResponse>();

        using var retryContent = new MultipartFormDataContent();
        retryContent.Add(new StringContent("StoreProducts"), "resourceType");
        var retryCsv = new ByteArrayContent(Encoding.UTF8.GetBytes("name,description,category,priceCrc\nCollar,Azul,Accessories,12000"));
        retryCsv.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        retryContent.Add(retryCsv, "file", "products.csv");
        retryContent.Headers.Add("Idempotency-Key", "integration-import-1");

        var retry = await client.PostAsync("/api/imports", retryContent);
        retry.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var retryBody = await retry.Content.ReadFromJsonAsync<ImportResponse>();
        retryBody!.Id.Should().Be(firstBody!.Id);
    }

    private sealed record ImportResponse(Guid Id, string Status, int RowCount, int ImportedCount, int DuplicateCount);
}
