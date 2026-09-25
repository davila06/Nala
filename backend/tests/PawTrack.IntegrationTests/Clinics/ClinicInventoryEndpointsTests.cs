using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Clinics;

[Collection("Integration")]
public sealed class ClinicInventoryEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    [Fact]
    public async Task ClinicCanCreateInventoryItemReceiveLotAndListStock()
    {
        var clinicEmail = $"clinic-inventory-{Guid.NewGuid():N}@pawtrack.cr";
        var clinicClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, clinicEmail);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var clinicUser = await db.Users.SingleAsync(user => user.Email == clinicEmail);
            clinicUser.AssignClinicRole();
            var clinic = Clinic.Create(clinicUser.Id, "Clinica Inventario", $"VET-{Guid.NewGuid():N}"[..12], "San Jose", 9.93m, -84.08m, clinicEmail);
            clinic.Activate();
            await db.Clinics.AddAsync(clinic);
            await db.SaveChangesAsync();
            clinicClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(clinicUser.Id, clinicUser.Email, clinicUser.Name, clinicUser.Role));
        }

        var create = await clinicClient.PostAsJsonAsync("/api/clinics/me/inventory/items", new
        {
            name = "Vacuna rabia",
            type = "Vaccine",
            unit = "unidad",
            minimumStock = 2
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var item = await create.Content.ReadFromJsonAsync<InventoryItemResponse>();
        item.Should().NotBeNull();

        var lot = await clinicClient.PostAsJsonAsync($"/api/clinics/me/inventory/items/{item!.Id}/lots", new
        {
            lotNumber = "RAB-001",
            expiresAt = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            quantity = 5,
            unitCostCrc = 1200m,
            supplierName = "Proveedor CR",
            locationName = "Sede Escazu"
        });
        lot.StatusCode.Should().Be(HttpStatusCode.Created);
        var lotPayload = await lot.Content.ReadFromJsonAsync<InventoryLotResponse>();
        lotPayload.Should().NotBeNull();

        var adjust = await clinicClient.PostAsJsonAsync($"/api/clinics/me/inventory/lots/{lotPayload!.Id}/adjustments", new
        {
            quantityDelta = -1,
            reason = "Conteo físico"
        });
        adjust.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await clinicClient.GetAsync("/api/clinics/me/inventory");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await list.Content.ReadFromJsonAsync<List<InventoryItemResponse>>();
        items.Should().ContainSingle(x => x.Id == item.Id && x.TotalAvailable == 4 && x.IsBelowMinimum == false);

        var valuation = await clinicClient.GetAsync("/api/clinics/me/inventory/valuation");
        valuation.StatusCode.Should().Be(HttpStatusCode.OK);
        var report = await valuation.Content.ReadFromJsonAsync<InventoryValuationResponse>();
        report.Should().NotBeNull();
        report!.TotalUnits.Should().Be(4);
        report.TotalValueCrc.Should().Be(4800m);
        report.ByLocation.Should().ContainSingle(x => x.LocationName == "Sede Escazu" && x.AvailableQuantity == 4);
    }

    private sealed record InventoryItemResponse(Guid Id, string Name, int TotalAvailable, bool IsBelowMinimum);
    private sealed record InventoryLotResponse(Guid Id, string LocationName);
    private sealed record InventoryValuationResponse(decimal TotalValueCrc, int TotalUnits, List<InventoryLocationValueResponse> ByLocation);
    private sealed record InventoryLocationValueResponse(string LocationName, int AvailableQuantity, decimal ValueCrc);
}
