using System.Net;
using System.Text;
using AutoSale.Application.Abstractions.Integrations;
using AutoSale.Domain.Sales;
using AutoSale.Infrastructure.Integrations.Vehicles;
using Microsoft.Extensions.Options;

namespace AutoSale.Sales.Infrastructure.UnitTests.Integrations;

public sealed class VehiclesClientTests
{
    [Fact]
    public async Task ReserveAsync_SendsExpectedContractAndMapsWrappedSnapshot()
    {
        var vehicleId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var handler = Respond(HttpStatusCode.Created, $$$"""
            {"saleId":"{{{saleId}}}","reservationStatus":"Reserved","vehicle":{
              "id":"{{{vehicleId}}}","make":"Ford","model":"Ka","year":2020,"color":"Blue",
              "price":50000.00,"status":"Reserved","version":2,"updatedAtUtc":"2026-09-08T12:00:00Z"}}
            """);
        var client = CreateClient(handler);

        var result = await client.ReserveAsync(vehicleId, saleId, 50_000m, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(VehicleStatus.Reserved, result.Value!.Status);
        Assert.Equal(2, result.Value.Version);
        Assert.Equal("service-secret", handler.ServiceKey);
        Assert.EndsWith($"internal/v1/vehicles/{vehicleId:D}/reservations/{saleId:D}", handler.RequestUri);
        Assert.Contains("\"expectedPrice\":50000", handler.RequestBody);
    }

    [Fact]
    public async Task ConfirmAsync_MapsDirectSnapshot()
    {
        var vehicleId = Guid.NewGuid();
        var handler = Respond(HttpStatusCode.OK, SnapshotJson(vehicleId, "Sold"));

        var result = await CreateClient(handler).ConfirmReservationAsync(vehicleId, Guid.NewGuid(), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(VehicleStatus.Sold, result.Value!.Status);
    }

    [Fact]
    public async Task ReleaseAsync_WithInvalidSnapshot_ReturnsInvalidResponse()
    {
        var result = await CreateClient(Respond(HttpStatusCode.OK, "{}"))
            .ReleaseReservationAsync(Guid.NewGuid(), Guid.NewGuid(), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(IntegrationFailureKind.InvalidResponse, result.FailureKind);
    }

    [Fact]
    public async Task ReserveAsync_WithConflict_MapsProblemCode()
    {
        var handler = Respond(HttpStatusCode.Conflict, "{\"code\":\"vehicle_unavailable\"}");

        var result = await CreateClient(handler)
            .ReserveAsync(Guid.NewGuid(), Guid.NewGuid(), 50_000m, default);

        Assert.False(result.IsSuccess);
        Assert.Equal(IntegrationFailureKind.Conflict, result.FailureKind);
        Assert.Equal("vehicle_unavailable", result.ErrorCode);
    }

    [Fact]
    public async Task ReserveAsync_WithTooManyRequests_PreservesRetryAfter()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(12));
            return Task.FromResult(response);
        });

        var result = await CreateClient(handler)
            .ReserveAsync(Guid.NewGuid(), Guid.NewGuid(), 50_000m, default);

        Assert.Equal(IntegrationFailureKind.Transient, result.FailureKind);
        Assert.Equal(TimeSpan.FromSeconds(12), result.RetryAfter);
    }

    [Fact]
    public async Task ReserveAsync_WithNetworkFailure_ReturnsTransientFailure()
    {
        var handler = new StubHttpMessageHandler((_, _) => throw new HttpRequestException("offline"));

        var result = await CreateClient(handler)
            .ReserveAsync(Guid.NewGuid(), Guid.NewGuid(), 50_000m, default);

        Assert.Equal(IntegrationFailureKind.Transient, result.FailureKind);
        Assert.Equal("integration_unavailable", result.ErrorCode);
    }

    private static VehiclesClient CreateClient(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://vehicles.test/") };
        return new VehiclesClient(httpClient,
            Options.Create(new VehiclesOptions { SalesToVehiclesServiceKey = "service-secret" }));
    }

    private static StubHttpMessageHandler Respond(HttpStatusCode status, string body) =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        }));

    private static string SnapshotJson(Guid vehicleId, string status) => $$"""
        {"id":"{{vehicleId}}","make":"Ford","model":"Ka","year":2020,"color":"Blue",
         "price":50000.00,"status":"{{status}}","version":2,"updatedAtUtc":"2026-09-08T12:00:00Z"}
        """;
}
