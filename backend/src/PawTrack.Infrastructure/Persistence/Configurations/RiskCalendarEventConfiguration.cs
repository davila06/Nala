using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Notifications;

namespace PawTrack.Infrastructure.Persistence.Configurations;

internal sealed class RiskCalendarEventConfiguration : IEntityTypeConfiguration<RiskCalendarEvent>
{
    public void Configure(EntityTypeBuilder<RiskCalendarEvent> builder)
    {
        builder.ToTable("RiskCalendarEvents");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Month).IsRequired();
        builder.Property(e => e.Day).IsRequired();
        builder.Property(e => e.DaysBeforeAlert).IsRequired();
        builder.Property(e => e.MessageTemplate).IsRequired().HasMaxLength(300);
        builder.Property(e => e.CantonFilter).HasMaxLength(100);
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => new { e.Month, e.Day }).HasDatabaseName("IX_RiskCalendarEvents_MonthDay");
        builder.HasIndex(e => e.IsActive).HasDatabaseName("IX_RiskCalendarEvents_IsActive");
    }
}
