using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Family;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class FamilyConfiguration
    : IEntityTypeConfiguration<FamilyAccount>,
      IEntityTypeConfiguration<FamilyMembership>,
      IEntityTypeConfiguration<FamilyInvitation>
{
    public void Configure(EntityTypeBuilder<FamilyAccount> builder)
    {
        builder.ToTable("FamilyAccounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OwnerId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.OwnerId).IsUnique();
    }

    public void Configure(EntityTypeBuilder<FamilyMembership> builder)
    {
        builder.ToTable("FamilyMemberships");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FamilyAccountId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Role).IsRequired().HasConversion<int>();
        builder.Property(x => x.JoinedAt).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => new { x.FamilyAccountId, x.UserId }).IsUnique();
        builder.HasIndex(x => x.UserId);
    }

    public void Configure(EntityTypeBuilder<FamilyInvitation> builder)
    {
        builder.ToTable("FamilyInvitations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FamilyAccountId).IsRequired();
        builder.Property(x => x.InvitedEmail).IsRequired().HasMaxLength(254);
        builder.Property(x => x.Token).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.Token).IsUnique();
        builder.HasIndex(x => x.InvitedEmail);
    }
}
