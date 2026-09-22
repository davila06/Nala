using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Imports;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> builder)
    {
        builder.ToTable("ImportJobs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ResourceType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Format).HasMaxLength(10).IsRequired();
        builder.Property(x => x.FileHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.FileHash });
        builder.Ignore(x => x.Errors);
        builder.HasMany<ImportRowError>("_errors").WithOne().HasForeignKey(x => x.ImportJobId);
    }
}

public sealed class ImportRowErrorConfiguration : IEntityTypeConfiguration<ImportRowError>
{
    public void Configure(EntityTypeBuilder<ImportRowError> builder)
    {
        builder.ToTable("ImportRowErrors");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Column).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(500).IsRequired();
        builder.Property(x => x.RawValue).HasMaxLength(500);
        builder.HasIndex(x => new { x.ImportJobId, x.RowNumber });
    }
}
