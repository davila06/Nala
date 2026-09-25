using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Clinics;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicClientCommunicationPreferenceConfiguration : IEntityTypeConfiguration<ClinicClientCommunicationPreference>
{
    public void Configure(EntityTypeBuilder<ClinicClientCommunicationPreference> builder)
    {
        builder.ToTable("ClinicClientCommunicationPreferences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Purpose).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.ConsentSource).IsRequired().HasMaxLength(200);
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasIndex(x => new { x.ClinicId, x.PetId, x.Channel, x.Purpose }).IsUnique();
        builder.HasIndex(x => new { x.ClinicId, x.OwnerUserId });
    }
}

public sealed class ClinicClientCommunicationActivityConfiguration : IEntityTypeConfiguration<ClinicClientCommunicationActivity>
{
    public void Configure(EntityTypeBuilder<ClinicClientCommunicationActivity> builder)
    {
        builder.ToTable("ClinicClientCommunicationActivities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Purpose).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Subject).IsRequired().HasMaxLength(160);
        builder.Property(x => x.Body).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.ProviderMessageId).HasMaxLength(120);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.ClinicId, x.CreatedAt });
        builder.HasIndex(x => new { x.ClinicId, x.RequestId }).IsUnique().HasFilter("[RequestId] IS NOT NULL");
        builder.HasIndex(x => new { x.ClinicId, x.PetId });
    }
}

public sealed class ClinicCrmTaskConfiguration : IEntityTypeConfiguration<ClinicCrmTask>
{
    public void Configure(EntityTypeBuilder<ClinicCrmTask> builder)
    {
        builder.ToTable("ClinicCrmTasks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasIndex(x => new { x.ClinicId, x.Status, x.DueDate });
        builder.HasIndex(x => new { x.ClinicId, x.PetId });
    }
}
