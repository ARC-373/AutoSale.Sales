using AutoSale.Payments.Mock.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoSale.Payments.Mock.Payments;

public sealed class PaymentService(PaymentsDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<(Payment Payment, bool Created)> CreateAsync(Guid paymentCode, Guid saleId,
        decimal amount, string currency, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Payments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PaymentCode == paymentCode, cancellationToken);
        if (existing is not null)
        {
            EnsureEquivalent(existing, saleId, amount, currency);
            return (existing, false);
        }

        var payment = Payment.Create(paymentCode, saleId, amount, currency, timeProvider.GetUtcNow());
        dbContext.Payments.Add(payment);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return (payment, true);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            existing = await dbContext.Payments.AsNoTracking()
                .SingleOrDefaultAsync(x => x.PaymentCode == paymentCode || x.SaleId == saleId, cancellationToken);
            if (existing is null || existing.PaymentCode != paymentCode)
            {
                throw new PaymentConflictException("sale_payment_conflict",
                    "The sale is already associated with another payment.");
            }

            EnsureEquivalent(existing, saleId, amount, currency);
            return (existing, false);
        }
    }

    public Task<Payment?> GetAsync(Guid paymentCode, CancellationToken cancellationToken) =>
        dbContext.Payments.AsNoTracking().SingleOrDefaultAsync(x => x.PaymentCode == paymentCode, cancellationToken);

    public Task<List<Payment>> ListPendingAsync(CancellationToken cancellationToken) =>
        dbContext.Payments.AsNoTracking()
            .Where(x => x.Status == PaymentStatus.Pending)
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.PaymentCode)
            .ToListAsync(cancellationToken);

    public async Task<(Payment Payment, bool Changed)> DecideAsync(Guid paymentCode, PaymentStatus decision,
        CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments.SingleOrDefaultAsync(x => x.PaymentCode == paymentCode,
            cancellationToken) ?? throw new PaymentNotFoundException(paymentCode);
        var changed = payment.Decide(decision, timeProvider.GetUtcNow());

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            payment = await dbContext.Payments.AsNoTracking().SingleAsync(x => x.PaymentCode == paymentCode,
                cancellationToken);
            if (payment.Status != decision)
            {
                throw new PaymentConflictException("payment_decision_conflict",
                    "The payment already has the opposite terminal decision.");
            }

            changed = false;
        }

        return (payment, changed);
    }

    public async Task<Payment> RetryCallbackAsync(Guid paymentCode, CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments.SingleOrDefaultAsync(x => x.PaymentCode == paymentCode,
            cancellationToken) ?? throw new PaymentNotFoundException(paymentCode);
        payment.ScheduleRetry(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return payment;
    }

    private static void EnsureEquivalent(Payment payment, Guid saleId, decimal amount, string currency)
    {
        if (!payment.HasSameCreation(saleId, amount, currency))
        {
            throw new PaymentConflictException("payment_creation_conflict",
                "The payment code was already used with a different payload.");
        }
    }
}

public sealed class PaymentNotFoundException(Guid paymentCode)
    : KeyNotFoundException($"Payment '{paymentCode:D}' was not found.");
