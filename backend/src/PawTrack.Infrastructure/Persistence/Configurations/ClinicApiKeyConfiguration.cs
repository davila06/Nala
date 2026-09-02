using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Clinics;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicApiKeyConfiguration : IEntityTypeConfiguration<ClinicApiKey>
{
    public void Configure(EntityTypeBuilder<ClinicApiKey> builder)
    {
        builder.ToTable("ClinicApiKeys");
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Id).ValueGeneratedNever();

        builder.Property(k => k.ClinicId).IsRequired();
        builder.Property(k => k.KeyHash).IsRequired().HasMaxLength(64);
        builder.Property(k => k.Label).IsRequired().HasMaxLength(100);
        builder.Property(k => k.IsRevoked).IsRequired().HasDefaultValue(false);
        builder.Property(k => k.CreatedAt).IsRequired();
        // No DB-level default: SQL Server can't default a column from another column.
        // New rows always get ExpiresAt from ClinicApiKey.Create(); pre-existing rows are
        // backfilled once by the AddClinicApiKeyExpirationAndRotation migration.
        builder.Property(k => k.ExpiresAt).IsRequired();
        builder.Property(k => k.RotatedToKeyId);

        // partial index — only non-revoked hashes need fast lookup
        builder.HasIndex(k => k.KeyHash)
            .HasFilter("[IsRevoked] = 0")
            .IsUnique();
        builder.HasIndex(k => k.ClinicId);
    }
}
