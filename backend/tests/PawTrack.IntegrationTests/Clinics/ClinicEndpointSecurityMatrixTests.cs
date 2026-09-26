using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.API.Controllers;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Clinics;

[Collection("Integration")]
public sealed class ClinicEndpointSecurityMatrixTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    private static readonly HashSet<string> SensitiveClinicalMutations = new(StringComparer.Ordinal)
    {
        "VerifyPatientMicrochip",
        "AddPatientMedicalRecord",
        "GenerateAccessCode",
        "AcceptOwnerCode",
        "ExportPatientMedical",
        "SubmitMyVerification",
        "UploadMyVerificationDocument",
    };

    private static readonly HashSet<string> MfaPolicies = new(StringComparer.Ordinal)
    {
        "ClinicOperationsMfa",
        "ClinicStaffMfa",
        "ClinicFinanceMfa",
        "ClinicAdminMfa",
    };

    [Fact]
    public void EveryClinicEndpointDeclaresAuthenticationOrAnonymousAccess()
    {
        var actions = GetClinicActions();
        var unclassified = actions
            .Where(action => !HasAttribute<AllowAnonymousAttribute>(action)
                && !HasAttribute<AuthorizeAttribute>(action))
            .Select(action => action.ActionName)
            .OrderBy(name => name)
            .ToArray();

        unclassified.Should().BeEmpty("every route must declare whether it is authenticated or intentionally anonymous");
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

    private ControllerActionDescriptor[] GetClinicActions()
    {
        var provider = factory.Services.GetRequiredService<IActionDescriptorCollectionProvider>();
        return provider.ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .Where(action => action.ControllerTypeInfo.AsType() == typeof(ClinicsController))
            .ToArray();
    }

    private static bool HasAttribute<TAttribute>(ControllerActionDescriptor action)
        where TAttribute : Attribute => GetAttributes<TAttribute>(action).Any();

    private static IEnumerable<TAttribute> GetAttributes<TAttribute>(ControllerActionDescriptor action)
        where TAttribute : Attribute => action.MethodInfo.GetCustomAttributes<TAttribute>(inherit: true)
            .Concat(action.ControllerTypeInfo.GetCustomAttributes<TAttribute>(inherit: true));

    private static IEnumerable<AuthorizeAttribute> GetAuthorizationAttributes(ControllerActionDescriptor action) =>
        GetAttributes<AuthorizeAttribute>(action);
}
