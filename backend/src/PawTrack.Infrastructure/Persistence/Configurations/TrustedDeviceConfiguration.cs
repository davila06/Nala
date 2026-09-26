using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Auth;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class TrustedDeviceConfiguration : IEntityTypeConfiguration<TrustedDevice>
{
    public void Configure(EntityTypeBuilder<TrustedDevice> builder)
    {
        builder.ToTable("TrustedDevices");
        builder.HasKey(device => device.Id);
        builder.Property(device => device.Id).ValueGeneratedNever();
        builder.Property(device => device.DeviceName).IsRequired().HasMaxLength(100);
        builder.Property(device => device.TokenHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(device => device.TokenHash).IsUnique();
        builder.HasIndex(device => new { device.UserId, device.RevokedAt, device.ExpiresAt })
            .HasDatabaseName("IX_TrustedDevices_UserId_RevokedAt_ExpiresAt");
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(device => device.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Property(device => device.CreatedAt).IsRequired();
        builder.Property(device => device.LastUsedAt).IsRequired();
        builder.Property(device => device.ExpiresAt).IsRequired();
    }
}
