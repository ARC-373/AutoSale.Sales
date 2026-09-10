using AutoSale.Application.Abstractions.Clock;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Sales.ProcessPending;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoSale.Infrastructure.BackgroundServices;

public sealed class SaleProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<SaleProcessingOptions> _options;
    private readonly ILogger<SaleProcessingWorker> _logger;
    private readonly string _leaseOwner = $"{Environment.MachineName}-{Guid.NewGuid():N}";

    public SaleProcessingWorker(IServiceScopeFactory scopeFactory, IOptions<SaleProcessingOptions> options,
        ILogger<SaleProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.Value.PollIntervalSeconds));
        do
        {
            await ProcessBatchAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyCollection<Guid> saleIds;
            using (var claimScope = _scopeFactory.CreateScope())
            {
                var repository = claimScope.ServiceProvider.GetRequiredService<ISaleRepository>();
                var clock = claimScope.ServiceProvider.GetRequiredService<IClock>();
                var now = clock.UtcNow;
                var claimed = await repository.ClaimPendingAsync(_leaseOwner, now,
                    now.AddSeconds(_options.Value.LeaseSeconds), _options.Value.BatchSize, cancellationToken);
                saleIds = claimed.Select(sale => sale.Id).ToArray();
            }

            foreach (var saleId in saleIds)
            {
                using var processingScope = _scopeFactory.CreateScope();
                var handler = processingScope.ServiceProvider.GetRequiredService<ProcessPendingSaleHandler>();
                var result = await handler.HandleAsync(new ProcessPendingSaleCommand(saleId), cancellationToken);
                if (result.IsFailure)
                {
                    _logger.LogWarning("Sale {SaleId} could not be processed: {ErrorCode}", saleId, result.Error.Code);
                }
                else if (result.Value!.RetryScheduled && result.Value.IntegrationError is not null)
                {
                    _logger.LogWarning("Sale {SaleId} scheduled for retry: {ErrorCode}",
                        saleId, result.Value.IntegrationError);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Graceful shutdown.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected failure while processing pending sales.");
        }
    }
}
