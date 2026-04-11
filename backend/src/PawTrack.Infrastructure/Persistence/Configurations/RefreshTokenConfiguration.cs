using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Auth;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id).ValueGeneratedNever();

        builder.Property(rt => rt.TokenHash)
            .IsRequired()
            .HasMaxLength(64); // SHA-256 hex = 64 chars

        builder.HasIndex(rt => rt.TokenHash).IsUnique();

        // Explicit index on UserId for O(log n) theft-detection and token-rotation queries.
        // Without this, LoadUserWithRefreshTokensAsync (via navigation property) and
        // RevokeAllRefreshTokens() perform a full table scan as the RefreshTokens table grows.
        // Declared explicitly (not relying on EF FK convention) to protect against convention
        // changes and to assign a stable, canonical database index name.
        builder.HasIndex(rt => rt.UserId).HasDatabaseName("IX_RefreshTokens_UserId");

        builder.Property(rt => rt.ExpiresAt).IsRequired();
        builder.Property(rt => rt.IsRevoked).IsRequired();
        builder.Property(rt => rt.CreatedAt).IsRequired();

        // Tracks when the original session was established (first login).
        // Preserved across every token rotation in the same chain so the handler
        // can enforce an absolute 90-day session ceiling.
        builder.Property(rt => rt.SessionIssuedAt).IsRequired();
    }
}
