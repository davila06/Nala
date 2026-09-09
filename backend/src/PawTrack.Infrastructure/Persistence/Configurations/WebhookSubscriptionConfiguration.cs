using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Webhooks;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("WebhookSubscriptions"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.EndpointUrl).IsRequired().HasMaxLength(500); builder.Property(x => x.SecretProtected).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.EventTypesJson).IsRequired().HasMaxLength(2000); builder.HasIndex(x => new { x.OwnerUserId, x.IsActive });
    }
}