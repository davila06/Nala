using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Medical;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicMedicalAccessGrantConfiguration
    : IEntityTypeConfiguration<ClinicMedicalAccessGrant>
{
    public void Configure(EntityTypeBuilder<ClinicMedicalAccessGrant> builder)
    {
        builder.ToTable("ClinicMedicalAccessGrants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.PetId).IsRequired();
        builder.Property(x => x.ClinicId).IsRequired();
        builder.Property(x => x.PetOwnerId).IsRequired();
        builder.Property(x => x.InitiatedBy).IsRequired().HasMaxLength(10);
        builder.Property(x => x.CodeHash).IsRequired().HasMaxLength(64);
        builder.Property(x => x.CodeExpiresAt).IsRequired();
        builder.Property(x => x.AcceptedAt);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RevokedAt);
        builder.Property(x => x.CreatedAt).IsRequired();

        // Fast lookup for accept flows
        builder.HasIndex(x => x.CodeHash)
            .HasFilter("[AcceptedAt] IS NULL AND [IsActive] = 0");

        // Fast lookup for access checks
        builder.HasIndex(x => new { x.ClinicId, x.PetId, x.IsActive });
        builder.HasIndex(x => x.PetId);
        builder.HasIndex(x => x.ClinicId);
    }
}
