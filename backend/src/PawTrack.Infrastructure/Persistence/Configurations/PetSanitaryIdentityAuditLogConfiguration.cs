using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Pets;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class PetSanitaryIdentityAuditLogConfiguration : IEntityTypeConfiguration<PetSanitaryIdentityAuditLog>
{
    public void Configure(EntityTypeBuilder<PetSanitaryIdentityAuditLog> builder)
    {
        builder.ToTable("PetSanitaryIdentityAuditLogs");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Id).ValueGeneratedNever();
        builder.Property(log => log.PetId).IsRequired();
        builder.Property(log => log.ActorUserId).IsRequired();
        builder.Property(log => log.ActorClinicId);
        builder.Property(log => log.Action).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(log => log.FieldName).IsRequired().HasMaxLength(80);
        builder.Property(log => log.PreviousValue).HasMaxLength(300);
        builder.Property(log => log.NewValue).HasMaxLength(300);
        builder.Property(log => log.Reason).HasMaxLength(300);
        builder.Property(log => log.CreatedAt).IsRequired();

        builder.HasIndex(log => new { log.PetId, log.CreatedAt });
        builder.HasIndex(log => log.ActorUserId);
        builder.HasIndex(log => log.ActorClinicId);
        builder.HasIndex(log => log.Action);
    }
}
