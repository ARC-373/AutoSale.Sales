using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace AutoSale.Infrastructure.Persistence.Repositories;

public sealed class PaymentCallbackRepository : IPaymentCallbackRepository
{
    private readonly SalesDbContext _dbContext;

    public PaymentCallbackRepository(SalesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PaymentCallback?> GetByPaymentCodeAsync(Guid paymentCode, CancellationToken cancellationToken) =>
        _dbContext.PaymentCallbacks.SingleOrDefaultAsync(callback => callback.Id == paymentCode,
            cancellationToken);

    public Task<PaymentCallback?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken) =>
        _dbContext.PaymentCallbacks.SingleOrDefaultAsync(callback => callback.EventId == eventId,
            cancellationToken);

    public async Task AddAsync(PaymentCallback callback, CancellationToken cancellationToken)
    {
        await _dbContext.PaymentCallbacks.AddAsync(callback, cancellationToken);
    }
}
