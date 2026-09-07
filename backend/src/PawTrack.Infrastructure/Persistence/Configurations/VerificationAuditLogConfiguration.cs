using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Certificates;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class VerificationAuditLogConfiguration : IEntityTypeConfiguration<VerificationAuditLog>
{
    public void Configure(EntityTypeBuilder<VerificationAuditLog> builder)
    {
        builder.ToTable("VerificationAuditLogs");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Id).ValueGeneratedNever();
        builder.Property(log => log.EntityType).IsRequired().HasMaxLength(80);
        builder.Property(log => log.EntityId).IsRequired();
        builder.Property(log => log.Action).IsRequired().HasConversion<string>().HasMaxLength(60);
        builder.Property(log => log.ActorUserId);
        builder.Property(log => log.Details).HasMaxLength(500);
        builder.Property(log => log.CreatedAt).IsRequired();

        builder.HasIndex(log => new { log.EntityType, log.EntityId, log.CreatedAt });
        builder.HasIndex(log => log.Action);
        builder.HasIndex(log => log.ActorUserId);
    }
}
