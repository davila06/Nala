using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Clinics;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicalConsultationConfiguration : IEntityTypeConfiguration<ClinicalConsultation>
{
    public void Configure(EntityTypeBuilder<ClinicalConsultation> builder)
    {
        builder.ToTable("ClinicalConsultations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Subjective).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Objective).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Assessment).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Plan).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Diagnosis).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Treatment).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.OwnerSummary).HasMaxLength(1500).IsRequired();
        builder.Property(x => x.PrescriptionInstructions).HasMaxLength(1500);
        builder.Property(x => x.HydrationStatus).HasMaxLength(100);
        builder.Property(x => x.AttachmentUrl).HasMaxLength(500);
        builder.Property(x => x.SignedByName).HasMaxLength(160);
        builder.Property(x => x.WeightKg).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TemperatureC).HasColumnType("decimal(4,1)");
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.AppointmentId).IsUnique();
        builder.HasIndex(x => new { x.ClinicId, x.PetId, x.CreatedAt });
    }
}
