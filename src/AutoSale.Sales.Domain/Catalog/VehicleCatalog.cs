using AutoSale.Domain.Sales;
using AutoSale.SharedKernel.Domain;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Domain.Catalog;

public sealed class VehicleCatalog : Entity
{
    private VehicleCatalog()
    {
    }

    private VehicleCatalog(VehicleSnapshot snapshot, DateTimeOffset synchronizedAtUtc)
        : base(snapshot.Id)
    {
        ApplySnapshot(snapshot, synchronizedAtUtc);
    }

    public Guid VehicleId => Id;
    public string Make { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public string Color { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public VehicleStatus Status { get; private set; }
    public int SourceVersion { get; private set; }
    public DateTimeOffset SourceUpdatedAtUtc { get; private set; }
    public DateTimeOffset SynchronizedAtUtc { get; private set; }

    public static Result<VehicleCatalog> Create(VehicleSnapshot snapshot, DateTimeOffset synchronizedAtUtc)
    {
        if (snapshot is null)
        {
            return Result.Failure<VehicleCatalog>(VehicleCatalogErrors.InvalidSnapshot);
        }

        if (synchronizedAtUtc == default)
        {
            return Result.Failure<VehicleCatalog>(VehicleCatalogErrors.InvalidSynchronizedAt);
        }

        return Result.Success(new VehicleCatalog(snapshot, synchronizedAtUtc.ToUniversalTime()));
    }

    public Result Apply(VehicleSnapshot snapshot, DateTimeOffset synchronizedAtUtc)
    {
        if (snapshot is null || snapshot.Id != Id)
        {
            return Result.Failure(VehicleCatalogErrors.InvalidSnapshot);
        }

        if (synchronizedAtUtc == default)
        {
            return Result.Failure(VehicleCatalogErrors.InvalidSynchronizedAt);
        }

        if (snapshot.Version < SourceVersion)
        {
            return Result.Success();
        }

        if (snapshot.Version == SourceVersion)
        {
            return HasSameSourceContent(snapshot)
                ? Result.Success()
                : Result.Failure(VehicleCatalogErrors.SourceVersionConflict);
        }

        ApplySnapshot(snapshot, synchronizedAtUtc.ToUniversalTime());
        return Result.Success();
    }

    public VehicleSnapshot ToSnapshot()
    {
        return VehicleSnapshot.Create(Id, Make, Model, Year, Color, Price, Status, SourceVersion, SourceUpdatedAtUtc).Value!;
    }

    private bool HasSameSourceContent(VehicleSnapshot snapshot) =>
        string.Equals(Make, snapshot.Make, StringComparison.Ordinal) &&
        string.Equals(Model, snapshot.Model, StringComparison.Ordinal) &&
        Year == snapshot.Year &&
        string.Equals(Color, snapshot.Color, StringComparison.Ordinal) &&
        Price == snapshot.Price &&
        Status == snapshot.Status &&
        SourceUpdatedAtUtc == snapshot.UpdatedAtUtc;

    private void ApplySnapshot(VehicleSnapshot snapshot, DateTimeOffset synchronizedAtUtc)
    {
        Make = snapshot.Make;
        Model = snapshot.Model;
        Year = snapshot.Year;
        Color = snapshot.Color;
        Price = snapshot.Price;
        Status = snapshot.Status;
        SourceVersion = snapshot.Version;
        SourceUpdatedAtUtc = snapshot.UpdatedAtUtc;
        SynchronizedAtUtc = synchronizedAtUtc;
    }
}
