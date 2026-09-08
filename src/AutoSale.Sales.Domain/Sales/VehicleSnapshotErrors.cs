using AutoSale.SharedKernel.Results;

namespace AutoSale.Domain.Sales;

public static class VehicleSnapshotErrors
{
    public static readonly Error InvalidId = new("vehicle_snapshot.id.invalid", "Vehicle id must be specified.", ErrorType.Validation);
    public static readonly Error InvalidMake = new("vehicle_snapshot.make.invalid", "Make must contain between 1 and 120 characters.", ErrorType.Validation);
    public static readonly Error InvalidModel = new("vehicle_snapshot.model.invalid", "Model must contain between 1 and 120 characters.", ErrorType.Validation);
    public static readonly Error InvalidYear = new("vehicle_snapshot.year.invalid", "Year must be 1886 or later.", ErrorType.Validation);
    public static readonly Error InvalidColor = new("vehicle_snapshot.color.invalid", "Color must contain between 1 and 50 characters.", ErrorType.Validation);
    public static readonly Error InvalidPrice = new("vehicle_snapshot.price.invalid", "Price must be positive and have at most two decimal places.", ErrorType.Validation);
    public static readonly Error InvalidStatus = new("vehicle_snapshot.status.invalid", "Status must be Available, Reserved, or Sold.", ErrorType.Validation);
    public static readonly Error InvalidVersion = new("vehicle_snapshot.version.invalid", "Version must be greater than zero.", ErrorType.Validation);
    public static readonly Error InvalidUpdatedAt = new("vehicle_snapshot.updated_at.invalid", "Updated timestamp must be specified.", ErrorType.Validation);
}
