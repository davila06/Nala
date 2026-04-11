using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Notifications;

namespace PawTrack.Infrastructure.Persistence.Configurations;

internal sealed class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Endpoint)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(x => x.KeysJson)
            .IsRequired()
            .HasMaxLength(512);

        builder.HasIndex(x => x.Endpoint).IsUnique();
        builder.HasIndex(x => x.UserId);

        builder.ToTable("PushSubscriptions");
    }
}
