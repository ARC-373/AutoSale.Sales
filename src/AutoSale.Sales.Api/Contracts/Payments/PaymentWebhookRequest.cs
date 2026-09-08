using AutoSale.Domain.Payments;

namespace AutoSale.Api.Contracts.Payments;

public sealed record PaymentWebhookRequest(Guid PaymentCode, Guid EventId, PaymentOutcome Status,
    DateTimeOffset OccurredAtUtc);
