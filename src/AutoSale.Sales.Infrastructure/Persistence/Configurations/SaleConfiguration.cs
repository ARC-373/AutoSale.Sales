using AutoSale.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoSale.Infrastructure.Persistence.Configurations;

public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("sales");
        builder.HasKey(sale => sale.Id);
        builder.Property(sale => sale.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(sale => sale.VehicleId).HasColumnName("vehicle_id").IsRequired();
        builder.Property(sale => sale.BuyerSubject).HasColumnName("buyer_subject").HasMaxLength(128).IsRequired();
        builder.Property(sale => sale.ExpectedPrice).HasColumnName("expected_price").HasPrecision(14, 2).IsRequired();
        builder.Property(sale => sale.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(100).IsRequired();
        builder.Property(sale => sale.RequestHash).HasColumnName("request_hash").HasMaxLength(256).IsRequired();
        builder.Property(sale => sale.PaymentCode).HasColumnName("payment_code").IsRequired();
        builder.Property(sale => sale.SalePrice).HasColumnName("sale_price").HasPrecision(14, 2);
        builder.Property(sale => sale.State).HasColumnName("state").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(sale => sale.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(sale => sale.PaymentRegisteredAtUtc).HasColumnName("payment_registered_at_utc");
        builder.Property(sale => sale.PaymentOccurredAtUtc).HasColumnName("payment_occurred_at_utc");
        builder.Property(sale => sale.CompletedAtUtc).HasColumnName("completed_at_utc");
        builder.Property(sale => sale.CancelledAtUtc).HasColumnName("cancelled_at_utc");
        builder.Property(sale => sale.FailureCode).HasColumnName("failure_code").HasMaxLength(100);
        builder.Property(sale => sale.Version).HasColumnName("version").IsConcurrencyToken().IsRequired();
        builder.Property(sale => sale.Attempts).HasColumnName("attempts").IsRequired();
        builder.Property(sale => sale.NextAttemptAtUtc).HasColumnName("next_attempt_at_utc");
        builder.Property(sale => sale.LastError).HasColumnName("last_error").HasMaxLength(2_000);
        builder.Property(sale => sale.LeaseOwner).HasColumnName("lease_owner").HasMaxLength(128);
        builder.Property(sale => sale.LeaseExpiresAtUtc).HasColumnName("lease_expires_at_utc");

        builder.OwnsOne(sale => sale.BuyerCpf, cpf =>
        {
            cpf.Property(value => value.Value).HasColumnName("buyer_cpf").HasMaxLength(11).IsRequired();
        });
        builder.Navigation(sale => sale.BuyerCpf).IsRequired();

        builder.OwnsOne(sale => sale.VehicleSnapshot, snapshot =>
        {
            snapshot.Property(value => value.Id).HasColumnName("snapshot_vehicle_id");
            snapshot.Property(value => value.Make).HasColumnName("snapshot_make").HasMaxLength(120);
            snapshot.Property(value => value.Model).HasColumnName("snapshot_model").HasMaxLength(120);
            snapshot.Property(value => value.Year).HasColumnName("snapshot_year");
            snapshot.Property(value => value.Color).HasColumnName("snapshot_color").HasMaxLength(50);
            snapshot.Property(value => value.Price).HasColumnName("snapshot_price").HasPrecision(14, 2);
            snapshot.Property(value => value.Status).HasColumnName("snapshot_status").HasConversion<string>().HasMaxLength(16);
            snapshot.Property(value => value.Version).HasColumnName("snapshot_version");
            snapshot.Property(value => value.UpdatedAtUtc).HasColumnName("snapshot_updated_at_utc");
        });

        builder.HasIndex(sale => sale.PaymentCode).IsUnique().HasDatabaseName("ux_sales_payment_code");
        builder.HasIndex(sale => new { sale.BuyerSubject, sale.IdempotencyKey })
            .IsUnique().HasDatabaseName("ux_sales_buyer_idempotency");
        builder.HasIndex(sale => sale.VehicleId).IsUnique()
            .HasFilter("\"state\" NOT IN ('Cancelled', 'Rejected')")
            .HasDatabaseName("ux_sales_active_vehicle");
        builder.HasIndex(sale => new { sale.SalePrice, sale.Id })
            .HasFilter("\"state\" = 'Completed'")
            .HasDatabaseName("ix_sales_completed_price_id");
        builder.HasIndex(sale => new { sale.State, sale.NextAttemptAtUtc, sale.LeaseExpiresAtUtc })
            .HasDatabaseName("ix_sales_pending_claim");
    }
}
