using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.LostPets;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class LostPetEventConfiguration : IEntityTypeConfiguration<LostPetEvent>
{
    public void Configure(EntityTypeBuilder<LostPetEvent> builder)
    {
        builder.ToTable("LostPetEvents");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.PetId).IsRequired();
        builder.Property(e => e.OwnerId).IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.PublicMessage).HasMaxLength(200);
        builder.Property(e => e.RecentPhotoUrl).HasMaxLength(2000);

        // Contact info — ContactPhone is PII: NOT surfaced in public endpoints
        builder.Property(e => e.ContactName).HasMaxLength(100);
        builder.Property(e => e.ContactPhone).HasMaxLength(30);

        // Reward — declared publicly by the owner; safe to surface in all endpoints
        builder.Property(e => e.RewardAmount).HasColumnType("decimal(12,2)");
        builder.Property(e => e.RewardNote).HasMaxLength(150);

        builder.Property(e => e.ReportedAt).IsRequired();
        builder.Property(e => e.LastSeenAt).IsRequired();
        builder.Property(e => e.ResolvedAt);
        builder.Property(e => e.ReunionLat);
        builder.Property(e => e.ReunionLng);
        builder.Property(e => e.RecoveryDistanceMeters);
        builder.Property(e => e.RecoveryTime);
        builder.Property(e => e.CantonName).HasMaxLength(120);

        builder.HasIndex(e => e.PetId);
        builder.HasIndex(e => e.OwnerId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.CantonName);

        // Domain events are transient — never persisted
        builder.Ignore(e => e.DomainEvents);
    }
}
