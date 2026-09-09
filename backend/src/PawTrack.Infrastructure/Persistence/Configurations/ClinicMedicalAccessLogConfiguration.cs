using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Medical;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicMedicalAccessLogConfiguration : IEntityTypeConfiguration<ClinicMedicalAccessLog>
{
    public void Configure(EntityTypeBuilder<ClinicMedicalAccessLog> builder)
    {
        builder.ToTable("ClinicMedicalAccessLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PetId).IsRequired();
        builder.Property(x => x.ClinicId).IsRequired();
        builder.Property(x => x.AccessedByUserId).IsRequired();
        builder.Property(x => x.Operation).IsRequired().HasMaxLength(80).HasDefaultValue("read_medical_history");
        builder.Property(x => x.Permission).IsRequired().HasMaxLength(40).HasDefaultValue("read");
        builder.Property(x => x.AccessMethod).IsRequired().HasMaxLength(40).HasDefaultValue("unknown");
        builder.Property(x => x.Outcome).IsRequired().HasMaxLength(20).HasDefaultValue("allowed");
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.AccessedAt).IsRequired();

        // Query pattern: find recent access for a specific pet
        builder.HasIndex(x => new { x.PetId, x.AccessedAt });
    }
}
