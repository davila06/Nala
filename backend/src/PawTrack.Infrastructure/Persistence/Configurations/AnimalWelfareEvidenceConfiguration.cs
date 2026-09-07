using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.AnimalWelfare;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class AnimalWelfareEvidenceConfiguration : IEntityTypeConfiguration<AnimalWelfareEvidence>
{
    public void Configure(EntityTypeBuilder<AnimalWelfareEvidence> builder)
    {
        builder.ToTable("AnimalWelfareEvidence");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.BlobUrl).IsRequired().HasMaxLength(500);
        builder.Property(e => e.ContentType).IsRequired().HasMaxLength(80);
        builder.Property(e => e.EvidenceKind).IsRequired().HasConversion<string>().HasMaxLength(40);
        builder.Property(e => e.HashSha256).HasMaxLength(64);
        builder.HasIndex(e => new { e.CaseId, e.UploadedAt });
    }
}
