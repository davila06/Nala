using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Imports;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Imports;
using PawTrack.Domain.Stores;

namespace PawTrack.UnitTests.Imports;

public sealed class StoreProductImportTests
{
    [Fact]
    public async Task Process_imports_valid_product_and_records_invalid_price()
    {
        var ownerId = Guid.NewGuid();
        var store = Store.Create(ownerId, "Tienda", "Mascotas", "San Jose", 9m, -84m, "store@example.com");
        store.Activate();
        var imports = Substitute.For<IImportJobRepository>();
        var stores = Substitute.For<IStoreRepository>();
        var providers = Substitute.For<IServiceProviderRepository>();
        var entitlements = Substitute.For<IEntitlementService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        stores.GetByUserIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(store);
        stores.GetProductsByStoreAsync(store.Id, Arg.Any<CancellationToken>()).Returns([]);
        entitlements.AuthorizeAsync(Arg.Any<Guid>(), "MaxActiveProducts", Arg.Any<decimal>(), Arg.Any<EntitlementContext>(), Arg.Any<CancellationToken>())
            .Returns(new EntitlementDecision(true, true, 100m, 0m, 100m, null, Domain.Subscriptions.SubscriptionTier.StorePartner));

        var csv = "name,description,category,priceCrc\nCollar,Azul,Accessories,12000\nComida,Seca,Food,nope"u8.ToArray();
        var processor = new StoreProductImportProcessor(imports, stores, providers, entitlements, unitOfWork);

        var job = await processor.ProcessAsync(ownerId, "csv", "hash-store", "import-store-1", csv, default);

        job.Status.Should().Be(ImportJobStatus.CompletedWithErrors);
        job.ImportedCount.Should().Be(1);
        job.Errors.Should().ContainSingle(error => error.Code == "INVALID_DECIMAL");
        await stores.Received(1).AddProductAsync(Arg.Any<StoreProduct>(), Arg.Any<CancellationToken>());
    }
}
