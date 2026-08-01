using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId);
        builder.Property(x => x.ClinicId);
        builder.Property(x => x.ClinicOwnerId);
        builder.Property(x => x.Tier).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.PaymentReference).IsRequired().HasMaxLength(8);
        builder.Property(x => x.AmountCrc).HasColumnType("decimal(12,2)");
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.ActivatedAt);
        builder.Property(x => x.ExpiresAt);
        builder.Property(x => x.CancelledAt);
        builder.Property(x => x.PaymentReportedAt);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ClinicId);
        builder.HasIndex(x => x.PaymentReference).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasIndex(x => new { x.ClinicId, x.Status });
    }
}
