using System.ComponentModel.DataAnnotations;
using AutoSale.Payments.Mock.Payments;

namespace AutoSale.Payments.Mock.Contracts;

public sealed record CreatePaymentRequest(
    Guid SaleId,
    decimal Amount,
    [Required] string Currency);

public sealed record PaymentResponse(
    Guid PaymentCode,
    Guid SaleId,
    decimal Amount,
    string Currency,
    PaymentStatus Status,
    Guid? EventId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? OccurredAtUtc,
    DateTimeOffset? CallbackDeliveredAtUtc,
    int Attempts,
    DateTimeOffset? NextAttemptAtUtc,
    string? LastError)
{
    public static PaymentResponse From(Payment payment) => new(
        payment.PaymentCode, payment.SaleId, payment.Amount, payment.Currency, payment.Status,
        payment.EventId, payment.CreatedAtUtc, payment.OccurredAtUtc, payment.CallbackDeliveredAtUtc,
        payment.Attempts, payment.NextAttemptAtUtc, payment.LastError);
}

public sealed record PaymentWebhookRequest(
    Guid PaymentCode,
    Guid EventId,
    PaymentStatus Status,
    DateTimeOffset OccurredAtUtc);
