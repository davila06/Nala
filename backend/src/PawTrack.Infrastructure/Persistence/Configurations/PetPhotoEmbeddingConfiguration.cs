using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Pets;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class PetPhotoEmbeddingConfiguration : IEntityTypeConfiguration<PetPhotoEmbedding>
{
    public void Configure(EntityTypeBuilder<PetPhotoEmbedding> builder)
    {
        builder.ToTable("PetPhotoEmbeddings");

        builder.HasKey(e => e.PetId);
        builder.Property(e => e.PetId).ValueGeneratedNever();

        // Azure CV 4.0 produces 1024-d float vectors; JSON ≈ 6 KB per row.
        builder.Property(e => e.EmbeddingJson)
               .IsRequired()
               .HasColumnType("nvarchar(max)");

        // SHA-256 hex string: 64 characters.
        builder.Property(e => e.PhotoUrlHash)
               .IsRequired()
               .HasMaxLength(64);

        builder.Property(e => e.GeneratedAt).IsRequired();
    }
}
