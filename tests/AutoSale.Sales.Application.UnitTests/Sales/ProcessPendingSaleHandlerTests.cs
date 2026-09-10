using AutoSale.Application.Abstractions.Integrations;
using AutoSale.Application.Common;
using AutoSale.Application.Sales.ProcessPending;
using AutoSale.Domain.Payments;
using AutoSale.Domain.Sales;

namespace AutoSale.Sales.Application.UnitTests.Sales;

public sealed class ProcessPendingSaleHandlerTests
{
    [Fact]
    public async Task Reserving_WithAcceptedReservation_MovesToAwaitingPaymentAndUpdatesCatalog()
    {
        var sale = TestData.CreateSale();
        var snapshot = TestData.Snapshot(sale.VehicleId, VehicleStatus.Reserved);
        var setup = CreateHandler(sale);
        setup.Vehicles.Reserve = (_, _, _) => IntegrationResult<VehicleSnapshot>.Success(snapshot);

        var result = await setup.Handler.HandleAsync(new ProcessPendingSaleCommand(sale.Id), default);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.StateChanged);
        Assert.Equal(SaleStatus.AwaitingPayment, sale.State);
        Assert.Single(setup.Catalog.Vehicles);
        Assert.Equal(snapshot, sale.VehicleSnapshot);
    }

    [Theory]
    [InlineData(IntegrationFailureKind.NotFound)]
    [InlineData(IntegrationFailureKind.Conflict)]
    public async Task Reserving_WithDefinitiveFailure_RejectsSale(IntegrationFailureKind kind)
    {
        var sale = TestData.CreateSale();
        var setup = CreateHandler(sale);
        setup.Vehicles.Reserve = (_, _, _) => IntegrationResult<VehicleSnapshot>.Failure(
            kind, "vehicle_unavailable", "Unavailable");

        var result = await setup.Handler.HandleAsync(new ProcessPendingSaleCommand(sale.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(SaleStatus.Rejected, sale.State);
        Assert.Equal("vehicle_unavailable", sale.FailureCode);
        Assert.False(result.Value!.RetryScheduled);
    }

    [Fact]
    public async Task Reserving_WithTransientFailure_SchedulesRetry()
    {
        var sale = TestData.CreateSale();
        var setup = CreateHandler(sale);
        setup.Vehicles.Reserve = (_, _, _) => IntegrationResult<VehicleSnapshot>.Failure(
            IntegrationFailureKind.Transient, "timeout", "Timed out");

        var result = await setup.Handler.HandleAsync(new ProcessPendingSaleCommand(sale.Id), default);

        Assert.True(result.Value!.RetryScheduled);
        Assert.Equal(SaleStatus.Reserving, sale.State);
        Assert.Equal(TestData.Now.AddSeconds(2), sale.NextAttemptAtUtc);
        Assert.Contains("timeout", sale.LastError);
    }

    [Fact]
    public async Task AwaitingPayment_CreatesPaymentAndPersistsRegistration()
    {
        var sale = CreateAwaitingPaymentSale();
        var setup = CreateHandler(sale);
        Guid receivedPaymentCode = default;
        setup.Payments.Create = (paymentCode, _, amount) =>
        {
            receivedPaymentCode = paymentCode;
            Assert.Equal(50_000m, amount);
            return IntegrationResult<bool>.Success(true);
        };

        var result = await setup.Handler.HandleAsync(new ProcessPendingSaleCommand(sale.Id), default);

        Assert.True(result.Value!.StateChanged);
        Assert.Equal(sale.PaymentCode, receivedPaymentCode);
        Assert.Equal(TestData.Now, sale.PaymentRegisteredAtUtc);
    }

    [Fact]
    public async Task ConfirmingVehicle_WithSoldSnapshot_CompletesSale()
    {
        var sale = CreateAwaitingPaymentSale();
        Assert.True(sale.RecordPayment(PaymentOutcome.Paid, TestData.Now).IsSuccess);
        var setup = CreateHandler(sale);
        setup.Catalog.Vehicles.Add(AutoSale.Domain.Catalog.VehicleCatalog.Create(
            TestData.Snapshot(sale.VehicleId, VehicleStatus.Reserved), TestData.Now).Value!);
        setup.Vehicles.Confirm = (_, _) => IntegrationResult<VehicleSnapshot>.Success(
            TestData.Snapshot(sale.VehicleId, VehicleStatus.Sold, 2));

        var result = await setup.Handler.HandleAsync(new ProcessPendingSaleCommand(sale.Id), default);

        Assert.True(result.Value!.StateChanged);
        Assert.Equal(SaleStatus.Completed, sale.State);
        Assert.Equal(VehicleStatus.Sold, setup.Catalog.Vehicles.Single().Status);
    }

    [Fact]
    public async Task CancellingVehicle_WithAvailableSnapshot_CancelsSale()
    {
        var sale = CreateAwaitingPaymentSale();
        Assert.True(sale.RecordPayment(PaymentOutcome.Cancelled, TestData.Now).IsSuccess);
        var setup = CreateHandler(sale);
        setup.Catalog.Vehicles.Add(AutoSale.Domain.Catalog.VehicleCatalog.Create(
            TestData.Snapshot(sale.VehicleId, VehicleStatus.Reserved), TestData.Now).Value!);
        setup.Vehicles.Release = (_, _) => IntegrationResult<VehicleSnapshot>.Success(
            TestData.Snapshot(sale.VehicleId, VehicleStatus.Available, 2));

        var result = await setup.Handler.HandleAsync(new ProcessPendingSaleCommand(sale.Id), default);

        Assert.True(result.Value!.StateChanged);
        Assert.Equal(SaleStatus.Cancelled, sale.State);
        Assert.Equal(VehicleStatus.Available, setup.Catalog.Vehicles.Single().Status);
    }

    [Fact]
    public async Task TerminalOrWaitingSale_PerformsNoTransition()
    {
        var waiting = CreateAwaitingPaymentSale();
        Assert.True(waiting.RegisterPayment(TestData.Now).IsSuccess);
        var waitingResult = await CreateHandler(waiting).Handler.HandleAsync(
            new ProcessPendingSaleCommand(waiting.Id), default);
        var terminal = TestData.CreateSale();
        Assert.True(terminal.RejectReservation("not_found").IsSuccess);
        var terminalResult = await CreateHandler(terminal).Handler.HandleAsync(
            new ProcessPendingSaleCommand(terminal.Id), default);

        Assert.False(waitingResult.Value!.StateChanged);
        Assert.False(terminalResult.Value!.StateChanged);
    }

    [Fact]
    public async Task UnknownSale_ReturnsNotFound()
    {
        var setup = CreateHandler();

        var result = await setup.Handler.HandleAsync(new ProcessPendingSaleCommand(Guid.NewGuid()), default);

        Assert.True(result.IsFailure);
        Assert.Equal(ApplicationErrors.SaleNotFound, result.Error);
    }

    [Fact]
    public void RetrySchedule_UsesProgressiveDelayAndRetryAfter()
    {
        Assert.Equal(TimeSpan.FromSeconds(2), RetrySchedule.GetDelay(0, null));
        Assert.Equal(TimeSpan.FromSeconds(60), RetrySchedule.GetDelay(99, null));
        Assert.Equal(TimeSpan.FromSeconds(42), RetrySchedule.GetDelay(0, TimeSpan.FromSeconds(42)));
    }

    private static Sale CreateAwaitingPaymentSale()
    {
        var sale = TestData.CreateSale();
        Assert.True(sale.AcceptReservation(TestData.Snapshot(sale.VehicleId, VehicleStatus.Reserved)).IsSuccess);
        return sale;
    }

    private static HandlerSetup CreateHandler(Sale? sale = null)
    {
        var sales = new FakeSaleRepository();
        if (sale is not null)
        {
            sales.Sales.Add(sale);
        }

        var catalog = new FakeCatalogRepository();
        var vehicles = new FakeVehiclesClient();
        var payments = new FakePaymentClient();
        var handler = new ProcessPendingSaleHandler(sales, catalog, vehicles, payments,
            new FakeUnitOfWork(), new FakeClock(TestData.Now));
        return new HandlerSetup(handler, catalog, vehicles, payments);
    }

    private sealed record HandlerSetup(ProcessPendingSaleHandler Handler, FakeCatalogRepository Catalog,
        FakeVehiclesClient Vehicles, FakePaymentClient Payments);
}
