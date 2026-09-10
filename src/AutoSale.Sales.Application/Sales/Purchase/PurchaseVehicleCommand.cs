namespace AutoSale.Application.Sales.Purchase;

public sealed record PurchaseVehicleCommand(Guid VehicleId, string BuyerCpf, decimal ExpectedPrice,
    string IdempotencyKey);
