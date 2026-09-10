using AutoSale.Payments.Mock.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoSale.Payments.Mock.Persistence;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(x => x.PaymentCode);
        builder.Property(x => x.PaymentCode).HasColumnName("payment_code");
        builder.Property(x => x.SaleId).HasColumnName("sale_id");
        builder.HasIndex(x => x.SaleId).IsUnique();
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(14, 2);
        builder.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.EventId).HasColumnName("event_id");
        builder.HasIndex(x => x.EventId).IsUnique();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasConversion<long>();
        builder.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc").HasConversion<long?>();
        builder.Property(x => x.CallbackDeliveredAtUtc).HasColumnName("callback_delivered_at_utc").HasConversion<long?>();
        builder.Property(x => x.Attempts).HasColumnName("attempts");
        builder.Property(x => x.NextAttemptAtUtc).HasColumnName("next_attempt_at_utc").HasConversion<long?>();
        builder.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(500);
        builder.Property(x => x.LeaseOwner).HasColumnName("lease_owner").HasMaxLength(128);
        builder.Property(x => x.LeaseExpiresAtUtc).HasColumnName("lease_expires_at_utc").HasConversion<long?>();
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        builder.HasIndex(x => new { x.CallbackDeliveredAtUtc, x.NextAttemptAtUtc });
    }
}
