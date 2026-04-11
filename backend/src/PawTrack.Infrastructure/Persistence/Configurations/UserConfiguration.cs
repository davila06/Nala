using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Auth;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(254);

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(u => u.IsEmailVerified)
            .IsRequired();

        builder.Property(u => u.EmailVerificationToken)
            .HasMaxLength(64);

        builder.HasIndex(u => u.EmailVerificationToken)
            .IsUnique()
            .HasFilter("[EmailVerificationToken] IS NOT NULL");

        builder.Property(u => u.PasswordResetToken)
            .HasMaxLength(64);

        builder.HasIndex(u => u.PasswordResetToken)
            .IsUnique()
            .HasFilter("[PasswordResetToken] IS NOT NULL");

        builder.Property(u => u.PasswordResetTokenExpiry);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.FailedLoginAttempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(u => u.LockoutEnd);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // NoTracking by default — handlers must call .AsTracking() or dbContext.Update()
        builder.Metadata.SetQueryFilter(null);
    }
}
