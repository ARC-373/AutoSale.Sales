using AutoSale.SharedKernel.Results;

namespace AutoSale.Domain.Sales;

public sealed record VehicleSnapshot
{
    private const int MakeMaxLength = 120;
    private const int ModelMaxLength = 120;
    private const int ColorMaxLength = 50;
    private const int MinimumYear = 1886;

    private VehicleSnapshot()
    {
    }

    private VehicleSnapshot(Guid id, string make, string model, int year, string color, decimal price,
        VehicleStatus status, int version, DateTimeOffset updatedAtUtc)
    {
        Id = id;
        Make = make;
        Model = model;
        Year = year;
        Color = color;
        Price = price;
        Status = status;
        Version = version;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid Id { get; private init; }
    public string Make { get; private init; } = string.Empty;
    public string Model { get; private init; } = string.Empty;
    public int Year { get; private init; }
    public string Color { get; private init; } = string.Empty;
    public decimal Price { get; private init; }
    public VehicleStatus Status { get; private init; }
    public int Version { get; private init; }
    public DateTimeOffset UpdatedAtUtc { get; private init; }

    public static Result<VehicleSnapshot> Create(Guid id, string make, string model, int year, string color,
        decimal price, VehicleStatus status, int version, DateTimeOffset updatedAtUtc)
    {
        if (id == Guid.Empty)
        {
            return Result.Failure<VehicleSnapshot>(VehicleSnapshotErrors.InvalidId);
        }

        var normalizedMake = Normalize(make);
        if (normalizedMake is null || normalizedMake.Length > MakeMaxLength)
        {
            return Result.Failure<VehicleSnapshot>(VehicleSnapshotErrors.InvalidMake);
        }

        var normalizedModel = Normalize(model);
        if (normalizedModel is null || normalizedModel.Length > ModelMaxLength)
        {
            return Result.Failure<VehicleSnapshot>(VehicleSnapshotErrors.InvalidModel);
        }

        if (year < MinimumYear)
        {
            return Result.Failure<VehicleSnapshot>(VehicleSnapshotErrors.InvalidYear);
        }

        var normalizedColor = Normalize(color);
        if (normalizedColor is null || normalizedColor.Length > ColorMaxLength)
        {
            return Result.Failure<VehicleSnapshot>(VehicleSnapshotErrors.InvalidColor);
        }

        if (price <= 0 || decimal.Round(price, 2) != price)
        {
            return Result.Failure<VehicleSnapshot>(VehicleSnapshotErrors.InvalidPrice);
        }

        if (!Enum.IsDefined(status))
        {
            return Result.Failure<VehicleSnapshot>(VehicleSnapshotErrors.InvalidStatus);
        }

        if (version <= 0)
        {
            return Result.Failure<VehicleSnapshot>(VehicleSnapshotErrors.InvalidVersion);
        }

        if (updatedAtUtc == default)
        {
            return Result.Failure<VehicleSnapshot>(VehicleSnapshotErrors.InvalidUpdatedAt);
        }

        return Result.Success(new VehicleSnapshot(id, normalizedMake, normalizedModel, year, normalizedColor,
            price, status, version, updatedAtUtc.ToUniversalTime()));
    }

    private static string? Normalize(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
