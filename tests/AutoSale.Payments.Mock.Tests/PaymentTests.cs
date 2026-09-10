using AutoSale.Payments.Mock.Payments;

namespace AutoSale.Payments.Mock.Tests;

public sealed class PaymentTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-08T12:00:00Z");

    [Fact]
    public void Create_sets_pending_payment()
    {
        var payment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 150.25m, "BRL", Now);

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal("BRL", payment.Currency);
        Assert.Null(payment.EventId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.001)]
    public void Create_rejects_invalid_amount(decimal amount) =>
        Assert.Throws<ArgumentException>(() => Payment.Create(Guid.NewGuid(), Guid.NewGuid(), amount, "BRL", Now));

    [Fact]
    public void Create_rejects_non_brl_currency() =>
        Assert.Throws<ArgumentException>(() => Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 10m, "USD", Now));

    [Fact]
    public void Decide_is_idempotent_for_same_outcome()
    {
        var payment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 10m, "BRL", Now);

        Assert.True(payment.Decide(PaymentStatus.Paid, Now));
        var eventId = payment.EventId;
        Assert.False(payment.Decide(PaymentStatus.Paid, Now.AddMinutes(1)));

        Assert.Equal(eventId, payment.EventId);
        Assert.Equal(Now, payment.OccurredAtUtc);
    }

    [Fact]
    public void Decide_rejects_opposite_outcome()
    {
        var payment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 10m, "BRL", Now);
        payment.Decide(PaymentStatus.Cancelled, Now);

        var exception = Assert.Throws<PaymentConflictException>(() =>
            payment.Decide(PaymentStatus.Paid, Now.AddMinutes(1)));

        Assert.Equal("payment_decision_conflict", exception.Code);
    }

    [Fact]
    public void Delivery_failure_preserves_event_and_schedules_retry()
    {
        var payment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 10m, "BRL", Now);
        payment.Decide(PaymentStatus.Paid, Now);
        var eventId = payment.EventId;

        payment.RecordDeliveryFailure("temporary", Now.AddSeconds(2));

        Assert.Equal(1, payment.Attempts);
        Assert.Equal("temporary", payment.LastError);
        Assert.Equal(eventId, payment.EventId);
        Assert.Equal(Now.AddSeconds(2), payment.NextAttemptAtUtc);
    }

    [Fact]
    public void Delivered_callback_cannot_be_retried()
    {
        var payment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 10m, "BRL", Now);
        payment.Decide(PaymentStatus.Paid, Now);
        payment.MarkDelivered(Now.AddSeconds(1));

        Assert.Throws<PaymentConflictException>(() => payment.ScheduleRetry(Now.AddSeconds(2)));
    }
}
