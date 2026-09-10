using AutoSale.Domain.Payments;

namespace AutoSale.Sales.Domain.UnitTests.Payments;

public sealed class PaymentCallbackTests
{
    [Fact]
    public void Create_WithValidValues_NormalizesDatesToUtc()
    {
        var occurredAt = new DateTimeOffset(2026, 9, 8, 10, 0, 0, TimeSpan.FromHours(-3));

        var result = PaymentCallback.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentOutcome.Paid,
            occurredAt, occurredAt.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(TimeSpan.Zero, result.Value!.OccurredAtUtc.Offset);
        Assert.Equal(TimeSpan.Zero, result.Value.ReceivedAtUtc.Offset);
    }

    [Fact]
    public void Create_WithUndefinedOutcome_ReturnsFailure()
    {
        var now = DateTimeOffset.UtcNow;

        var result = PaymentCallback.Create(Guid.NewGuid(), Guid.NewGuid(), (PaymentOutcome)999, now, now);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentCallbackErrors.InvalidOutcome, result.Error);
    }

    [Theory]
    [InlineData("payment")]
    [InlineData("event")]
    [InlineData("occurred")]
    [InlineData("received")]
    public void Create_RejectsInvalidIdentifiersAndDates(string field)
    {
        var now = DateTimeOffset.UtcNow;

        var result = PaymentCallback.Create(
            field == "payment" ? Guid.Empty : Guid.NewGuid(),
            field == "event" ? Guid.Empty : Guid.NewGuid(),
            PaymentOutcome.Cancelled,
            field == "occurred" ? default : now,
            field == "received" ? default : now);

        Assert.True(result.IsFailure);
    }
}
