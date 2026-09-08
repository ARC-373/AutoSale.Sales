using AutoSale.SharedKernel.Results;

namespace AutoSale.Domain.Payments;

public static class PaymentCallbackErrors
{
    public static readonly Error InvalidPaymentCode = new("payment_callback.payment_code.invalid", "Payment code must be specified.", ErrorType.Validation);
    public static readonly Error InvalidEventId = new("payment_callback.event_id.invalid", "Event id must be specified.", ErrorType.Validation);
    public static readonly Error InvalidOutcome = new("payment_callback.outcome.invalid", "Outcome must be Paid or Cancelled.", ErrorType.Validation);
    public static readonly Error InvalidTimestamp = new("payment_callback.timestamp.invalid", "Occurred and received timestamps must be specified.", ErrorType.Validation);
}
