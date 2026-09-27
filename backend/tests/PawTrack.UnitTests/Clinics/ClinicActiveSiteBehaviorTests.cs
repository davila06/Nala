using System.Reflection;
using System.Runtime.CompilerServices;
using FluentAssertions;
using MediatR;
using NSubstitute;
using PawTrack.Application;
using PawTrack.Application.Common.Behaviors;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicActiveSiteBehaviorTests
{
    [Fact]
    public async Task EveryClinicRequestWithClinicIdRejectsAnUnselectedSiteBeforeHandler()
    {
        var context = Substitute.For<IActiveClinicSiteContext>();
        context.IsClinicPrincipal.Returns(true);
        context.IsPlatformAdministrator.Returns(false);
        context.ClinicId.Returns(Guid.NewGuid());
        var requestTypes = typeof(ApplicationServiceCollectionExtensions).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface)
            .Where(type => type.Namespace is { } requestNamespace && IsClinicRequestNamespace(requestNamespace))
            .Where(type => type.GetProperty("ClinicId")?.PropertyType == typeof(Guid)
                || type.GetProperty("ClinicId")?.PropertyType == typeof(Guid?))
            .Where(type => !type.IsDefined(typeof(BypassClinicActiveSiteAttribute), inherit: true))
            .Where(type => type.GetInterfaces().Any(contract =>
                contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequest<>)))
            .ToArray();

        requestTypes.Should().HaveCountGreaterThan(20);
        var invoke = typeof(ClinicActiveSiteBehaviorTests).GetMethod(
            nameof(IsRejectedBeforeHandlerAsync), BindingFlags.Static | BindingFlags.NonPublic)!;

        foreach (var requestType in requestTypes)
        {
            var requestContract = requestType.GetInterfaces().First(contract =>
                contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequest<>));
            var responseType = requestContract.GetGenericArguments()[0];
            var request = RuntimeHelpers.GetUninitializedObject(requestType);
            requestType.GetProperty("ClinicId")!.SetValue(request, Guid.NewGuid());
            var closedInvoke = invoke.MakeGenericMethod(requestType, responseType);
            var task = (Task<bool>)closedInvoke.Invoke(null, [request, context])!;

            (await task).Should().BeTrue($"{requestType.FullName} must enforce the active clinic scope");
        }
    }

    private static async Task<bool> IsRejectedBeforeHandlerAsync<TRequest, TResponse>(
        object request,
        IActiveClinicSiteContext context)
        where TRequest : notnull
    {
        var handlerInvoked = false;
        RequestHandlerDelegate<TResponse> next = () =>
        {
            handlerInvoked = true;
            return Task.FromResult(default(TResponse)!);
        };

        try
        {
            await new ClinicActiveSiteBehavior<TRequest, TResponse>(context)
                .Handle((TRequest)request, next, CancellationToken.None);
            return false;
        }
        catch (ClinicActiveSiteRequiredException)
        {
            return !handlerInvoked;
        }
    }

    private static bool IsClinicRequestNamespace(string requestNamespace) =>
        requestNamespace.StartsWith("PawTrack.Application.Clinics", StringComparison.Ordinal)
        || requestNamespace.StartsWith("PawTrack.Application.Certificates", StringComparison.Ordinal)
        || requestNamespace.StartsWith("PawTrack.Application.Medical.ClinicAccess", StringComparison.Ordinal)
        || requestNamespace.StartsWith("PawTrack.Application.Pets.SanitaryIdentity", StringComparison.Ordinal)
        || requestNamespace.StartsWith("PawTrack.Application.CastrationCampaigns", StringComparison.Ordinal)
        || requestNamespace.StartsWith("PawTrack.Application.Subscriptions", StringComparison.Ordinal);
}
