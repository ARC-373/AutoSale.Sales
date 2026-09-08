using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Common;

public static class ApplicationErrors
{
    public static readonly Error Unauthenticated = new("auth.unauthenticated", "An authenticated user is required to perform this operation.", ErrorType.Unauthorized);
    public static readonly Error InvalidVehicleId = new("vehicle.id.invalid", "Vehicle id must be specified.", ErrorType.Validation);
    public static readonly Error VehicleNotFound = new("vehicle.not_found", "The requested vehicle was not found.", ErrorType.NotFound);
    public static readonly Error InvalidPage = new("paging.page.invalid", "Page must be greater than zero.", ErrorType.Validation);
    public static readonly Error InvalidPageSize = new("paging.page_size.invalid", "Page size must be between 1 and 100.", ErrorType.Validation);
}
