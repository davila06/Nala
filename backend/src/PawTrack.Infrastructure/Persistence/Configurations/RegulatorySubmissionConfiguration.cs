using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Regulatory;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class RegulatorySubmissionConfiguration : IEntityTypeConfiguration<RegulatorySubmission>
{
    public void Configure(EntityTypeBuilder<RegulatorySubmission> builder)
    {
        builder.ToTable("RegulatorySubmissions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Destination).IsRequired().HasMaxLength(120);
        builder.Property(s => s.SubmissionType).IsRequired().HasMaxLength(80);
        builder.Property(s => s.IdempotencyKey).IsRequired().HasMaxLength(200);
        builder.Property(s => s.PayloadSha256).IsRequired().HasMaxLength(64);
        builder.Property(s => s.PayloadBlobUrl).HasMaxLength(500);
        builder.Property(s => s.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.ExternalReference).HasMaxLength(200);
        builder.Property(s => s.ErrorCode).HasMaxLength(120);
        builder.HasIndex(s => s.IdempotencyKey).IsUnique();
        builder.HasIndex(s => new { s.ExportId, s.CreatedAt });
        builder.HasIndex(s => new { s.Status, s.CreatedAt });
    }
}
