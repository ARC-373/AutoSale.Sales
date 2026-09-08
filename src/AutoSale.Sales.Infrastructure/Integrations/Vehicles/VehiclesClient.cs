using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoSale.Application.Abstractions.Integrations;
using AutoSale.Domain.Sales;
using Microsoft.Extensions.Options;

namespace AutoSale.Infrastructure.Integrations.Vehicles;

public sealed class VehiclesClient : HttpIntegrationClient, IVehiclesClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public VehiclesClient(HttpClient httpClient, IOptions<VehiclesOptions> options)
        : base(httpClient, options.Value.ServiceKey)
    {
    }

    public async Task<IntegrationResult<VehicleSnapshot>> ReserveAsync(Guid vehicleId, Guid saleId,
        decimal expectedPrice, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put,
            $"internal/v1/vehicles/{vehicleId:D}/reservations/{saleId:D}")
        {
            Content = JsonContent.Create(new ReserveVehicleRequest(expectedPrice), options: JsonOptions)
        };
        return await SendForSnapshotAsync(request, cancellationToken);
    }

    public async Task<IntegrationResult<VehicleSnapshot>> ConfirmReservationAsync(Guid vehicleId, Guid saleId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put,
            $"internal/v1/vehicles/{vehicleId:D}/reservations/{saleId:D}/confirmation")
        {
            Content = JsonContent.Create(new { }, options: JsonOptions)
        };
        return await SendForSnapshotAsync(request, cancellationToken);
    }

    public async Task<IntegrationResult<VehicleSnapshot>> ReleaseReservationAsync(Guid vehicleId, Guid saleId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put,
            $"internal/v1/vehicles/{vehicleId:D}/reservations/{saleId:D}/release")
        {
            Content = JsonContent.Create(new { }, options: JsonOptions)
        };
        return await SendForSnapshotAsync(request, cancellationToken);
    }

    private async Task<IntegrationResult<VehicleSnapshot>> SendForSnapshotAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await SendAsync(request, cancellationToken);
        if (!response.IsSuccess)
        {
            return IntegrationResult<VehicleSnapshot>.Failure(response.FailureKind,
                response.ErrorCode!, response.SanitizedError!, response.RetryAfter);
        }

        try
        {
            var root = response.Value;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("vehicle", out var vehicle))
            {
                root = vehicle;
            }

            var contract = root.Deserialize<VehicleSnapshotContract>(JsonOptions);
            if (contract is null)
            {
                return InvalidSnapshot();
            }

            var snapshot = VehicleSnapshot.Create(contract.Id, contract.Make, contract.Model, contract.Year,
                contract.Color, contract.Price, contract.Status, contract.Version, contract.UpdatedAtUtc);
            return snapshot.IsSuccess
                ? IntegrationResult<VehicleSnapshot>.Success(snapshot.Value!)
                : InvalidSnapshot();
        }
        catch (JsonException)
        {
            return InvalidSnapshot();
        }
    }

    private static IntegrationResult<VehicleSnapshot> InvalidSnapshot() =>
        IntegrationResult<VehicleSnapshot>.Failure(IntegrationFailureKind.InvalidResponse,
            "vehicle_snapshot_invalid", "Vehicles returned an invalid snapshot.");

    private sealed record ReserveVehicleRequest(decimal ExpectedPrice);
    private sealed record VehicleSnapshotContract(Guid Id, string Make, string Model, int Year, string Color,
        decimal Price, VehicleStatus Status, int Version, DateTimeOffset UpdatedAtUtc);
}
