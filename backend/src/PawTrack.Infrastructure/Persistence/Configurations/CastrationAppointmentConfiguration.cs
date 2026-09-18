using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.CastrationCampaigns;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class CastrationAppointmentConfiguration : IEntityTypeConfiguration<CastrationAppointment>
{
    public void Configure(EntityTypeBuilder<CastrationAppointment> builder)
    {
        builder.ToTable("CastrationAppointments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.EligibilitySnapshot).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(x => x.ConsentVersion).IsRequired().HasMaxLength(40);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.BasePriceCrc).HasColumnType("decimal(12,2)");
        builder.Property(x => x.IvaAmountCrc).HasColumnType("decimal(12,2)");
        builder.Property(x => x.TotalAmountCrc).HasColumnType("decimal(12,2)");
        builder.Property(x => x.ClinicalOutcome).HasMaxLength(1_000);
        builder.Property(x => x.PostOperativeInstructions).HasMaxLength(2_000);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne<CastrationCampaign>()
            .WithMany()
            .HasForeignKey(x => x.CampaignId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CampaignId, x.PetId }).IsUnique();
        builder.HasIndex(x => new { x.CampaignId, x.Status, x.ScheduledAt });
        builder.HasIndex(x => new { x.OwnerUserId, x.Status, x.ScheduledAt });
    }
}
