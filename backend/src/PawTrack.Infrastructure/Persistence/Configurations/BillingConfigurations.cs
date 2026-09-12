using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Payments;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class UserBillingProfileConfiguration : IEntityTypeConfiguration<UserBillingProfile>
{
    public void Configure(EntityTypeBuilder<UserBillingProfile> builder)
    {
        builder.ToTable("UserBillingProfiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.IdentificationType).IsRequired().HasConversion<int>();
        builder.Property(x => x.IdentificationNumber).IsRequired().HasMaxLength(30);
        builder.Property(x => x.LegalName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.BillingEmail).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Province).HasMaxLength(50);
        builder.Property(x => x.Canton).HasMaxLength(50);
        builder.Property(x => x.District).HasMaxLength(50);
        builder.Property(x => x.AddressDetails).HasMaxLength(300);
        builder.Property(x => x.PhoneNumber).HasMaxLength(30);
        builder.Property(x => x.RequiresInvoice).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);

        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => x.IdentificationNumber);
    }
}

public sealed class ElectronicInvoiceConfiguration : IEntityTypeConfiguration<ElectronicInvoice>
{
    public void Configure(EntityTypeBuilder<ElectronicInvoice> builder)
    {
        builder.ToTable("ElectronicInvoices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.TransactionId);
        builder.Property(x => x.DocumentType).IsRequired().HasConversion<int>();
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.ClaveNumerica).IsRequired().HasMaxLength(50);
        builder.Property(x => x.NumeroConsecutivo).IsRequired().HasMaxLength(20);
        builder.Property(x => x.CodigoCabys).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ServiceDescription).IsRequired().HasMaxLength(300);
        builder.Property(x => x.SubtotalCrc).HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(x => x.IvaRate).HasColumnType("decimal(5,4)").IsRequired();
        builder.Property(x => x.IvaAmountCrc).HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(x => x.TotalAmountCrc).HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(x => x.PaymentMethodCode).IsRequired().HasMaxLength(5);

        builder.Property(x => x.ReceiverIdType).HasConversion<int>();
        builder.Property(x => x.ReceiverIdNumber).HasMaxLength(30);
        builder.Property(x => x.ReceiverName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ReceiverEmail).IsRequired().HasMaxLength(200);

        builder.Property(x => x.SignedXmlUrl).HasMaxLength(500);
        builder.Property(x => x.HaciendaResponseXmlUrl).HasMaxLength(500);
        builder.Property(x => x.PdfRepresentationUrl).HasMaxLength(500);
        builder.Property(x => x.HaciendaResponseStatus).HasMaxLength(50);
        builder.Property(x => x.HaciendaErrorMessage).HasMaxLength(500);

        builder.Property(x => x.IssuedAt).IsRequired();
        builder.Property(x => x.ProcessedByHaciendaAt);

        builder.HasIndex(x => x.ClaveNumerica).IsUnique();
        builder.HasIndex(x => x.NumeroConsecutivo).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Status);
    }
}
