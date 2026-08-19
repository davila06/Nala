using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Auth;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class RevokedTokenConfiguration : IEntityTypeConfiguration<RevokedToken>
{
    public void Configure(EntityTypeBuilder<RevokedToken> builder)
    {
        builder.ToTable("RevokedTokens");
        builder.HasKey(x => x.Jti);
        builder.Property(x => x.Jti).IsRequired().HasMaxLength(256);
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.RevokedAt).IsRequired();
        // Expiry index: used by cleanup job and to quickly skip expired entries
        builder.HasIndex(x => x.ExpiresAt);
    }
}
