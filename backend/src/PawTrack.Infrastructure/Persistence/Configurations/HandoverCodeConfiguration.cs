using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Safety;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class HandoverCodeConfiguration : IEntityTypeConfiguration<HandoverCode>
{
    public void Configure(EntityTypeBuilder<HandoverCode> builder)
    {
        builder.ToTable("HandoverCodes");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.LostPetEventId).IsRequired();

        builder.Property(h => h.Code)
               .HasMaxLength(4)
               .IsRequired();

        builder.Property(h => h.GeneratedAt).IsRequired();
        builder.Property(h => h.ExpiresAt).IsRequired();
        builder.Property(h => h.IsUsed).IsRequired();
        builder.Property(h => h.UsedAt);
        builder.Property(h => h.VerifiedByUserId);

        builder.HasIndex(h => h.LostPetEventId);
    }
}
