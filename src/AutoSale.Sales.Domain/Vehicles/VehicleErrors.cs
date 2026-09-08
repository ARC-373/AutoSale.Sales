using AutoSale.SharedKernel.Results;

namespace AutoSale.Domain.Vehicles;

public static class VehicleErrors
{
    public static readonly Error InvalidMake = new("vehicle.make.invalid", "Make must contain between 1 and 120 characters.", ErrorType.Validation);
    public static readonly Error InvalidModel = new("vehicle.model.invalid", "Model must contain between 1 and 120 characters.", ErrorType.Validation);
    public static readonly Error InvalidYear = new("vehicle.year.invalid", "Year must be between 1886 and the next calendar year.", ErrorType.Validation);
    public static readonly Error InvalidColor = new("vehicle.color.invalid", "Color must contain between 1 and 50 characters.", ErrorType.Validation);
    public static readonly Error InvalidPrice = new("vehicle.price.invalid", "Price must be positive and have at most two decimal places.", ErrorType.Validation);
    public static readonly Error CannotUpdateSoldVehicle = new("vehicle.sold.cannot_update", "A sold vehicle cannot be updated.", ErrorType.Conflict);
    public static readonly Error AlreadySold = new("vehicle.already_sold", "The vehicle has already been sold.", ErrorType.Conflict);
}
