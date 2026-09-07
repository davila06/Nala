using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.AnimalWelfare;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class AnimalWelfareCaseNoteConfiguration : IEntityTypeConfiguration<AnimalWelfareCaseNote>
{
    public void Configure(EntityTypeBuilder<AnimalWelfareCaseNote> builder)
    {
        builder.ToTable("AnimalWelfareCaseNotes");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.Body).IsRequired().HasMaxLength(2000);
        builder.HasIndex(n => new { n.CaseId, n.CreatedAt });
    }
}

public sealed class AnimalWelfareReferralConfiguration : IEntityTypeConfiguration<AnimalWelfareReferral>
{
    public void Configure(EntityTypeBuilder<AnimalWelfareReferral> builder)
    {
        builder.ToTable("AnimalWelfareReferrals");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Destination).IsRequired().HasMaxLength(120);
        builder.Property(r => r.Reason).IsRequired().HasMaxLength(1000);
        builder.HasIndex(r => new { r.CaseId, r.ReferredAt });
    }
}

public sealed class AnimalWelfareCaseAuditLogConfiguration : IEntityTypeConfiguration<AnimalWelfareCaseAuditLog>
{
    public void Configure(EntityTypeBuilder<AnimalWelfareCaseAuditLog> builder)
    {
        builder.ToTable("AnimalWelfareCaseAuditLogs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Property(l => l.Action).IsRequired().HasConversion<string>().HasMaxLength(40);
        builder.Property(l => l.Details).HasMaxLength(1000);
        builder.HasIndex(l => new { l.CaseId, l.CreatedAt });
        builder.HasIndex(l => l.Action);
    }
}
