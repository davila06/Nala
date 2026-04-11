using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Notifications;

namespace PawTrack.Infrastructure.Persistence.Configurations;

internal sealed class UserNotificationPreferencesConfiguration
    : IEntityTypeConfiguration<UserNotificationPreferences>
{
    public void Configure(EntityTypeBuilder<UserNotificationPreferences> builder)
    {
        builder.ToTable("UserNotificationPreferences");

        // One row per user — UserId is both PK and the link to Auth.Users
        builder.HasKey(p => p.UserId);
        builder.Property(p => p.UserId).ValueGeneratedNever();

        builder.Property(p => p.EnablePreventiveAlerts).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();
    }
}
