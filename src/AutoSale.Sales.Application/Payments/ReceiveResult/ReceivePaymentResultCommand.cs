using AutoSale.Domain.Payments;

namespace AutoSale.Application.Payments.ReceiveResult;

public sealed record ReceivePaymentResultCommand(Guid PaymentCode, Guid EventId, PaymentOutcome Outcome,
    DateTimeOffset OccurredAtUtc);
