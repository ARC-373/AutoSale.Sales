using AutoSale.Payments.Mock.Integrations;
using AutoSale.Payments.Mock.Persistence;
using AutoSale.Payments.Mock.Payments;
using Microsoft.EntityFrameworkCore;

namespace AutoSale.Payments.Mock.BackgroundServices;

public sealed class CallbackDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<CallbackDeliveryWorker> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private readonly string _workerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2), timeProvider);
        do
        {
            await ProcessOneAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task ProcessOneAsync(CancellationToken cancellationToken)
    {
        Payment? payment;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
            var now = timeProvider.GetUtcNow();
            payment = await db.Payments
                .Where(x => x.Status != PaymentStatus.Pending && x.CallbackDeliveredAtUtc == null &&
                    (x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= now) &&
                    (x.LeaseExpiresAtUtc == null || x.LeaseExpiresAtUtc <= now))
                .OrderBy(x => x.NextAttemptAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            if (payment is null)
            {
                return;
            }

            payment.Claim(_workerId, now.AddSeconds(30));
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return;
            }
        }

        try
        {
            await using var deliveryScope = scopeFactory.CreateAsyncScope();
            var client = deliveryScope.ServiceProvider.GetRequiredService<SalesWebhookClient>();
            await client.DeliverAsync(payment, cancellationToken);
            var db = deliveryScope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
            var tracked = await db.Payments.SingleAsync(x => x.PaymentCode == payment.PaymentCode, cancellationToken);
            tracked.MarkDelivered(timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Payment callback delivery failed for {PaymentCode}", payment.PaymentCode);
            await RecordFailureAsync(payment.PaymentCode, exception, cancellationToken);
        }
    }

    private async Task RecordFailureAsync(Guid paymentCode, Exception exception, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var payment = await db.Payments.SingleAsync(x => x.PaymentCode == paymentCode, cancellationToken);
        var delaySeconds = Math.Min(60, payment.Attempts switch
        {
            0 => 2,
            1 => 5,
            2 => 15,
            3 => 30,
            _ => 60
        });
        payment.RecordDeliveryFailure(exception.Message, timeProvider.GetUtcNow().AddSeconds(delaySeconds));
        await db.SaveChangesAsync(cancellationToken);
    }
}
