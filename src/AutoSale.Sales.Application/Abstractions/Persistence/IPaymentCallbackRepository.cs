using AutoSale.Domain.Payments;

namespace AutoSale.Application.Abstractions.Persistence;

public interface IPaymentCallbackRepository
{
    Task<PaymentCallback?> GetByPaymentCodeAsync(Guid paymentCode, CancellationToken cancellationToken);
    Task<PaymentCallback?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken);
    Task AddAsync(PaymentCallback callback, CancellationToken cancellationToken);
}
