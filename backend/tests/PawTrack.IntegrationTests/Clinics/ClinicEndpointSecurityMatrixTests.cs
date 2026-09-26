using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Common.Interfaces;
using PawTrack.API.Controllers;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Medical;
using PawTrack.Domain.Pets;
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
        "GenerateAccessCode", "GetApiKeys", "GetAuthorizedPets", "GetCertificateIssuers", "GetClinicAgendaAudit",
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
