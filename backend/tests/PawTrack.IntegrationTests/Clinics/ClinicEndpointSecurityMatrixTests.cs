using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.API.Controllers;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Medical;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Common;
using PawTrack.Domain.Subscriptions;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Clinics;

[Collection("Integration")]
public sealed class ClinicEndpointSecurityMatrixTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    private static readonly HashSet<string> ClinicEndpointActions = new(StringComparer.Ordinal)
    {
        "AcceptOwnerCode", "AddClinicInventoryItem", "AddPatientMedicalRecord", "AdjustClinicInventoryLot",
        "CloseClinicalConsultation", "CloseClinicCash", "CloseFinanceCash", "CloseStaffConsultation",
        "CompleteClinicCrmTask", "CompleteStaffClinicCrmTask", "CreateApiKey", "CreateClinicalConsultation",
        "CreateClinicCrmTask", "CreateClinicSale", "CreateFinanceSale", "CreateStaffClinicCrmTask",
        "CreateStaffConsultation", "CreateVeterinarian", "CreateVeterinarianScheduleBlock",
        "DeleteVeterinarianScheduleBlock", "DownloadClinicalConsultationPrescription",
        "DownloadClinicVerificationDocument", "DownloadMyVerificationDocument", "DownloadPatientMedicalExport",
        "DownloadVeterinarianDocument", "DownloadVeterinarianDocumentForAdmin", "ExportPatientMedical",
        "GenerateAccessCode", "GetAccessibleClinicSites", "GetApiKeys", "GetAuthorizedPets", "GetCertificateIssuers", "GetClinicAgendaAudit",
        "GetClinicalConsultationTemplates", "GetClinicCommunicationTemplates", "GetClinicCrmDashboard",
        "GetClinicInventory", "GetClinicInventoryValuation", "GetClinicSalesReport", "GetClinicStaffMembers",
        "GetClinicVerificationsForAdmin", "GetFinanceMembers", "GetFinanceReport", "GetFinanceSaleLedger",
        "GetMyClinic", "GetMyFinanceWorkspaces", "GetMySaleLedger", "GetMyVerification", "GetMyVeterinarians",
        "GetNearbyAlerts", "GetOwnerClinicCommunicationPreferences", "GetPatientMedicalHistory",
        "GetPatientSanitaryIdentity", "GetPendingClinics", "GetPendingProfileChanges", "GetPublicClinicProfile",
        "GetPublicClinics", "GetScanStats", "GetStaffAgenda", "GetStaffClinicCrmDashboard", "GetStaffTaskAssignees",
        "GetStaffWorkspaces", "GetVeterinarianAgenda", "GetVeterinarianScheduleBlocks", "GetVeterinariansForAdmin",
        "GetVisibilityStats", "GrantClinicStaffMember", "GrantFinanceMember", "LogClinicCommunicationActivity",
        "ReceiveClinicInventoryLot", "RecordFinanceRefund", "RecordMySaleRefund", "Register",
        "RegisterClinicSalePayment", "RegisterFinancePayment", "RescheduleVeterinarianAppointment",
        "ReviewClinic", "ReviewClinicVerification", "ReviewProfileChange", "ReviewVeterinarian",
        "RevokeApiKey", "RevokeClinicStaffMember", "RevokeFinanceMember", "RevokeMyVeterinarian", "RotateApiKey",
        "Scan", "ScheduleVeterinarianAppointment", "SearchForAccess", "SendClinicCommunicationTemplate",
        "SetOwnerClinicCommunicationPreference", "SetVeterinarianPermissions", "SubmitFinanceFiscalSale",
        "SubmitMyFiscalSale", "SubmitMyVerification", "SubmitProfileChange", "SuspendVeterinarian", "TrackView",
        "UpdateMyProfile", "UpdateStaffAppointmentStatus", "UpdateVeterinarianAppointmentStatus",
        "UpdateVeterinarianScheduleBlock", "UploadClinicalConsultationAttachment", "UploadLogo",
        "UploadMyVerificationDocument", "UploadVeterinarianDocument", "UploadVeterinarianSignature",
        "UpsertClinicCommunicationPreference", "VerifyClinicForCertificates", "VerifyPatientMicrochip", "VoidClinicSale",
        "VoidFinanceSale",
    };

    private static readonly HashSet<string> AnonymousEndpointActions = new(StringComparer.Ordinal)
    {
        "Register", "GetPublicClinics", "GetPublicClinicProfile", "TrackView",
    };

    private static readonly HashSet<string> SensitiveClinicalMutations = new(StringComparer.Ordinal)
    {
        "Scan",
        "CreateApiKey",
        "RevokeApiKey",
        "RotateApiKey",
        "VerifyPatientMicrochip",
        "AddPatientMedicalRecord",
        "GenerateAccessCode",
        "AcceptOwnerCode",
        "ExportPatientMedical",
        "SubmitMyVerification",
        "UploadMyVerificationDocument",
        "ReviewClinic",
        "ReviewProfileChange",
        "VerifyClinicForCertificates",
        "ReviewClinicVerification",
        "ReviewVeterinarian",
        "SuspendVeterinarian",
    };

    private static readonly IReadOnlyDictionary<string, string> MfaExemptWriteReasons =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Register"] = "Public clinic onboarding; no authenticated session exists yet.",
            ["UpdateMyProfile"] = "Self-service display name/profile fields only.",
            ["SubmitProfileChange"] = "Stages clinic public-profile changes for review.",
            ["TrackView"] = "Anonymous public-directory view telemetry only.",
            ["UploadLogo"] = "Public presentation asset; no clinical or financial state.",
            ["CreateClinicSale"] = "Routine point-of-sale operation; refund/void remain step-up protected.",
            ["RegisterClinicSalePayment"] = "Routine cashier collection; privileged reversals remain step-up protected.",
            ["CreateFinanceSale"] = "Routine cashier point-of-sale operation; refund/void remain step-up protected.",
            ["RegisterFinancePayment"] = "Routine cashier collection; privileged reversals remain step-up protected.",
            ["SetOwnerClinicCommunicationPreference"] = "Tutor-controlled consent; the clinic cannot grant opt-in on the tutor's behalf.",
        };

    private static readonly HashSet<string> MfaPolicies = new(StringComparer.Ordinal)
    {
        "ClinicOperationsMfa",
        "ClinicStaffMfa",
        "ClinicFinanceMfa",
        "ClinicAdminMfa",
        "MfaStepUp",
    };

    private static readonly HashSet<string> AdditionalClinicalMutations = new(StringComparer.Ordinal)
    {
        "MedicalController.AddRecord",
        "MedicalController.CompleteReminder",
        "MedicalController.DeleteRecord",
        "MedicalController.UpdateRecord",
        "MedicalController.CreateReminder",
        "MedicalController.DeleteReminder",
        "PetClinicAccessController.GenerateCode",
        "PetClinicAccessController.AcceptClinicCode",
        "PetClinicAccessController.RevokeAccess",
        "CertificatesController.Issue",
        "CertificatesController.IssuePassport",
        "CertificatesController.Revoke",
    };

    private static readonly HashSet<string> AdditionalClinicalEndpointActions = new(StringComparer.Ordinal)
    {
        "MedicalController.GetMyReminders", "MedicalController.GetAccessLog", "MedicalController.GetCount",
        "MedicalController.GetHistory", "MedicalController.GetWeightHistory", "MedicalController.GetHealthAlerts",
        "MedicalController.GetHealthScore", "MedicalController.AddRecord", "MedicalController.GetReminders",
        "MedicalController.CompleteReminder", "MedicalController.ExportPdf", "MedicalController.GetAnnualReport",
        "MedicalController.DeleteRecord", "MedicalController.UpdateRecord", "MedicalController.CreateReminder",
        "MedicalController.DeleteReminder", "PetClinicAccessController.GetGrants", "PetClinicAccessController.GenerateCode",
        "PetClinicAccessController.AcceptClinicCode", "PetClinicAccessController.RevokeAccess",
        "CertificatesController.GetForClinic", "CertificatesController.GetForPet", "CertificatesController.Verify",
        "CertificatesController.Issue", "CertificatesController.IssuePassport", "CertificatesController.Download",
        "CertificatesController.Revoke",
    };

    private static readonly HashSet<string> ClinicIdTenantCases = new(StringComparer.Ordinal)
    {
        "GetStaffAgenda", "UpdateStaffAppointmentStatus", "CreateStaffConsultation", "CloseStaffConsultation",
        "GetFinanceReport", "GetFinanceSaleLedger", "CreateFinanceSale", "RegisterFinancePayment",
        "RecordFinanceRefund", "VoidFinanceSale", "CloseFinanceCash", "SubmitFinanceFiscalSale",
        "GetStaffClinicCrmDashboard", "GetStaffTaskAssignees", "CreateStaffClinicCrmTask",
        "CompleteStaffClinicCrmTask", "SetOwnerClinicCommunicationPreference",
    };

    private static readonly HashSet<string> ClinicIdPublicOrAdminCases = new(StringComparer.Ordinal)
    {
        "GetPublicClinicProfile", "TrackView", "ReviewClinic", "VerifyClinicForCertificates",
    };

    [Fact]
    public void EveryClinicIdRouteIsClassifiedForDynamicBolaTesting()
    {
        var actions = GetClinicActions()
            .Where(action => action.AttributeRouteInfo?.Template?.Contains("{clinicId:guid}", StringComparison.OrdinalIgnoreCase) == true)
            .Select(action => action.ActionName)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        actions.Should().BeEquivalentTo(ClinicIdTenantCases.Concat(ClinicIdPublicOrAdminCases),
            "new clinicId actions must be classified and supplied with real-own/foreign resource HTTP cases");
    }

    [Fact]
    public void EveryClinicEndpointDeclaresAuthenticationOrAnonymousAccess()
    {
        var actions = GetClinicActions();
        actions.Select(action => action.ActionName).Distinct(StringComparer.Ordinal)
            .Should().BeEquivalentTo(ClinicEndpointActions,
                "every route action must be present in the reviewed endpoint catalog");
        actions.Where(HasAttribute<AllowAnonymousAttribute>)
            .Select(action => action.ActionName).Distinct(StringComparer.Ordinal)
            .Should().BeEquivalentTo(AnonymousEndpointActions,
                "anonymous access is an explicit allowlist, not an accidental default");

        var unclassified = actions
            .Where(action => !HasAttribute<AllowAnonymousAttribute>(action)
                && !HasAttribute<AuthorizeAttribute>(action))
            .Select(action => action.ActionName)
            .OrderBy(name => name)
            .ToArray();

        unclassified.Should().BeEmpty("every route must declare whether it is authenticated or intentionally anonymous");
    }

    [Fact]
    public void EveryAdditionalClinicalEndpointDeclaresAuthenticationOrAnonymousAccess()
    {
        var actions = GetAdditionalClinicalActions();
        var identities = actions.Select(action => $"{action.ControllerTypeInfo.Name}.{action.ActionName}")
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        identities.Should().BeEquivalentTo(AdditionalClinicalEndpointActions,
            "every additional medical/certificate/grant action must be inventoried");

        actions.Where(HasAttribute<AllowAnonymousAttribute>)
            .Select(action => $"{action.ControllerTypeInfo.Name}.{action.ActionName}")
            .Distinct(StringComparer.Ordinal)
            .Should().BeEquivalentTo(["CertificatesController.Verify"],
                "only public certificate verification is anonymous in these clinical controllers");

        actions.Where(action => !HasAttribute<AllowAnonymousAttribute>(action)
                && !HasAttribute<AuthorizeAttribute>(action))
            .Select(action => $"{action.ControllerTypeInfo.Name}.{action.ActionName}")
            .Should().BeEmpty("every additional clinical route must declare its authentication boundary");
    }

    [Fact]
    public void EveryInventoriedClinicalActionHasHttpMethodAndRouteTemplate()
    {
        var actions = GetClinicActions().Concat(GetAdditionalClinicalActions()).ToArray();
        var missingRouteMetadata = actions
            .Where(action => string.IsNullOrWhiteSpace(action.AttributeRouteInfo?.Template)
                || !GetAttributes<HttpMethodAttribute>(action).SelectMany(attribute => attribute.HttpMethods).Any())
            .Select(action => $"{action.ControllerTypeInfo.Name}.{action.ActionName}")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(identity => identity)
            .ToArray();

        missingRouteMetadata.Should().BeEmpty("all inventoried actions must map to a concrete HTTP method and route template");
    }

    [Fact]
    public void EveryClinicMutationRequiresMfaOrHasAReviewedException()
    {
        var actions = GetClinicActions();
        var writes = actions
            .Where(IsWriteAction)
            .GroupBy(action => action.ActionName, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
        var writeNames = writes.Select(action => action.ActionName).ToHashSet(StringComparer.Ordinal);

        MfaExemptWriteReasons.Keys.Should().BeSubsetOf(writeNames,
            "exception entries must continue to refer to real write endpoints");

        var missingMfa = writes
            .Where(action => !MfaExemptWriteReasons.ContainsKey(action.ActionName)
                && !GetAuthorizationAttributes(action).Any(attribute => MfaPolicies.Contains(attribute.Policy ?? string.Empty)))
            .Select(action => action.ActionName)
            .OrderBy(name => name)
            .ToArray();

        missingMfa.Should().BeEmpty("every non-exempt mutation requires an MFA policy");
        MfaExemptWriteReasons.Values.Should().OnlyContain(reason => !string.IsNullOrWhiteSpace(reason));
    }

    [Fact]
    public void MedicalGrantAndCertificateMutationsRequireMfa()
    {
        var actions = GetAdditionalClinicalActions()
            .Where(IsWriteAction)
            .GroupBy(action => $"{action.ControllerTypeInfo.Name}.{action.ActionName}", StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();

        actions.Select(action => $"{action.ControllerTypeInfo.Name}.{action.ActionName}")
            .Should().BeEquivalentTo(AdditionalClinicalMutations,
                "clinical data/grant/certificate writes outside ClinicsController must remain in the reviewed matrix");

        var missingMfa = actions
            .Where(action => !GetAuthorizationAttributes(action).Any(attribute => MfaPolicies.Contains(attribute.Policy ?? string.Empty)))
            .Select(action => $"{action.ControllerTypeInfo.Name}.{action.ActionName}")
            .OrderBy(name => name)
            .ToArray();

        missingMfa.Should().BeEmpty("medical record, consent grant and certificate writes require MFA step-up");
    }

    [Fact]
    public async Task CrossControllerClinicalWritesRejectSessionsWithoutMfa()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory,
            $"clinical-write-mfa-{Guid.NewGuid():N}@pawtrack.cr");

        var medicalWrite = await client.PostAsJsonAsync($"/api/pets/{Guid.NewGuid()}/medical/reminders", new
        {
            type = "Vaccine",
            dueDate = "2027-01-01",
            title = "Rabies booster",
        });
        medicalWrite.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var grantWrite = await client.PostAsJsonAsync($"/api/pets/{Guid.NewGuid()}/clinic-access/accept", new { code = "ABCDEFGH" });
        grantWrite.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var certificateWrite = await client.PostAsJsonAsync("/api/certificates", new
        {
            petId = Guid.NewGuid(),
            clinicId = Guid.NewGuid(),
            type = "Vaccination",
            petName = "Luna",
            petSpecies = "Dog",
            clinicName = "Clinic",
            clinicLicense = "VET-001",
            vetName = "Dr. Mora",
        });
        certificateWrite.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OrganizationMembershipDoesNotGrantStaffAccessToAnotherClinic()
    {
        var ownerEmail = $"org-owner-{Guid.NewGuid():N}@pawtrack.cr";
        var staffEmail = $"org-staff-{Guid.NewGuid():N}@pawtrack.cr";
        _ = await AuthHelper.CreateAuthenticatedClientAsync(factory, ownerEmail);
        var staffClient = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, staffEmail);
        Guid primaryClinicId;
        Guid foreignClinicId;
        Guid appointmentId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            var staff = await db.Users.SingleAsync(user => user.Email == staffEmail);
            var primary = Clinic.Create(owner.Id, "Principal", $"VET-{Guid.NewGuid():N}"[..12], "San Jose", 9.93m, -84.08m, ownerEmail);
            var foreign = Clinic.Create(owner.Id, "Secundaria", $"VET-{Guid.NewGuid():N}"[..12], "Cartago", 9.86m, -83.92m, ownerEmail);
            primary.Activate();
            foreign.Activate();
            var organization = ClinicOrganization.Create("Red", owner.Id, primary.Id);
            organization.AddSite(foreign.Id);
            organization.AddMember(staff.Id, ClinicOrganizationRole.Member);
            var veterinarian = ClinicVeterinarian.Create(primary.Id, "Dra. Mora", $"VET-{Guid.NewGuid():N}"[..12]);
            var pet = Pet.Create(owner.Id, "Max", PetSpecies.Dog, null, null);
            var appointment = VeterinarianAppointment.Schedule(primary.Id, veterinarian.Id, pet.Id,
                DateTimeOffset.UtcNow.AddDays(1), TimeSpan.FromMinutes(30));
            db.Clinics.AddRange(primary, foreign);
            db.ClinicVeterinarians.Add(veterinarian);
            db.Pets.Add(pet);
            db.VeterinarianAppointments.Add(appointment);
            db.ClinicOrganizations.Add(organization);
            db.ClinicOrganizationSites.AddRange(organization.Sites);
            db.ClinicOrganizationMemberships.AddRange(organization.Memberships);
            db.ClinicStaffMemberships.Add(ClinicStaffMembership.Grant(primary.Id, staff.Id, ClinicStaffRole.Receptionist, owner.Id));
            db.ClinicFinanceMemberships.Add(ClinicFinanceMembership.Grant(primary.Id, staff.Id, ClinicFinanceRole.Administrator, owner.Id));
            await db.SaveChangesAsync();
            primaryClinicId = primary.Id;
            foreignClinicId = foreign.Id;
            appointmentId = appointment.Id;
        }

        var from = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddHours(-1).ToString("O"));
        var to = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddHours(1).ToString("O"));
        var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var cases = new (string Action, Func<Guid, string> Path, HttpStatusCode Denied)[]
        {
            ("GetStaffAgenda", id => $"/api/clinics/{id}/staff/appointments?from={from}&to={to}", HttpStatusCode.UnprocessableEntity),
            ("GetStaffClinicCrmDashboard", id => $"/api/clinics/{id}/staff/crm-dashboard?today={date}", HttpStatusCode.Forbidden),
            ("GetStaffTaskAssignees", id => $"/api/clinics/{id}/staff/task-assignees", HttpStatusCode.Forbidden),
            ("GetFinanceReport", id => $"/api/clinics/{id}/finance/sales-report?businessDate={date}", HttpStatusCode.UnprocessableEntity),
        };
        foreach (var prefix in new[] { "/api/clinics", "/api/v1/clinics" })
            foreach (var scenario in cases)
            {
                var own = await staffClient.GetAsync(scenario.Path(primaryClinicId).Replace("/api/clinics", prefix, StringComparison.Ordinal));
                own.StatusCode.Should().Be(HttpStatusCode.OK, $"{scenario.Action} must be usable for authorized clinic staff");
                var foreignResponse = await staffClient.GetAsync(scenario.Path(foreignClinicId).Replace("/api/clinics", prefix, StringComparison.Ordinal));
                foreignResponse.StatusCode.Should().Be(scenario.Denied, $"{scenario.Action} must reject a member of another clinic in the same organization");
            }

        var receipt = $"BOLA-{Guid.NewGuid():N}"[..16];
        var salePayload = new
        {
            receiptNumber = receipt,
            lines = new[] { new { description = "Consulta", type = "Service", quantity = 1, unitPriceCrc = 1000m } },
        };
        var ownSale = await staffClient.PostAsJsonAsync($"/api/clinics/{primaryClinicId}/finance/sales", salePayload);
        ownSale.StatusCode.Should().Be(HttpStatusCode.Created);
        using var saleJson = JsonDocument.Parse(await ownSale.Content.ReadAsStringAsync());
        var saleId = saleJson.RootElement.GetProperty("id").GetGuid();
        var foreignSale = await staffClient.PostAsJsonAsync($"/api/v1/clinics/{foreignClinicId}/finance/sales", salePayload);
        foreignSale.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var ownLedger = await staffClient.GetAsync($"/api/clinics/{primaryClinicId}/finance/sales/{saleId}/ledger");
        ownLedger.StatusCode.Should().Be(HttpStatusCode.OK);
        var foreignLedger = await staffClient.GetAsync($"/api/v1/clinics/{foreignClinicId}/finance/sales/{saleId}/ledger");
        foreignLedger.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var foreignAppointment = await staffClient.PatchAsJsonAsync(
            $"/api/v1/clinics/{foreignClinicId}/staff/appointments/{appointmentId}/status", new { status = "Confirmed" });
        foreignAppointment.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var ownAppointment = await staffClient.PatchAsJsonAsync(
            $"/api/clinics/{primaryClinicId}/staff/appointments/{appointmentId}/status", new { status = "Confirmed" });
        ownAppointment.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var foreignPayment = await staffClient.PostAsJsonAsync(
            $"/api/v1/clinics/{foreignClinicId}/finance/sales/{saleId}/payments",
            new { amountCrc = 1000m, method = "Cash", reference = "BOLA-CASH" });
        foreignPayment.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var ownPayment = await staffClient.PostAsJsonAsync(
            $"/api/clinics/{primaryClinicId}/finance/sales/{saleId}/payments",
            new { amountCrc = 1000m, method = "Cash", reference = "BOLA-CASH" });
        ownPayment.StatusCode.Should().Be(HttpStatusCode.OK);

        var taskPayload = new
        {
            type = "CallClient",
            dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            title = "Contactar tutor",
            idempotencyKey = Guid.NewGuid(),
            priority = "Normal",
            assignedRole = "Receptionist",
        };
        var ownTask = await staffClient.PostAsJsonAsync($"/api/clinics/{primaryClinicId}/staff/crm/tasks", taskPayload);
        ownTask.StatusCode.Should().Be(HttpStatusCode.Created);
        using var taskJson = JsonDocument.Parse(await ownTask.Content.ReadAsStringAsync());
        var taskId = taskJson.RootElement.GetProperty("taskId").GetGuid();
        var foreignTask = await staffClient.PostAsJsonAsync($"/api/v1/clinics/{foreignClinicId}/staff/crm/tasks", taskPayload);
        foreignTask.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var foreignCompletion = await staffClient.PostAsync($"/api/v1/clinics/{foreignClinicId}/staff/crm/tasks/{taskId}/complete", null);
        foreignCompletion.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var ownCompletion = await staffClient.PostAsync($"/api/clinics/{primaryClinicId}/staff/crm/tasks/{taskId}/complete", null);
        ownCompletion.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var paidLedger = await staffClient.GetAsync($"/api/clinics/{primaryClinicId}/finance/sales/{saleId}/ledger");
        paidLedger.StatusCode.Should().Be(HttpStatusCode.OK);
        using var ledgerJson = JsonDocument.Parse(await paidLedger.Content.ReadAsStringAsync());
        var paymentId = ledgerJson.RootElement.GetProperty("payments")[0].GetProperty("id").GetGuid();
        var refundPayload = new
        {
            paymentId,
            amountCrc = 1000m,
            reason = "Servicio cancelado",
            evidenceReference = $"REF-{Guid.NewGuid():N}",
        };
        var foreignRefund = await staffClient.PostAsJsonAsync($"/api/v1/clinics/{foreignClinicId}/finance/sales/{saleId}/refunds", refundPayload);
        foreignRefund.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var ownRefund = await staffClient.PostAsJsonAsync($"/api/clinics/{primaryClinicId}/finance/sales/{saleId}/refunds", refundPayload);
        ownRefund.StatusCode.Should().Be(HttpStatusCode.OK);

        var foreignVoid = await staffClient.PostAsJsonAsync($"/api/v1/clinics/{foreignClinicId}/finance/sales/{saleId}/void", new { reason = "Servicio cancelado" });
        foreignVoid.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var ownVoid = await staffClient.PostAsJsonAsync($"/api/clinics/{primaryClinicId}/finance/sales/{saleId}/void", new { reason = "Servicio cancelado" });
        ownVoid.StatusCode.Should().Be(HttpStatusCode.OK);

        var closeDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-6));
        var foreignClose = await staffClient.PostAsJsonAsync($"/api/v1/clinics/{foreignClinicId}/finance/cash-closes", new { businessDate = closeDate });
        foreignClose.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var ownClose = await staffClient.PostAsJsonAsync($"/api/clinics/{primaryClinicId}/finance/cash-closes", new { businessDate = closeDate });
        ownClose.StatusCode.Should().Be(HttpStatusCode.Created);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
        (await verifyDb.ClinicSales.CountAsync(sale => sale.ClinicId == foreignClinicId)).Should().Be(0);
        (await verifyDb.ClinicCrmTasks.CountAsync(task => task.ClinicId == foreignClinicId)).Should().Be(0);
        (await verifyDb.VeterinarianAppointments.SingleAsync(item => item.Id == appointmentId)).Status
            .Should().Be(VeterinarianAppointmentStatus.Confirmed);
        (await verifyDb.ClinicSalePayments.CountAsync(payment => payment.SaleId == saleId)).Should().Be(1);
    }

    [Fact]
    public async Task FinanceFiscalSubmissionRejectsAnotherOrganizationSiteBeforeCallingProvider()
    {
        var issuer = Substitute.For<IClinicFiscalIssuerRegistry>();
        issuer.GetVerifiedIssuerTaxId(Arg.Any<Guid>()).Returns("123456789");
        var gateway = Substitute.For<IClinicFiscalGateway>();
        gateway.SubmitAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success("stub-fiscal-reference"));
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IClinicFiscalIssuerRegistry>();
            services.RemoveAll<IClinicFiscalGateway>();
            services.AddSingleton(issuer);
            services.AddSingleton(gateway);
        }));
        var ownerEmail = $"fiscal-owner-{Guid.NewGuid():N}@pawtrack.cr";
        var staffEmail = $"fiscal-staff-{Guid.NewGuid():N}@pawtrack.cr";
        _ = await AuthHelper.CreateAuthenticatedClientAsync(factory, ownerEmail);
        var authenticatedStaff = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, staffEmail);
        var staffClient = app.CreateClient();
        staffClient.DefaultRequestHeaders.Authorization = authenticatedStaff.DefaultRequestHeaders.Authorization;
        Guid ownClinicId;
        Guid foreignClinicId;
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            var staff = await db.Users.SingleAsync(user => user.Email == staffEmail);
            var ownClinic = Clinic.Create(owner.Id, "Fiscal A", $"VET-{Guid.NewGuid():N}"[..12], "San Jose", 9.93m, -84.08m, ownerEmail);
            var foreignClinic = Clinic.Create(Guid.NewGuid(), "Fiscal B", $"VET-{Guid.NewGuid():N}"[..12], "Cartago", 9.86m, -83.92m, "foreign@pawtrack.test");
            ownClinic.Activate();
            foreignClinic.Activate();
            var organization = ClinicOrganization.Create("Fiscal Red", owner.Id, ownClinic.Id);
            organization.AddSite(foreignClinic.Id);
            organization.AddMember(staff.Id, ClinicOrganizationRole.FinanceManager);
            db.Clinics.AddRange(ownClinic, foreignClinic);
            db.ClinicOrganizations.Add(organization);
            db.ClinicOrganizationMemberships.AddRange(organization.Memberships);
            db.ClinicOrganizationSites.AddRange(organization.Sites);
            db.ClinicFinanceMemberships.Add(ClinicFinanceMembership.Grant(ownClinic.Id, staff.Id, ClinicFinanceRole.Administrator, owner.Id));
            await db.SaveChangesAsync();
            ownClinicId = ownClinic.Id;
            foreignClinicId = foreignClinic.Id;
        }

        var saleResponse = await staffClient.PostAsJsonAsync($"/api/clinics/{ownClinicId}/finance/sales", new
        {
            receiptNumber = $"FISC-{Guid.NewGuid():N}"[..16],
            lines = new[] { new { description = "Consulta", type = "Service", quantity = 1, unitPriceCrc = 1000m } },
        });
        saleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var saleJson = JsonDocument.Parse(await saleResponse.Content.ReadAsStringAsync());
        var saleId = saleJson.RootElement.GetProperty("id").GetGuid();
        var payment = await staffClient.PostAsJsonAsync($"/api/clinics/{ownClinicId}/finance/sales/{saleId}/payments",
            new { amountCrc = 1000m, method = "Cash", reference = "FISC-BOLA" });
        payment.StatusCode.Should().Be(HttpStatusCode.OK);

        var denied = await staffClient.PostAsync(
            $"/api/v1/clinics/{foreignClinicId}/finance/sales/{saleId}/fiscal-submission", null);
        denied.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var accepted = await staffClient.PostAsync(
            $"/api/clinics/{ownClinicId}/finance/sales/{saleId}/fiscal-submission", null);
        accepted.StatusCode.Should().Be(HttpStatusCode.Accepted);
        await gateway.Received(1).SubmitAsync(ownClinicId, saleId, Arg.Any<string>(), Arg.Any<string>(),
            1000m, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClinicApiKeysCannotBeRotatedOrRevokedByAnotherClinic()
    {
        var emailA = $"key-a-{Guid.NewGuid():N}@pawtrack.cr";
        var emailB = $"key-b-{Guid.NewGuid():N}@pawtrack.cr";
        var clientA = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, emailA);
        var clientB = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, emailB);
        Guid clinicAId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var ownerA = await db.Users.SingleAsync(user => user.Email == emailA);
            var ownerB = await db.Users.SingleAsync(user => user.Email == emailB);
            ownerA.AssignClinicRole();
            ownerB.AssignClinicRole();
            var clinicA = Clinic.Create(ownerA.Id, "Keys A", $"VET-{Guid.NewGuid():N}"[..12], "San Jose", 9.93m, -84.08m, emailA);
            var clinicB = Clinic.Create(ownerB.Id, "Keys B", $"VET-{Guid.NewGuid():N}"[..12], "Cartago", 9.86m, -83.92m, emailB);
            clinicA.Activate();
            clinicB.Activate();
            var planA = Subscription.CreateForClinic(clinicA.Id, ownerA.Id, SubscriptionTier.ClinicPartner, $"K{Guid.NewGuid():N}"[..8], 35000m);
            var planB = Subscription.CreateForClinic(clinicB.Id, ownerB.Id, SubscriptionTier.ClinicPartner, $"K{Guid.NewGuid():N}"[..8], 35000m);
            planA.Activate();
            planB.Activate();
            db.Clinics.AddRange(clinicA, clinicB);
            db.Subscriptions.AddRange(planA, planB);
            await db.SaveChangesAsync();
            clinicAId = clinicA.Id;
            clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(ownerA.Id, ownerA.Email, ownerA.Name, ownerA.Role, mfaVerified: true));
            clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(ownerB.Id, ownerB.Email, ownerB.Name, ownerB.Role, mfaVerified: true));
        }

        var created = await clientA.PostAsJsonAsync("/api/clinics/me/api-keys", new { label = "Partner A", scopes = new[] { "scan" } });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var keyId = createdJson.RootElement.GetProperty("id").GetGuid();
        var keysB = await clientB.GetAsync("/api/v1/clinics/me/api-keys");
        keysB.StatusCode.Should().Be(HttpStatusCode.OK);
        var keysBJson = await keysB.Content.ReadFromJsonAsync<JsonElement>();
        keysBJson.GetArrayLength().Should().Be(0);

        var foreignRotation = await clientB.PostAsync($"/api/v1/clinics/me/api-keys/{keyId}/rotate", null);
        foreignRotation.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var foreignRevocation = await clientB.DeleteAsync($"/api/v1/clinics/me/api-keys/{keyId}");
        foreignRevocation.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var ownRotation = await clientA.PostAsync($"/api/clinics/me/api-keys/{keyId}/rotate", null);
        ownRotation.StatusCode.Should().Be(HttpStatusCode.OK);
        using var rotatedJson = JsonDocument.Parse(await ownRotation.Content.ReadAsStringAsync());
        var rotatedId = rotatedJson.RootElement.GetProperty("id").GetGuid();
        var ownRevocation = await clientA.DeleteAsync($"/api/clinics/me/api-keys/{rotatedId}");
        ownRevocation.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
        (await verifyDb.ClinicApiKeys.CountAsync(key => key.ClinicId == clinicAId)).Should().Be(2);
        (await verifyDb.ClinicApiKeys.CountAsync(key => key.ClinicId == clinicAId && key.IsRevoked)).Should().Be(2);
    }

    [Fact]
    public void SensitiveClinicalMutationsRequireAnMfaPolicy()
    {
        var actions = GetClinicActions()
            .Where(action => SensitiveClinicalMutations.Contains(action.ActionName))
            .ToArray();

        actions.Select(action => action.ActionName).Distinct(StringComparer.Ordinal)
            .Should().BeEquivalentTo(SensitiveClinicalMutations,
            "the security matrix must fail when a sensitive action is renamed or removed without review");

        var missingMfa = actions
            .Where(action => !GetAuthorizationAttributes(action).Any(attribute => MfaPolicies.Contains(attribute.Policy ?? string.Empty)))
            .Select(action => action.ActionName)
            .OrderBy(name => name)
            .ToArray();

        missingMfa.Should().BeEmpty("clinical data, grants and verification mutations require a verified MFA claim");
    }

    [Fact]
    public async Task AcceptOwnerAccessGrantRequiresMfa()
    {
        var clinicEmail = $"clinic-grant-{Guid.NewGuid():N}@pawtrack.cr";
        var ownerEmail = $"owner-grant-{Guid.NewGuid():N}@pawtrack.cr";
        var clinicClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, clinicEmail);
        _ = await AuthHelper.CreateAuthenticatedClientAsync(factory, ownerEmail);
        string rawCode;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var clinicUser = await db.Users.SingleAsync(user => user.Email == clinicEmail);
            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            clinicUser.AssignClinicRole();
            var clinic = Clinic.Create(clinicUser.Id, "Clinica Grant", $"VET-{Guid.NewGuid():N}"[..12], "San Jose", 9.93m, -84.08m, clinicEmail);
            clinic.Activate();
            var pet = Pet.Create(owner.Id, "Luna", PetSpecies.Dog, null, null);
            var (grant, code) = ClinicMedicalAccessGrant.Generate(pet.Id, clinic.Id, owner.Id, "Owner");
            await db.Clinics.AddAsync(clinic);
            await db.Pets.AddAsync(pet);
            await db.ClinicMedicalAccessGrants.AddAsync(grant);
            await db.SaveChangesAsync();
            rawCode = code;
            clinicClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(clinicUser.Id, clinicUser.Email, clinicUser.Name, clinicUser.Role));
        }

        var denied = await clinicClient.PostAsJsonAsync("/api/clinics/access-grants/accept", new { code = rawCode });
        denied.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var clinicUser = await db.Users.SingleAsync(user => user.Email == clinicEmail);
            clinicClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(clinicUser.Id, clinicUser.Email, clinicUser.Name, clinicUser.Role, mfaVerified: true));
        }

        var accepted = await clinicClient.PostAsJsonAsync("/api/clinics/access-grants/accept", new { code = rawCode });
        accepted.StatusCode.Should().Be(HttpStatusCode.OK);
        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
        (await verifyDb.ClinicMedicalAccessGrants.AnyAsync(grant => grant.AcceptedAt != null && grant.IsActive)).Should().BeTrue();
    }

    private ControllerActionDescriptor[] GetClinicActions()
    {
        var provider = factory.Services.GetRequiredService<IActionDescriptorCollectionProvider>();
        return provider.ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .Where(action => action.ControllerTypeInfo.AsType() == typeof(ClinicsController))
            .ToArray();
    }

    private ControllerActionDescriptor[] GetAdditionalClinicalActions()
    {
        var provider = factory.Services.GetRequiredService<IActionDescriptorCollectionProvider>();
        var controllerTypes = new HashSet<Type>
        {
            typeof(MedicalController),
            typeof(PetClinicAccessController),
            typeof(CertificatesController),
        };
        return provider.ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .Where(action => controllerTypes.Contains(action.ControllerTypeInfo.AsType()))
            .ToArray();
    }

    private static bool HasAttribute<TAttribute>(ControllerActionDescriptor action)
        where TAttribute : Attribute => GetAttributes<TAttribute>(action).Any();

    private static IEnumerable<TAttribute> GetAttributes<TAttribute>(ControllerActionDescriptor action)
        where TAttribute : Attribute => action.MethodInfo.GetCustomAttributes<TAttribute>(inherit: true)
            .Concat(action.ControllerTypeInfo.GetCustomAttributes<TAttribute>(inherit: true));

    private static IEnumerable<AuthorizeAttribute> GetAuthorizationAttributes(ControllerActionDescriptor action) =>
        GetAttributes<AuthorizeAttribute>(action);

    private static bool IsWriteAction(ControllerActionDescriptor action) =>
        action.MethodInfo.GetCustomAttributes<HttpMethodAttribute>(inherit: true)
            .SelectMany(attribute => attribute.HttpMethods)
            .Any(method => method is not ("GET" or "HEAD" or "OPTIONS"));
}
