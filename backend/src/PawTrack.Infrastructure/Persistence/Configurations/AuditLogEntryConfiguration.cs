using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Audit;

namespace PawTrack.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> b)
    {
        b.ToTable("AuditLog");
        b.HasKey(a => a.Id);
        b.Property(a => a.EntityType).HasMaxLength(80).IsRequired();
        b.Property(a => a.EntityId).HasMaxLength(80).IsRequired();
        b.Property(a => a.Details).HasMaxLength(500);
        b.Property(a => a.Action).HasConversion<string>().HasMaxLength(40);
        b.HasIndex(a => a.AdminUserId);
        b.HasIndex(a => new { a.EntityType, a.EntityId });
        b.HasIndex(a => a.PerformedAt);
    }
}
