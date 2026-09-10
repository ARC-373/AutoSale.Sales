using System.Net.Http.Json;
using AutoSale.Application.Abstractions.Integrations;
using Microsoft.Extensions.Options;

namespace AutoSale.Infrastructure.Integrations.Payments;

public sealed class PaymentProcessorClient : HttpIntegrationClient, IPaymentProcessorClient
{
    public PaymentProcessorClient(HttpClient httpClient, IOptions<PaymentsOptions> options)
        : base(httpClient, options.Value.ServiceKey)
    {
    }

    public async Task<IntegrationResult<bool>> CreatePaymentAsync(Guid paymentCode, Guid saleId, decimal amount,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/v1/payments/{paymentCode:D}")
        {
            Content = JsonContent.Create(new CreatePaymentRequest(saleId, amount, "BRL"))
        };
        var response = await SendAsync(request, cancellationToken);
        return response.IsSuccess
            ? IntegrationResult<bool>.Success(true)
            : IntegrationResult<bool>.Failure(response.FailureKind, response.ErrorCode!,
                response.SanitizedError!, response.RetryAfter);
    }

    private sealed record CreatePaymentRequest(Guid SaleId, decimal Amount, string Currency);
}
