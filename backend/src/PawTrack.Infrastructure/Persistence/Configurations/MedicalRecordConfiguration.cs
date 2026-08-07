using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Medical;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class MedicalRecordConfiguration : IEntityTypeConfiguration<MedicalRecord>
{
    public void Configure(EntityTypeBuilder<MedicalRecord> builder)
    {
        builder.ToTable("MedicalRecords");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PetId).IsRequired();
        builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.ClinicId); // nullable FK to Clinics

        builder.Property(x => x.Type).IsRequired().HasConversion<int>();
        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.Description).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.VetName).HasMaxLength(120);
        builder.Property(x => x.ClinicName).HasMaxLength(200);
        builder.Property(x => x.DocumentUrl).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.Property(x => x.WeightKg).HasColumnType("decimal(5,2)");
        builder.Property(x => x.DosageDescription).HasMaxLength(300);
        builder.Property(x => x.Frequency).HasMaxLength(100);
        builder.Property(x => x.DurationDays);
        builder.Property(x => x.MedicationEndDate);

        builder.HasIndex(x => x.PetId);
        builder.HasIndex(x => new { x.PetId, x.Date });
        builder.HasIndex(x => x.ClinicId).HasFilter("[ClinicId] IS NOT NULL");
    }
}
