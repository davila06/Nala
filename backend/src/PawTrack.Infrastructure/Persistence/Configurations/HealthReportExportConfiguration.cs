using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Medical;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class HealthReportExportConfiguration : IEntityTypeConfiguration<HealthReportExport>
{
    public void Configure(EntityTypeBuilder<HealthReportExport> builder)
    {
        builder.ToTable("HealthReportExports");
        builder.HasKey(export => export.Id);
        builder.Property(export => export.Id).ValueGeneratedNever();
        builder.Property(export => export.PetId).IsRequired();
        builder.Property(export => export.RequestedByUserId).IsRequired();
        builder.Property(export => export.Status).HasConversion<int>().IsRequired();
        builder.Property(export => export.BlobUrl).HasMaxLength(500);
        builder.Property(export => export.ErrorCode).HasMaxLength(100);
        builder.Property(export => export.ItemCount);
        builder.Property(export => export.AttemptCount).IsRequired();
        builder.Property(export => export.RequestedAt).IsRequired();
        builder.Property(export => export.ExpiresAt).IsRequired();
        builder.HasIndex(export => new { export.Status, export.RequestedAt });
        builder.HasIndex(export => new { export.Status, export.StartedAt });
        builder.HasIndex(export => new { export.RequestedByUserId, export.PetId, export.RequestedAt });
    }
}
