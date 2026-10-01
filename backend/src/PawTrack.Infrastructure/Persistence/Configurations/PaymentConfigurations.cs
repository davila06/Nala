using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Payments;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class UserPaymentProfileConfiguration : IEntityTypeConfiguration<UserPaymentProfile>
{
    public void Configure(EntityTypeBuilder<UserPaymentProfile> builder)
    {
        builder.ToTable("UserPaymentProfiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.MethodType).IsRequired().HasConversion<int>();
        builder.Property(x => x.ProviderToken).IsRequired().HasMaxLength(256);
        builder.Property(x => x.CardBrand).HasMaxLength(50);
        builder.Property(x => x.LastFourDigits).HasMaxLength(4);
        builder.Property(x => x.CardholderName).HasMaxLength(150);
        builder.Property(x => x.IsDefault).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.LastUsedAt);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.IsDefault });
    }
}

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.PaymentProfileId);
        builder.Property(x => x.AmountCrc).HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(x => x.GrossAmountCrc).HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(x => x.ProrationCreditCrc).HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(10);
        builder.Property(x => x.TransactionReference).IsRequired().HasMaxLength(100);
        builder.Property(x => x.GatewayAuthorizationCode).HasMaxLength(100);
        builder.Property(x => x.Purpose).IsRequired().HasMaxLength(50);
        builder.Property(x => x.TargetEntityId);
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.FailureReason).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CompletedAt);

        builder.HasIndex(x => x.TransactionReference).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.Purpose, x.TargetEntityId });
    }
}

public sealed class PaymentIntentConfiguration : IEntityTypeConfiguration<PaymentIntent>
{
    public void Configure(EntityTypeBuilder<PaymentIntent> builder)
    {
        builder.ToTable("PaymentIntents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.AmountCrc).HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(10);
        builder.Property(x => x.MerchantReference).IsRequired().HasMaxLength(100);
        builder.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Purpose).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.GatewayTransactionId).HasMaxLength(150);
        builder.Property(x => x.AuthorizationCode).HasMaxLength(100);
        builder.Property(x => x.FailureReason).HasMaxLength(500);
        builder.Property(x => x.CapturedAmountCrc).HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(x => x.RefundedAmountCrc).HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.IdempotencyKey }).IsUnique();
        builder.HasIndex(x => x.MerchantReference).IsUnique();
        builder.HasIndex(x => new { x.Status, x.UpdatedAt });
    }
}

public sealed class PaymentOperationConfiguration : IEntityTypeConfiguration<PaymentOperation>
{
    public void Configure(EntityTypeBuilder<PaymentOperation> builder)
    {
        builder.ToTable("PaymentOperations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.PaymentIntentId);
        builder.Property(x => x.OperationType).IsRequired().HasConversion<int>();
        builder.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(200);
        builder.Property(x => x.RequestHash).IsRequired().HasMaxLength(128);
        builder.Property(x => x.ProviderOperationId).HasMaxLength(200);
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.ResponseJson).HasMaxLength(8000);
        builder.Property(x => x.FailureReason).HasMaxLength(1000);
        builder.Property(x => x.CorrelationId).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.CompletedAt);

        builder.HasOne<PaymentIntent>()
            .WithMany()
            .HasForeignKey(x => x.PaymentIntentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.OperationType, x.IdempotencyKey }).IsUnique();
        builder.HasIndex(x => new { x.OperationType, x.ProviderOperationId })
            .IsUnique()
            .HasFilter("[ProviderOperationId] IS NOT NULL");
        builder.HasIndex(x => new { x.PaymentIntentId, x.OperationType, x.CreatedAt });
        builder.HasIndex(x => new { x.OperationType, x.Status, x.CreatedAt });
    }
}

public sealed class PaymentLedgerEntryConfiguration : IEntityTypeConfiguration<PaymentLedgerEntry>
{
    public void Configure(EntityTypeBuilder<PaymentLedgerEntry> builder)
    {
        builder.ToTable("PaymentLedgerEntries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.PaymentIntentId).IsRequired();
        builder.Property(x => x.PaymentOperationId).IsRequired();
        builder.Property(x => x.EntryType).IsRequired().HasConversion<int>();
        builder.Property(x => x.AmountCrc).HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Reference).IsRequired().HasMaxLength(200);
        builder.Property(x => x.CorrelationId).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.PaymentOperationId).IsUnique();
        builder.HasIndex(x => new { x.PaymentIntentId, x.CreatedAt });
    }
}
