using AutoSale.Domain.Sales;

namespace AutoSale.Application.Sales;

public sealed record SaleDto(
    Guid Id,
    Guid VehicleId,
    string BuyerSubject,
    decimal SalePrice,
    DateTimeOffset PurchasedAtUtc,
    string? IdempotencyKey)
{
    public static SaleDto FromDomain(Sale sale) => new(
        sale.Id,
        sale.VehicleId,
        sale.BuyerSubject,
        sale.SalePrice,
        sale.PurchasedAtUtc,
        sale.IdempotencyKey);
}
