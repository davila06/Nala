using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Bounties;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class BountyConfiguration : IEntityTypeConfiguration<Bounty>
{
    public void Configure(EntityTypeBuilder<Bounty> builder)
    {
        builder.ToTable("Bounties");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.LostPetEventId).IsRequired();
        builder.Property(x => x.OwnerId).IsRequired();
        builder.Property(x => x.Amount).HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.DepositReference).IsRequired().HasMaxLength(8);
        builder.Property(x => x.PlatformFee).HasColumnType("decimal(5,4)").IsRequired();
        builder.Property(x => x.ClaimedBySightingId);
        builder.Property(x => x.ClaimedByUserId);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.DepositedAt);
        builder.Property(x => x.ClaimedAt);
        builder.Property(x => x.ReleasedAt);

        builder.Ignore(x => x.NetPayoutAmount); // computed property

        builder.HasIndex(x => x.LostPetEventId);
        builder.HasIndex(x => x.DepositReference).IsUnique();
        builder.HasIndex(x => new { x.LostPetEventId, x.Status });
    }
}
