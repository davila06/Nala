using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Imports;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Imports;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.Imports;

public sealed class ProviderServiceImportTests
{
    [Fact]
    public async Task ProcessProviderServices_imports_valid_rows_and_keeps_row_errors()
    {
        var ownerId = Guid.NewGuid();
        var provider = ServiceProvider.Create(ownerId, "Vet", "Services", ServiceProviderCategory.Trainer, "San Jose", 9m, -84m, "vet@example.com");
        provider.Activate();
        var imports = Substitute.For<IImportJobRepository>();
        var providers = Substitute.For<IServiceProviderRepository>();
        var entitlements = Substitute.For<IEntitlementService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        providers.GetByUserIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(provider);
        providers.GetServicesByProviderAsync(provider.Id, Arg.Any<CancellationToken>()).Returns([]);
        entitlements.AuthorizeAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<EntitlementContext>(), Arg.Any<CancellationToken>())
            .Returns(new EntitlementDecision(true, true, 25m, 0m, 25m, null, Domain.Subscriptions.SubscriptionTier.Free));

        var json = "[{\"name\":\"Paseo\",\"description\":\"Canino\",\"modality\":\"AtProviderLocation\",\"durationMinutes\":60,\"priceCrc\":25000,\"capacity\":1},{\"name\":\"Invalido\",\"durationMinutes\":5,\"priceCrc\":1,\"capacity\":1}]"u8.ToArray();
        var processor = new StoreProductImportProcessor(imports, Substitute.For<IStoreRepository>(), providers, entitlements, unitOfWork);

        var job = await processor.ProcessProviderServicesAsync(ownerId, "json", "hash", "import-1", json, default);

        job.Status.Should().Be(ImportJobStatus.CompletedWithErrors);
        job.ImportedCount.Should().Be(1);
        job.Errors.Should().ContainSingle(error => error.Code == "INVALID_DURATION");
        await providers.Received(1).AddServiceAsync(Arg.Any<ProviderService>(), Arg.Any<CancellationToken>());
    }
}
