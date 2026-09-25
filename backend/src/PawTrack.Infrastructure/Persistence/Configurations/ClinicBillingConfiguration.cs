using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Clinics;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicSaleConfiguration : IEntityTypeConfiguration<ClinicSale>
{
    public void Configure(EntityTypeBuilder<ClinicSale> builder)
    {
        builder.ToTable("ClinicSales");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ReceiptNumber).IsRequired().HasMaxLength(40);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.DiscountCrc).HasColumnType("decimal(12,2)");
        builder.Property(x => x.DiscountReason).HasMaxLength(300);
        builder.Property(x => x.VoidReason).HasMaxLength(300);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.ClinicId, x.ReceiptNumber }).IsUnique();
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Payments).WithOne().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ClinicSaleLineConfiguration : IEntityTypeConfiguration<ClinicSaleLine>
{
    public void Configure(EntityTypeBuilder<ClinicSaleLine> builder)
    {
        builder.ToTable("ClinicSaleLines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Description).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.UnitPriceCrc).HasColumnType("decimal(12,2)");
        builder.Property(x => x.LineTotalCrc).HasColumnType("decimal(12,2)");
        builder.HasIndex(x => x.SaleId);
    }
}

public sealed class ClinicSalePaymentConfiguration : IEntityTypeConfiguration<ClinicSalePayment>
{
    public void Configure(EntityTypeBuilder<ClinicSalePayment> builder)
    {
        builder.ToTable("ClinicSalePayments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.AmountCrc).HasColumnType("decimal(12,2)");
        builder.Property(x => x.Method).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(100);
        builder.Property(x => x.ReceivedAt).IsRequired();
        builder.HasIndex(x => new { x.ClinicId, x.ReceivedAt });
    }
}

public sealed class ClinicCashCloseConfiguration : IEntityTypeConfiguration<ClinicCashClose>
{
    public void Configure(EntityTypeBuilder<ClinicCashClose> builder)
    {
        builder.ToTable("ClinicCashCloses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TotalCrc).HasColumnType("decimal(12,2)");
        builder.Property(x => x.ClosedAt).IsRequired();
        builder.OwnsMany(x => x.PaymentsByMethod, payments =>
        {
            payments.ToTable("ClinicCashClosePaymentSnapshots");
            payments.WithOwner().HasForeignKey("CashCloseId");
            payments.Property<Guid>("Id");
            payments.HasKey("Id");
            payments.Property(x => x.Method).HasConversion<string>().HasMaxLength(30).IsRequired();
            payments.Property(x => x.AmountCrc).HasColumnType("decimal(12,2)");
        });
        builder.HasIndex(x => new { x.ClinicId, x.BusinessDate }).IsUnique();
    }
}
