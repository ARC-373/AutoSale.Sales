namespace AutoSale.Api.Contracts.Sales;

public sealed record PurchaseVehicleRequest(string BuyerCpf, decimal ExpectedPrice);
