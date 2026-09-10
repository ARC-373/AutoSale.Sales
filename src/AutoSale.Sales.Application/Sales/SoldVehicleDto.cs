using AutoSale.Domain.Sales;

namespace AutoSale.Application.Sales;

public sealed record SoldVehicleDto(Guid VehicleId, string Make, string Model, int Year, string Color,
    decimal SalePrice, DateTimeOffset SoldAtUtc)
{
    public static SoldVehicleDto FromDomain(Sale sale)
    {
        ArgumentNullException.ThrowIfNull(sale.VehicleSnapshot);
        ArgumentNullException.ThrowIfNull(sale.CompletedAtUtc);
        ArgumentNullException.ThrowIfNull(sale.SalePrice);

        return new SoldVehicleDto(sale.VehicleId, sale.VehicleSnapshot.Make, sale.VehicleSnapshot.Model,
            sale.VehicleSnapshot.Year, sale.VehicleSnapshot.Color, sale.SalePrice.Value,
            sale.CompletedAtUtc.Value);
    }
}
