using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Medical;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class VetReminderConfiguration : IEntityTypeConfiguration<VetReminder>
{
    public void Configure(EntityTypeBuilder<VetReminder> builder)
    {
        builder.ToTable("VetReminders");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PetId).IsRequired();
        builder.Property(x => x.OwnerId).IsRequired();
        builder.Property(x => x.Type).IsRequired().HasConversion<int>();
        builder.Property(x => x.DueDate).IsRequired();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.IsCompleted).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.PetId);
        builder.HasIndex(x => new { x.PetId, x.IsCompleted });
        builder.HasIndex(x => new { x.DueDate, x.IsCompleted });
    }
}
