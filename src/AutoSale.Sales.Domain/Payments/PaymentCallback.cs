using AutoSale.SharedKernel.Domain;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Domain.Payments;

public sealed class PaymentCallback : Entity
{
    private PaymentCallback()
    {
    }

    private PaymentCallback(Guid paymentCode, Guid eventId, PaymentOutcome outcome,
        DateTimeOffset occurredAtUtc, DateTimeOffset receivedAtUtc)
        : base(paymentCode)
    {
        EventId = eventId;
        Outcome = outcome;
        OccurredAtUtc = occurredAtUtc;
        ReceivedAtUtc = receivedAtUtc;
    }

    public Guid PaymentCode => Id;
    public Guid EventId { get; private set; }
    public PaymentOutcome Outcome { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public DateTimeOffset ReceivedAtUtc { get; private set; }

    public static Result<PaymentCallback> Create(Guid paymentCode, Guid eventId, PaymentOutcome outcome,
        DateTimeOffset occurredAtUtc, DateTimeOffset receivedAtUtc)
    {
        if (paymentCode == Guid.Empty)
        {
            return Result.Failure<PaymentCallback>(PaymentCallbackErrors.InvalidPaymentCode);
        }

        if (eventId == Guid.Empty)
        {
            return Result.Failure<PaymentCallback>(PaymentCallbackErrors.InvalidEventId);
        }

        if (!Enum.IsDefined(outcome))
        {
            return Result.Failure<PaymentCallback>(PaymentCallbackErrors.InvalidOutcome);
        }

        if (occurredAtUtc == default || receivedAtUtc == default)
        {
            return Result.Failure<PaymentCallback>(PaymentCallbackErrors.InvalidTimestamp);
        }

        return Result.Success(new PaymentCallback(paymentCode, eventId, outcome,
            occurredAtUtc.ToUniversalTime(), receivedAtUtc.ToUniversalTime()));
    }
}
