using FluentAssertions;
using PawTrack.Domain.Imports;

namespace PawTrack.UnitTests.Imports;

public sealed class ImportJobDomainTests
{
    [Fact]
    public void Create_starts_pending_with_hash_and_tenant_context()
    {
        var tenantId = Guid.NewGuid();
        var job = ImportJob.Create(tenantId, "StoreProducts", "csv", "hash-1", "idem-1", 10);

        job.Status.Should().Be(ImportJobStatus.Pending);
        job.TenantId.Should().Be(tenantId);
        job.ResourceType.Should().Be("StoreProducts");
        job.RowCount.Should().Be(10);
        job.IdempotencyKey.Should().Be("idem-1");
    }

    [Fact]
    public void AddError_records_row_and_column_without_secret_payload()
    {
        var job = ImportJob.Create(Guid.NewGuid(), "StoreProducts", "csv", "hash-2", "idem-2", 1);

        job.AddError(4, "price", "INVALID_DECIMAL", "Price is invalid", "not-a-secret-value");

        job.Errors.Should().ContainSingle(error =>
            error.RowNumber == 4 && error.Column == "price" && error.Code == "INVALID_DECIMAL");
        job.Errors.Single().RawValue.Should().Be("not-a-secret-value");
    }
}
