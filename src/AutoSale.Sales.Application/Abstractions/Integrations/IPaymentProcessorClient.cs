namespace AutoSale.Application.Abstractions.Integrations;

public interface IPaymentProcessorClient
{
    Task<IntegrationResult<bool>> CreatePaymentAsync(Guid paymentCode, Guid saleId, decimal amount,
        CancellationToken cancellationToken);
}
