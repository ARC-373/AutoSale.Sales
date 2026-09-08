using AutoSale.Domain.Buyers;
using AutoSale.Domain.Payments;
using AutoSale.Domain.Sales;

namespace AutoSale.Sales.Domain.UnitTests.Sales;

public sealed class SaleTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_StartsReservingWithoutPriceOrSnapshot()
    {
        var sale = CreateSale();

        Assert.Equal(SaleStatus.Reserving, sale.State);
        Assert.Null(sale.VehicleSnapshot);
        Assert.Null(sale.SalePrice);
        Assert.NotEqual(Guid.Empty, sale.PaymentCode);
        Assert.Equal(1, sale.Version);
        Assert.False(sale.IsTerminal);
    }

    [Fact]
    public void PaidFlow_ReachesCompletedWithReservationPriceAndDates()
    {
        var sale = CreateSale();
        var snapshot = CreateSnapshot(sale.VehicleId, VehicleStatus.Reserved, 75_500.90m, 3);

        Assert.True(sale.AcceptReservation(snapshot).IsSuccess);
        Assert.True(sale.RegisterPayment(CreatedAt.AddMinutes(1)).IsSuccess);
        Assert.True(sale.RecordPayment(PaymentOutcome.Paid, CreatedAt.AddMinutes(2)).IsSuccess);
        Assert.True(sale.Complete(CreatedAt.AddMinutes(3)).IsSuccess);

        Assert.Equal(SaleStatus.Completed, sale.State);
        Assert.Equal(75_500.90m, sale.SalePrice);
        Assert.Equal(snapshot, sale.VehicleSnapshot);
        Assert.Equal(CreatedAt.AddMinutes(1), sale.PaymentRegisteredAtUtc);
        Assert.Equal(CreatedAt.AddMinutes(2), sale.PaymentOccurredAtUtc);
        Assert.Equal(CreatedAt.AddMinutes(3), sale.CompletedAtUtc);
        Assert.True(sale.IsTerminal);
    }

    [Fact]
    public void CancelledFlow_ReachesCancelledOnlyAfterVehicleRelease()
    {
        var sale = CreateSale();
        Assert.True(sale.AcceptReservation(CreateSnapshot(sale.VehicleId, VehicleStatus.Reserved)).IsSuccess);
        Assert.True(sale.RecordPayment(PaymentOutcome.Cancelled, CreatedAt.AddMinutes(1)).IsSuccess);

        Assert.Equal(SaleStatus.CancellingVehicle, sale.State);
        Assert.Null(sale.CancelledAtUtc);

        Assert.True(sale.Cancel(CreatedAt.AddMinutes(2)).IsSuccess);
        Assert.Equal(SaleStatus.Cancelled, sale.State);
        Assert.Equal(CreatedAt.AddMinutes(2), sale.CancelledAtUtc);
    }

    [Fact]
    public void TerminalSale_RejectsFurtherChanges()
    {
        var sale = CreateSale();
        Assert.True(sale.RejectReservation("vehicle_unavailable").IsSuccess);

        var result = sale.AcceptReservation(CreateSnapshot(sale.VehicleId, VehicleStatus.Reserved));

        Assert.True(result.IsFailure);
        Assert.Equal(SaleErrors.TerminalSaleCannotChange, result.Error);
    }

    [Fact]
    public void AcceptReservation_RejectsSnapshotForAnotherVehicle()
    {
        var sale = CreateSale();

        var result = sale.AcceptReservation(CreateSnapshot(Guid.NewGuid(), VehicleStatus.Reserved));

        Assert.True(result.IsFailure);
        Assert.Equal(SaleErrors.InvalidVehicleSnapshot, result.Error);
    }

    [Fact]
    public void RejectReservation_WithValidFailureCode_IsTerminal()
    {
        var sale = CreateSale();

        var result = sale.RejectReservation(" vehicle_unavailable ");

        Assert.True(result.IsSuccess);
        Assert.Equal(SaleStatus.Rejected, sale.State);
        Assert.Equal("vehicle_unavailable", sale.FailureCode);
        Assert.True(sale.IsTerminal);
    }

    [Fact]
    public void RegisterPayment_IsIdempotent()
    {
        var sale = CreateAwaitingPaymentSale();
        var registeredAt = CreatedAt.AddMinutes(1);
        Assert.True(sale.RegisterPayment(registeredAt).IsSuccess);
        var version = sale.Version;

        var result = sale.RegisterPayment(registeredAt.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(registeredAt, sale.PaymentRegisteredAtUtc);
        Assert.Equal(version, sale.Version);
    }

    [Fact]
    public void InvalidTransition_DoesNotChangeSale()
    {
        var sale = CreateSale();

        var result = sale.Complete(CreatedAt.AddMinutes(1));

        Assert.True(result.IsFailure);
        Assert.Equal(SaleErrors.InvalidStateTransition, result.Error);
        Assert.Equal(SaleStatus.Reserving, sale.State);
    }

    [Fact]
    public void RecordProcessingFailure_SchedulesRetryAndClearsLease()
    {
        var sale = CreateSale();
        Assert.True(sale.AcquireLease("worker-1", CreatedAt.AddMinutes(1), CreatedAt).IsSuccess);

        var result = sale.RecordProcessingFailure(" temporary failure ", CreatedAt.AddMinutes(2));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, sale.Attempts);
        Assert.Equal("temporary failure", sale.LastError);
        Assert.Equal(CreatedAt.AddMinutes(2), sale.NextAttemptAtUtc);
        Assert.Null(sale.LeaseOwner);
        Assert.Null(sale.LeaseExpiresAtUtc);
    }

    [Fact]
    public void AcquireLease_PreventsAnotherOwnerUntilExpiration()
    {
        var sale = CreateSale();
        Assert.True(sale.AcquireLease("worker-1", CreatedAt.AddMinutes(2), CreatedAt).IsSuccess);

        var conflict = sale.AcquireLease("worker-2", CreatedAt.AddMinutes(3), CreatedAt.AddMinutes(1));
        var acquiredAfterExpiration = sale.AcquireLease("worker-2", CreatedAt.AddMinutes(4), CreatedAt.AddMinutes(3));

        Assert.True(conflict.IsFailure);
        Assert.Equal(SaleErrors.LeaseAlreadyAcquired, conflict.Error);
        Assert.True(acquiredAfterExpiration.IsSuccess);
        Assert.Equal("worker-2", sale.LeaseOwner);
    }

    [Fact]
    public void ReleaseLease_RequiresCurrentOwner()
    {
        var sale = CreateSale();
        Assert.True(sale.AcquireLease("worker-1", CreatedAt.AddMinutes(2), CreatedAt).IsSuccess);

        var mismatch = sale.ReleaseLease("worker-2");
        var released = sale.ReleaseLease("worker-1");

        Assert.True(mismatch.IsFailure);
        Assert.Equal(SaleErrors.LeaseOwnerMismatch, mismatch.Error);
        Assert.True(released.IsSuccess);
        Assert.Null(sale.LeaseOwner);
    }

    [Theory]
    [InlineData("vehicle")]
    [InlineData("subject")]
    [InlineData("cpf")]
    [InlineData("price")]
    [InlineData("key")]
    [InlineData("hash")]
    [InlineData("timestamp")]
    public void Create_RejectsInvalidRequiredValues(string invalidField)
    {
        var vehicleId = invalidField == "vehicle" ? Guid.Empty : Guid.NewGuid();
        var subject = invalidField == "subject" ? " " : "subject";
        var cpf = invalidField == "cpf" ? null! : BuyerCpf.Create("52998224725").Value!;
        var price = invalidField == "price" ? 0 : 50_000m;
        var key = invalidField == "key" ? " " : "key";
        var hash = invalidField == "hash" ? " " : "hash";
        var timestamp = invalidField == "timestamp" ? default : CreatedAt;

        var result = Sale.Create(vehicleId, subject, cpf, price, key, hash, timestamp);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Complete_RejectsDateBeforePayment()
    {
        var sale = CreateAwaitingPaymentSale();
        Assert.True(sale.RecordPayment(PaymentOutcome.Paid, CreatedAt.AddMinutes(2)).IsSuccess);

        var result = sale.Complete(CreatedAt.AddMinutes(1));

        Assert.True(result.IsFailure);
        Assert.Equal(SaleErrors.InvalidTimestamp, result.Error);
    }

    private static Sale CreateAwaitingPaymentSale()
    {
        var sale = CreateSale();
        Assert.True(sale.AcceptReservation(CreateSnapshot(sale.VehicleId, VehicleStatus.Reserved)).IsSuccess);
        return sale;
    }

    private static Sale CreateSale()
    {
        var cpf = BuyerCpf.Create("52998224725").Value!;
        return Sale.Create(Guid.NewGuid(), " buyer-subject ", cpf, 50_000m, " request-1 ", " sha256:payload ", CreatedAt).Value!;
    }

    internal static VehicleSnapshot CreateSnapshot(Guid vehicleId, VehicleStatus status,
        decimal price = 50_000m, int version = 1)
    {
        return VehicleSnapshot.Create(vehicleId, "Ford", "Ka", 2020, "Blue", price, status,
            version, CreatedAt).Value!;
    }
}
