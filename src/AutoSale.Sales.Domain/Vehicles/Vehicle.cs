using AutoSale.SharedKernel.Domain;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Domain.Vehicles;

public sealed class Vehicle : Entity
{
    private const int MakeMaxLength = 120;
    private const int ModelMaxLength = 120;
    private const int ColorMaxLength = 50;
    private const int MinimumYear = 1886;

    private Vehicle()
    {
    }

    private Vehicle(Guid id, string make, string model, int year, string color, decimal price, DateTimeOffset nowUtc)
        : base(id)
    {
        Make = make;
        Model = model;
        Year = year;
        Color = color;
        Price = price;
        Status = VehicleStatus.Available;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        Version = 1;
    }

    public string Make { get; private set; } = string.Empty;

    public string Model { get; private set; } = string.Empty;

    public int Year { get; private set; }

    public string Color { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public VehicleStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public int Version { get; private set; }

    public static Result<Vehicle> Create(
        string make,
        string model,
        int year,
        string color,
        decimal price,
        DateTimeOffset now)
    {
        var details = ValidateDetails(make, model, year, color, price, now);
        if (details.IsFailure)
        {
            return Result.Failure<Vehicle>(details.Error);
        }

        var value = details.Value!;
        return Result.Success(new Vehicle(Guid.CreateVersion7(), value.Make, value.Model, value.Year, value.Color, value.Price, value.NowUtc));
    }

    public Result UpdateDetails(
        string make,
        string model,
        int year,
        string color,
        decimal price,
        DateTimeOffset now)
    {
        if (Status == VehicleStatus.Sold)
        {
            return Result.Failure(VehicleErrors.CannotUpdateSoldVehicle);
        }

        var details = ValidateDetails(make, model, year, color, price, now);
        if (details.IsFailure)
        {
            return Result.Failure(details.Error);
        }

        var value = details.Value!;
        Make = value.Make;
        Model = value.Model;
        Year = value.Year;
        Color = value.Color;
        Price = value.Price;
        UpdatedAtUtc = value.NowUtc;
        Version++;

        return Result.Success();
    }

    public Result MarkAsSold(DateTimeOffset now)
    {
        if (Status != VehicleStatus.Available)
        {
            return Result.Failure(VehicleErrors.AlreadySold);
        }

        Status = VehicleStatus.Sold;
        UpdatedAtUtc = now.ToUniversalTime();
        Version++;

        return Result.Success();
    }

    private static Result<VehicleDetails> ValidateDetails(
        string make,
        string model,
        int year,
        string color,
        decimal price,
        DateTimeOffset now)
    {
        var normalizedMake = Normalize(make);
        if (normalizedMake is null || normalizedMake.Length > MakeMaxLength)
        {
            return Result.Failure<VehicleDetails>(VehicleErrors.InvalidMake);
        }

        var normalizedModel = Normalize(model);
        if (normalizedModel is null || normalizedModel.Length > ModelMaxLength)
        {
            return Result.Failure<VehicleDetails>(VehicleErrors.InvalidModel);
        }

        var nowUtc = now.ToUniversalTime();
        if (year < MinimumYear || year > nowUtc.Year + 1)
        {
            return Result.Failure<VehicleDetails>(VehicleErrors.InvalidYear);
        }

        var normalizedColor = Normalize(color);
        if (normalizedColor is null || normalizedColor.Length > ColorMaxLength)
        {
            return Result.Failure<VehicleDetails>(VehicleErrors.InvalidColor);
        }

        if (price <= 0 || decimal.Round(price, 2) != price)
        {
            return Result.Failure<VehicleDetails>(VehicleErrors.InvalidPrice);
        }

        return Result.Success(new VehicleDetails(normalizedMake, normalizedModel, year, normalizedColor, price, nowUtc));
    }

    private static string? Normalize(string? value)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue) ? null : normalizedValue;
    }

    private sealed record VehicleDetails(string Make, string Model, int Year, string Color, decimal Price, DateTimeOffset NowUtc);
}
