using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Safety;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class FraudReportConfiguration : IEntityTypeConfiguration<FraudReport>
{
    public void Configure(EntityTypeBuilder<FraudReport> builder)
    {
        builder.ToTable("FraudReports");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.ReporterUserId);

        builder.Property(f => f.ReporterIpHash)
               .HasMaxLength(64)     // SHA-256 hex = 64 chars
               .IsRequired();

        builder.Property(f => f.Context)
               .HasConversion<string>()
               .HasMaxLength(30)
               .IsRequired();

        builder.Property(f => f.RelatedEntityId);
        builder.Property(f => f.TargetUserId);

        builder.Property(f => f.Description)
               .HasMaxLength(500);

        builder.Property(f => f.ReportedAt).IsRequired();

        builder.Property(f => f.SuspicionLevel)
               .HasConversion<int>()
               .IsRequired();

        // Indices used by the rolling-window pattern queries.
        builder.HasIndex(f => new { f.TargetUserId, f.ReportedAt });
        builder.HasIndex(f => new { f.ReporterIpHash, f.ReportedAt });
    }
}
