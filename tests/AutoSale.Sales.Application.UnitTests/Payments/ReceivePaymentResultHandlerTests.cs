using AutoSale.Application.Common;
using AutoSale.Application.Payments.ReceiveResult;
using AutoSale.Domain.Payments;
using AutoSale.Domain.Sales;

namespace AutoSale.Sales.Application.UnitTests.Payments;

public sealed class ReceivePaymentResultHandlerTests
{
    [Theory]
    [InlineData(PaymentOutcome.Paid, SaleStatus.ConfirmingVehicle)]
    [InlineData(PaymentOutcome.Cancelled, SaleStatus.CancellingVehicle)]
    public async Task HandleAsync_WithFirstResult_PersistsCallbackAndMovesSale(
        PaymentOutcome outcome, SaleStatus expectedState)
    {
        var (handler, sales, callbacks, unitOfWork) = CreateHandler();
        var sale = CreateAwaitingPaymentSale();
        sales.Sales.Add(sale);

        var result = await handler.HandleAsync(new ReceivePaymentResultCommand(
            sale.PaymentCode, Guid.NewGuid(), outcome, TestData.Now.AddMinutes(1)), default);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsDuplicate);
        Assert.Equal(expectedState, sale.State);
        Assert.Single(callbacks.Callbacks);
        Assert.Equal(1, unitOfWork.TransactionExecutionCount);
    }

    [Fact]
    public async Task HandleAsync_WithEquivalentDuplicate_ReturnsExistingResultWithoutSaving()
    {
        var (handler, sales, callbacks, unitOfWork) = CreateHandler();
        var sale = CreateAwaitingPaymentSale();
        sales.Sales.Add(sale);
        var eventId = Guid.NewGuid();
        await handler.HandleAsync(new ReceivePaymentResultCommand(
            sale.PaymentCode, eventId, PaymentOutcome.Paid, TestData.Now), default);
        var saves = unitOfWork.SaveCount;

        var duplicate = await handler.HandleAsync(new ReceivePaymentResultCommand(
            sale.PaymentCode, eventId, PaymentOutcome.Paid, TestData.Now.AddMinutes(2)), default);

        Assert.True(duplicate.IsSuccess);
        Assert.True(duplicate.Value!.IsDuplicate);
        Assert.Equal(saves, unitOfWork.SaveCount);
        Assert.Single(callbacks.Callbacks);
    }

    [Fact]
    public async Task HandleAsync_WithOppositeResult_ReturnsConflict()
    {
        var (handler, sales, _, _) = CreateHandler();
        var sale = CreateAwaitingPaymentSale();
        sales.Sales.Add(sale);
        await handler.HandleAsync(new ReceivePaymentResultCommand(
            sale.PaymentCode, Guid.NewGuid(), PaymentOutcome.Paid, TestData.Now), default);

        var conflict = await handler.HandleAsync(new ReceivePaymentResultCommand(
            sale.PaymentCode, Guid.NewGuid(), PaymentOutcome.Cancelled, TestData.Now), default);

        Assert.True(conflict.IsFailure);
        Assert.Equal(ApplicationErrors.PaymentResultConflict, conflict.Error);
    }

    [Fact]
    public async Task HandleAsync_WithEventUsedByAnotherPayment_ReturnsConflict()
    {
        var (handler, sales, callbacks, _) = CreateHandler();
        var sale = CreateAwaitingPaymentSale();
        sales.Sales.Add(sale);
        var eventId = Guid.NewGuid();
        callbacks.Callbacks.Add(PaymentCallback.Create(
            Guid.NewGuid(), eventId, PaymentOutcome.Paid, TestData.Now, TestData.Now).Value!);

        var result = await handler.HandleAsync(new ReceivePaymentResultCommand(
            sale.PaymentCode, eventId, PaymentOutcome.Paid, TestData.Now), default);

        Assert.True(result.IsFailure);
        Assert.Equal(ApplicationErrors.PaymentEventConflict, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ForUnknownPaymentOrInvalidState_ReturnsFailure()
    {
        var (handler, sales, _, _) = CreateHandler();
        var unknown = await handler.HandleAsync(new ReceivePaymentResultCommand(
            Guid.NewGuid(), Guid.NewGuid(), PaymentOutcome.Paid, TestData.Now), default);
        var reserving = TestData.CreateSale();
        sales.Sales.Add(reserving);
        var invalidState = await handler.HandleAsync(new ReceivePaymentResultCommand(
            reserving.PaymentCode, Guid.NewGuid(), PaymentOutcome.Paid, TestData.Now), default);

        Assert.Equal(ApplicationErrors.PaymentNotFound, unknown.Error);
        Assert.Equal(ApplicationErrors.PaymentStateConflict, invalidState.Error);
    }

    [Theory]
    [InlineData("payment")]
    [InlineData("event")]
    [InlineData("outcome")]
    [InlineData("date")]
    public async Task HandleAsync_WithInvalidContract_ReturnsValidationFailure(string field)
    {
        var (handler, _, _, _) = CreateHandler();
        var command = new ReceivePaymentResultCommand(
            field == "payment" ? Guid.Empty : Guid.NewGuid(),
            field == "event" ? Guid.Empty : Guid.NewGuid(),
            field == "outcome" ? (PaymentOutcome)99 : PaymentOutcome.Paid,
            field == "date" ? default : TestData.Now);

        Assert.True((await handler.HandleAsync(command, default)).IsFailure);
    }

    private static Sale CreateAwaitingPaymentSale()
    {
        var sale = TestData.CreateSale();
        Assert.True(sale.AcceptReservation(TestData.Snapshot(sale.VehicleId, VehicleStatus.Reserved)).IsSuccess);
        return sale;
    }

    private static (ReceivePaymentResultHandler Handler, FakeSaleRepository Sales,
        FakePaymentCallbackRepository Callbacks, FakeUnitOfWork UnitOfWork) CreateHandler()
    {
        var sales = new FakeSaleRepository();
        var callbacks = new FakePaymentCallbackRepository();
        var unitOfWork = new FakeUnitOfWork();
        return (new ReceivePaymentResultHandler(sales, callbacks, unitOfWork, new FakeClock(TestData.Now)),
            sales, callbacks, unitOfWork);
    }
}
