using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoSale.Payments.Mock.Contracts;
using AutoSale.Payments.Mock.Payments;

namespace AutoSale.Payments.Mock.Tests;

public sealed class PaymentsApiTests(MockApiFactory factory) : IClassFixture<MockApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Creation_requires_sales_key()
    {
        var response = await _client.PutAsJsonAsync($"/api/v1/payments/{Guid.NewGuid()}", Request(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_is_idempotent_and_rejects_different_payload()
    {
        var paymentCode = Guid.NewGuid();
        var saleId = Guid.NewGuid();

        var first = await PutPayment(paymentCode, Request(saleId));
        var duplicate = await PutPayment(paymentCode, Request(saleId));
        var conflict = await PutPayment(paymentCode, Request(Guid.NewGuid()));

        Assert.True(first.StatusCode == HttpStatusCode.Created, await first.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task Operator_can_approve_and_read_payment()
    {
        var paymentCode = Guid.NewGuid();
        await PutPayment(paymentCode, Request(Guid.NewGuid()));

        var approved = await OperatorPost($"/api/v1/payments/{paymentCode}/approve");
        var duplicate = await OperatorPost($"/api/v1/payments/{paymentCode}/approve");
        using var get = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/payments/{paymentCode}");
        get.Headers.Add("X-Operator-Key", "operator-test-key");
        var read = await _client.SendAsync(get);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        var payment = await read.Content.ReadFromJsonAsync<PaymentResponse>(options);

        Assert.Equal(HttpStatusCode.Accepted, approved.StatusCode);
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        Assert.Equal(PaymentStatus.Paid, payment!.Status);
        Assert.NotNull(payment.EventId);
    }

    [Fact]
    public async Task Opposite_decision_returns_conflict()
    {
        var paymentCode = Guid.NewGuid();
        await PutPayment(paymentCode, Request(Guid.NewGuid()));
        await OperatorPost($"/api/v1/payments/{paymentCode}/reject");

        var response = await OperatorPost($"/api/v1/payments/{paymentCode}/approve");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_payment_returns_not_found()
    {
        var response = await OperatorPost($"/api/v1/payments/{Guid.NewGuid()}/approve");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_lists_only_pending_payments()
    {
        var pendingCode = Guid.NewGuid();
        var paidCode = Guid.NewGuid();
        await PutPayment(pendingCode, Request(Guid.NewGuid()));
        await PutPayment(paidCode, Request(Guid.NewGuid()));
        await OperatorPost($"/api/v1/payments/{paidCode}/approve");

        var html = await _client.GetStringAsync("/");

        Assert.Contains(pendingCode.ToString(), html, StringComparison.Ordinal);
        Assert.DoesNotContain(paidCode.ToString(), html, StringComparison.Ordinal);
        Assert.Contains("Aprovar", html, StringComparison.Ordinal);
        Assert.Contains("Cancelar", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Health_endpoints_are_available()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health/live")).StatusCode);
    }

    private static CreatePaymentRequest Request(Guid saleId) => new(saleId, 149_990m, "BRL");

    private async Task<HttpResponseMessage> PutPayment(Guid paymentCode, CreatePaymentRequest request)
    {
        using var message = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/payments/{paymentCode}")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("X-Service-Key", "sales-test-key");
        return await _client.SendAsync(message);
    }

    private async Task<HttpResponseMessage> OperatorPost(string uri)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, uri);
        message.Headers.Add("X-Operator-Key", "operator-test-key");
        return await _client.SendAsync(message);
    }
}
