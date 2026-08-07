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
        builder.Property(x => x.AccessedAt).IsRequired();

        // Query pattern: find recent access for a specific pet
        builder.HasIndex(x => new { x.PetId, x.AccessedAt });
    }
}
