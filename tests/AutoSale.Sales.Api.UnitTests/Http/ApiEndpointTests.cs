using System.Net;
using System.Net.Http.Json;

namespace AutoSale.Sales.Api.UnitTests.Http;

public sealed class ApiEndpointTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ApiEndpointTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PublicListings_ReturnCamelCasePagedContractsWithStringEnums()
    {
        var available = await _client.GetAsync("/api/v1/vehicles/available");
        var sold = await _client.GetAsync("/api/v1/sales/sold");
        var availableJson = await available.Content.ReadAsStringAsync();
        var soldJson = await sold.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, available.StatusCode);
        Assert.Contains("\"status\":\"Available\"", availableJson);
        Assert.Contains("\"totalCount\":1", availableJson);
        Assert.Equal(HttpStatusCode.OK, sold.StatusCode);
        Assert.Contains("\"soldAtUtc\"", soldJson);
        Assert.DoesNotContain("buyer", soldJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Purchase_ReturnsAcceptedAndLocation()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"/api/v1/vehicles/{ApiFactory.VehicleId:D}/purchase")
        {
            Content = JsonContent.Create(new { buyerCpf = "52998224725", expectedPrice = 50_000m })
        };
        request.Headers.Add("Idempotency-Key", "purchase-1");

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal($"/api/v1/sales/{ApiFactory.SaleId:D}", response.Headers.Location?.ToString());
        Assert.Contains("\"status\":\"Reserving\"", body);
    }

    [Fact]
    public async Task InvalidPurchaseBody_ReturnsStableProblemDetails()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"/api/v1/vehicles/{ApiFactory.VehicleId:D}/purchase")
        {
            Content = new StringContent("{invalid-json", System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Idempotency-Key", "purchase-1");

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("request.invalid", body);
        Assert.Contains("traceId", body);
    }

    [Fact]
    public async Task InternalEndpoints_RequireDirectionSpecificKeys()
    {
        var webhookBody = new
        {
            paymentCode = ApiFactory.PaymentCode,
            eventId = Guid.NewGuid(),
            status = "Paid",
            occurredAtUtc = "2026-09-08T12:01:00Z"
        };
        var deniedWebhook = await _client.PostAsJsonAsync("/api/v1/payments/webhook", webhookBody);
        using var webhook = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/webhook")
        {
            Content = JsonContent.Create(webhookBody)
        };
        webhook.Headers.Add("X-Payment-Webhook-Key", "payment-webhook");
        var acceptedWebhook = await _client.SendAsync(webhook);

        Assert.Equal(HttpStatusCode.Forbidden, deniedWebhook.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, acceptedWebhook.StatusCode);
    }

    [Fact]
    public async Task CatalogEndpoint_AcceptsVehicleServiceKey()
    {
        using var request = new HttpRequestMessage(HttpMethod.Put,
            $"/internal/v1/catalog/vehicles/{ApiFactory.VehicleId:D}")
        {
            Content = JsonContent.Create(new
            {
                id = ApiFactory.VehicleId,
                make = "Ford",
                model = "Ka",
                year = 2020,
                color = "Blue",
                price = 50_000m,
                status = "Available",
                version = 1,
                updatedAtUtc = "2026-09-08T12:00:00Z"
            })
        };
        request.Headers.Add("X-Service-Key", "vehicles-inbound");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SaleQueryAndLiveness_AreMapped()
    {
        var sale = await _client.GetAsync($"/api/v1/sales/{ApiFactory.SaleId:D}");
        var live = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, sale.StatusCode);
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
    }
}
