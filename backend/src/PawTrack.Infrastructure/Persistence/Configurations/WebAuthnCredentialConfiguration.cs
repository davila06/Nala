using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Auth;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class WebAuthnCredentialConfiguration : IEntityTypeConfiguration<WebAuthnCredential>
{
    public void Configure(EntityTypeBuilder<WebAuthnCredential> builder)
    {
        builder.ToTable("WebAuthnCredentials");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CredentialId).IsRequired().HasMaxLength(1024);
        builder.Property(x => x.PublicKey).IsRequired().HasMaxLength(4096);
        builder.Property(x => x.DeviceName).HasMaxLength(120);
        builder.HasIndex(x => x.CredentialId).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.RevokedAt });
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}