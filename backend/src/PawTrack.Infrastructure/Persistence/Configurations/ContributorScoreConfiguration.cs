using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Incentives;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ContributorScoreConfiguration : IEntityTypeConfiguration<ContributorScore>
{
    public void Configure(EntityTypeBuilder<ContributorScore> builder)
    {
        builder.ToTable("ContributorScores");

        // UserId is the PK — one row per user
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.UserId)
            .ValueGeneratedNever();

        builder.Property(x => x.OwnerName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ReunificationCount)
            .IsRequired();

        builder.Property(x => x.Badge)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.TotalPoints)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Leaderboard query orders by ReunificationCount desc
        builder.HasIndex(x => x.ReunificationCount);
    }
}
