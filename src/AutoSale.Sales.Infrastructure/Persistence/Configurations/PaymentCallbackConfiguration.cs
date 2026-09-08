using AutoSale.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoSale.Infrastructure.Persistence.Configurations;

public sealed class PaymentCallbackConfiguration : IEntityTypeConfiguration<PaymentCallback>
{
    public void Configure(EntityTypeBuilder<PaymentCallback> builder)
    {
        builder.ToTable("payment_callbacks");
        builder.HasKey(callback => callback.Id);
        builder.Property(callback => callback.Id).HasColumnName("payment_code").ValueGeneratedNever();
        builder.Ignore(callback => callback.PaymentCode);
        builder.Property(callback => callback.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(callback => callback.Outcome).HasColumnName("outcome").HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(callback => callback.OccurredAtUtc).HasColumnName("occurred_at_utc").IsRequired();
        builder.Property(callback => callback.ReceivedAtUtc).HasColumnName("received_at_utc").IsRequired();
        builder.HasIndex(callback => callback.EventId).IsUnique().HasDatabaseName("ux_payment_callbacks_event_id");
    }
}
