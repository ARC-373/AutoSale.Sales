using AutoSale.Domain.Sales;

namespace AutoSale.Application.Sales.ProcessPending;

public sealed record ProcessPendingSaleResult(Guid SaleId, SaleStatus State, bool StateChanged,
    bool RetryScheduled, string? IntegrationError);
