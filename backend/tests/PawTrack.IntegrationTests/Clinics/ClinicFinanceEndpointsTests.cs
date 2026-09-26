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
public sealed class ClinicFinanceEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    [Fact]
    public async Task CashierCollects_AdministratorRefundsVoidsAndClosesNet()
    {
        var clinicEmail = $"clinic-finance-{Guid.NewGuid():N}@pawtrack.cr";
        var staffEmail = $"staff-finance-{Guid.NewGuid():N}@pawtrack.cr";
        var clinicClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, clinicEmail);
        var staffClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, staffEmail);
        Guid clinicId;
        Guid foreignClinicId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var owner = await db.Users.SingleAsync(user => user.Email == clinicEmail);
            var staff = await db.Users.SingleAsync(user => user.Email == staffEmail);
            owner.AssignClinicRole();
            owner.ConfigureMfa("protected");
            staff.ConfigureMfa("protected");
            var clinic = Clinic.Create(owner.Id, "Clinica Caja", $"VET-{Guid.NewGuid():N}"[..12], "San Jose", 9.93m, -84.08m, clinicEmail);
            clinic.Activate();
            var foreignClinic = Clinic.Create(Guid.NewGuid(), "Otra Clinica Caja", $"VET-{Guid.NewGuid():N}"[..12], "Cartago", 9.86m, -83.92m, $"foreign-finance-{Guid.NewGuid():N}@pawtrack.cr");
            foreignClinic.Activate();
            await db.Clinics.AddAsync(clinic);
            await db.Clinics.AddAsync(foreignClinic);
            await db.SaveChangesAsync();
            clinicId = clinic.Id;
            foreignClinicId = foreignClinic.Id;
            clinicClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(owner.Id, owner.Email, owner.Name, owner.Role));
        }

        var grant = await clinicClient.PutAsJsonAsync("/api/clinics/me/finance/members", new { email = staffEmail, role = "Cashier" });
        grant.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var owner = await db.Users.SingleAsync(user => user.Email == clinicEmail);
            clinicClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(owner.Id, owner.Email, owner.Name, owner.Role, mfaVerified: true));
        }
        grant = await clinicClient.PutAsJsonAsync("/api/clinics/me/finance/members", new { email = staffEmail, role = "Cashier" });
        grant.StatusCode.Should().Be(HttpStatusCode.OK);
        var workspaces = await staffClient.GetFromJsonAsync<List<FinanceWorkspace>>("/api/clinics/finance-workspaces");
        workspaces.Should().ContainSingle(workspace => workspace.ClinicId == clinicId && workspace.Role == "Cashier");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var staff = await db.Users.SingleAsync(user => user.Email == staffEmail);
            staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(staff.Id, staff.Email, staff.Name, staff.Role, mfaVerified: true));
        }

        var saleResponse = await staffClient.PostAsJsonAsync($"/api/clinics/{clinicId}/finance/sales", new
        {
            receiptNumber = $"REC-{Guid.NewGuid():N}"[..16],
            lines = new[] { new { description = "Consulta", type = "Service", quantity = 1, unitPriceCrc = 1000m } }
        });
        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = await saleResponse.Content.ReadFromJsonAsync<SaleResponse>();
        sale.Should().NotBeNull();

        var foreignSale = await staffClient.PostAsJsonAsync($"/api/clinics/{foreignClinicId}/finance/sales", new
        {
            receiptNumber = $"REC-{Guid.NewGuid():N}"[..16],
            lines = new[] { new { description = "Consulta", type = "Service", quantity = 1, unitPriceCrc = 1000m } }
        });
        foreignSale.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var foreignLedger = await staffClient.GetAsync($"/api/clinics/{foreignClinicId}/finance/sales/{sale!.Id}/ledger");
        foreignLedger.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var payment = await staffClient.PostAsJsonAsync($"/api/clinics/{clinicId}/finance/sales/{sale.Id}/payments", new { amountCrc = 1000m, method = "Cash", reference = "CASH-1" });
        payment.StatusCode.Should().Be(HttpStatusCode.OK);
        var deniedVoid = await staffClient.PostAsJsonAsync($"/api/clinics/{clinicId}/finance/sales/{sale.Id}/void", new { reason = "Error" });
        deniedVoid.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var promotion = await clinicClient.PutAsJsonAsync("/api/clinics/me/finance/members", new { email = staffEmail, role = "Administrator" });
        promotion.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var staff = await db.Users.SingleAsync(user => user.Email == staffEmail);
            staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(staff.Id, staff.Email, staff.Name, staff.Role));
        }
        var deniedWithoutSessionMfa = await staffClient.PostAsJsonAsync($"/api/clinics/{clinicId}/finance/sales/{sale.Id}/void", new { reason = "Error" });
        deniedWithoutSessionMfa.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var staff = await db.Users.SingleAsync(user => user.Email == staffEmail);
            staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(staff.Id, staff.Email, staff.Name, staff.Role, mfaVerified: true));
        }
        var ledger = await staffClient.GetFromJsonAsync<SaleLedgerResponse>($"/api/clinics/{clinicId}/finance/sales/{sale.Id}/ledger");
        ledger!.Payments.Should().ContainSingle();
        var refund = await staffClient.PostAsJsonAsync($"/api/clinics/{clinicId}/finance/sales/{sale.Id}/refunds", new
        {
            paymentId = ledger.Payments[0].Id,
            amountCrc = 1000m,
            reason = "Servicio cancelado",
            evidenceReference = $"REF-{Guid.NewGuid():N}"
        });
        refund.StatusCode.Should().Be(HttpStatusCode.OK);
        var voidResponse = await staffClient.PostAsJsonAsync($"/api/clinics/{clinicId}/finance/sales/{sale.Id}/void", new { reason = "Servicio cancelado" });
        voidResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var report = await staffClient.GetFromJsonAsync<FinanceReport>($"/api/clinics/{clinicId}/finance/sales-report?businessDate={date:yyyy-MM-dd}");
        report!.TotalPaidCrc.Should().Be(0m);
        var close = await staffClient.PostAsJsonAsync($"/api/clinics/{clinicId}/finance/cash-closes", new { businessDate = date });
        close.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private sealed record FinanceWorkspace(Guid ClinicId, string Role);
    private sealed record SaleResponse(Guid Id);
    private sealed record SaleLedgerResponse(List<PaymentResponse> Payments);
    private sealed record PaymentResponse(Guid Id);
    private sealed record FinanceReport(decimal TotalPaidCrc);
}
