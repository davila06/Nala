using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Clinics;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicScanConfiguration : IEntityTypeConfiguration<ClinicScan>
{
    public void Configure(EntityTypeBuilder<ClinicScan> builder)
    {
        builder.ToTable("ClinicScans");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.ClinicId).IsRequired();
        builder.HasIndex(s => s.ClinicId);

        builder.Property(s => s.ScanInput)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(s => s.InputType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.MatchedPetId); // nullable

        builder.Property(s => s.ScannedAt).IsRequired();

        // composite index for auditing queries by clinic + time
        builder.HasIndex(s => new { s.ClinicId, s.ScannedAt });
    }
}
