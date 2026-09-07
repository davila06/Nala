using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Regulatory;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class RegulatoryExportConfiguration : IEntityTypeConfiguration<RegulatoryExport>
{
    public void Configure(EntityTypeBuilder<RegulatoryExport> builder)
    {
        builder.ToTable("RegulatoryExports");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.ExportCode).IsRequired().HasMaxLength(20);
        builder.Property(e => e.ReportType).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.Scope).IsRequired().HasConversion<string>().HasMaxLength(25);
        builder.Property(e => e.Format).IsRequired().HasConversion<string>().HasMaxLength(10);
        builder.Property(e => e.SchemaVersion).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Canton).HasMaxLength(80);
        builder.Property(e => e.TimeZone).IsRequired().HasMaxLength(80);
        builder.Property(e => e.IdempotencyKey).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.PayloadSha256).HasMaxLength(64);
        builder.Property(e => e.BlobUrl).HasMaxLength(500);
        builder.Property(e => e.ErrorCode).HasMaxLength(120);
        builder.Ignore(e => e.IsDownloadable);

        builder.HasIndex(e => e.ExportCode).IsUnique();
        builder.HasIndex(e => new { e.RequestedByUserId, e.RequestedAt });
        builder.HasIndex(e => new { e.Status, e.RequestedAt });
        builder.HasIndex(e => new { e.ReportType, e.SchemaVersion });
        builder.HasIndex(e => new { e.OrganizationId, e.PeriodStart, e.PeriodEnd });
        builder.HasIndex(e => new { e.RequestedByUserId, e.IdempotencyKey }).IsUnique();
    }
}
