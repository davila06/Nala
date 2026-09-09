using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Medical;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicMedicalExportConfiguration : IEntityTypeConfiguration<ClinicMedicalExport>
{
    public void Configure(EntityTypeBuilder<ClinicMedicalExport> builder)
    {
        builder.ToTable("ClinicMedicalExports");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.BlobUrl).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => new { x.ClinicId, x.CreatedAt });
        builder.HasIndex(x => new { x.ClinicId, x.PetId, x.CreatedAt });
    }
}