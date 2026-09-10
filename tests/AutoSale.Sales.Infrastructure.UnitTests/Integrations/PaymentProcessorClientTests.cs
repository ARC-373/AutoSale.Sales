using System.Net;
using AutoSale.Application.Abstractions.Integrations;
using AutoSale.Infrastructure.Integrations.Payments;
using Microsoft.Extensions.Options;

namespace AutoSale.Sales.Infrastructure.UnitTests.Integrations;

public sealed class PaymentProcessorClientTests
{
    [Fact]
    public async Task CreatePaymentAsync_SendsStableCodeAmountAndCurrency()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)));
        var client = new PaymentProcessorClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://payments.test/") },
            Options.Create(new PaymentsOptions { ServiceKey = "payment-secret" }));
        var paymentCode = Guid.NewGuid();
        var saleId = Guid.NewGuid();

        var result = await client.CreatePaymentAsync(paymentCode, saleId, 50_000m, default);

        Assert.True(result.IsSuccess);
        Assert.EndsWith($"api/v1/payments/{paymentCode:D}", handler.RequestUri);
        Assert.Equal("payment-secret", handler.ServiceKey);
        Assert.Contains($"\"saleId\":\"{saleId:D}\"", handler.RequestBody);
        Assert.Contains("\"amount\":50000", handler.RequestBody);
        Assert.Contains("\"currency\":\"BRL\"", handler.RequestBody);
    }

    [Fact]
    public async Task CreatePaymentAsync_WithUnauthorizedResponse_ClassifiesFailure()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        var client = new PaymentProcessorClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://payments.test/") },
            Options.Create(new PaymentsOptions { ServiceKey = "payment-secret" }));

        var result = await client.CreatePaymentAsync(Guid.NewGuid(), Guid.NewGuid(), 1m, default);

        Assert.False(result.IsSuccess);
        Assert.Equal(IntegrationFailureKind.Unauthorized, result.FailureKind);
    }
}
