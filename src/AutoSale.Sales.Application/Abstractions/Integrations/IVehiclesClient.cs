using AutoSale.Domain.Sales;

namespace AutoSale.Application.Abstractions.Integrations;

public interface IVehiclesClient
{
    Task<IntegrationResult<VehicleSnapshot>> ReserveAsync(Guid vehicleId, Guid saleId, decimal expectedPrice,
        CancellationToken cancellationToken);
    Task<IntegrationResult<VehicleSnapshot>> ConfirmReservationAsync(Guid vehicleId, Guid saleId,
        CancellationToken cancellationToken);
    Task<IntegrationResult<VehicleSnapshot>> ReleaseReservationAsync(Guid vehicleId, Guid saleId,
        CancellationToken cancellationToken);
}
