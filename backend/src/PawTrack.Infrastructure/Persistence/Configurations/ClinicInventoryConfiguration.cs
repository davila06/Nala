using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Clinics;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicInventoryItemConfiguration : IEntityTypeConfiguration<ClinicInventoryItem>
{
    public void Configure(EntityTypeBuilder<ClinicInventoryItem> builder)
    {
        builder.ToTable("ClinicInventoryItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(160);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Unit).IsRequired().HasMaxLength(40);
        builder.Property(x => x.MinimumStock).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.ClinicId, x.Type, x.Name });
    }
}

public sealed class ClinicInventoryLotConfiguration : IEntityTypeConfiguration<ClinicInventoryLot>
{
    public void Configure(EntityTypeBuilder<ClinicInventoryLot> builder)
    {
        builder.ToTable("ClinicInventoryLots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.LotNumber).IsRequired().HasMaxLength(100);
        builder.Property(x => x.InitialQuantity).IsRequired();
        builder.Property(x => x.AvailableQuantity).IsRequired();
        builder.Property(x => x.UnitCostCrc).HasColumnType("decimal(12,2)");
        builder.Property(x => x.SupplierName).HasMaxLength(200);
        builder.Property(x => x.LocationName).IsRequired().HasMaxLength(120);
        builder.Property(x => x.ReceivedAt).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.ClinicId, x.ItemId, x.LocationName, x.LotNumber });
        builder.HasIndex(x => new { x.ItemId, x.AvailableQuantity, x.ExpiresAt });
    }
}

public sealed class ClinicInventoryMovementConfiguration : IEntityTypeConfiguration<ClinicInventoryMovement>
{
    public void Configure(EntityTypeBuilder<ClinicInventoryMovement> builder)
    {
        builder.ToTable("ClinicInventoryMovements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.QuantityDelta).IsRequired();
        builder.Property(x => x.Reason).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.ClinicId, x.ItemId, x.CreatedAt });
        builder.HasIndex(x => x.ConsultationId).HasFilter("[ConsultationId] IS NOT NULL");
        builder.HasIndex(x => x.CertificateId).HasFilter("[CertificateId] IS NOT NULL");
    }
}
