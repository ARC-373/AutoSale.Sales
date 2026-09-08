using AutoSale.Domain.Sales;

namespace AutoSale.Application.Catalog.Upsert;

public sealed record UpsertCatalogVehicleCommand(Guid VehicleId, VehicleSnapshot Snapshot);
