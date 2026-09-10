using System.Net;
using AutoSale.Payments.Mock.Integrations;
using AutoSale.Payments.Mock.Payments;
using Microsoft.Extensions.Options;

namespace AutoSale.Payments.Mock.Tests;

public sealed class SalesWebhookClientTests
{
    [Fact]
    public async Task Deliver_sends_sales_contract_with_key_and_string_status()
    {
        HttpRequestMessage? captured = null;
        string? body = null;
        var handler = new StubHandler(async request =>
        {
            captured = request;
            body = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://sales.test/") };
        var client = new SalesWebhookClient(httpClient, Options.Create(new SalesWebhookOptions
        {
            BaseUrl = "http://sales.test/",
            WebhookKey = "callback-key"
        }));
        var payment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "BRL", DateTimeOffset.UtcNow);
        payment.Decide(PaymentStatus.Paid, DateTimeOffset.UtcNow);

        await client.DeliverAsync(payment, CancellationToken.None);

        Assert.Equal("/api/v1/payments/webhook", captured!.RequestUri!.AbsolutePath);
        Assert.Equal("callback-key", captured.Headers.GetValues("X-Payment-Webhook-Key").Single());
        Assert.Contains("\"status\":\"Paid\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("cpf", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Deliver_throws_when_sales_rejects_callback()
    {
        var handler = new StubHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        var client = new SalesWebhookClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://sales.test/") },
            Options.Create(new SalesWebhookOptions { BaseUrl = "http://sales.test/", WebhookKey = "key" }));
        var payment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "BRL", DateTimeOffset.UtcNow);
        payment.Decide(PaymentStatus.Cancelled, DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.DeliverAsync(payment, CancellationToken.None));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> response)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) => response(request);
    }
}
