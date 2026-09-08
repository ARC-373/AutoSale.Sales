using AutoSale.SharedKernel.Results;

namespace AutoSale.Domain.Catalog;

public static class VehicleCatalogErrors
{
    public static readonly Error InvalidSnapshot = new(
        "vehicle_catalog.snapshot.invalid",
        "Snapshot must be specified and belong to the catalog vehicle.",
        ErrorType.Validation);

    public static readonly Error InvalidSynchronizedAt = new(
        "vehicle_catalog.synchronized_at.invalid",
        "Synchronization timestamp must be specified.",
        ErrorType.Validation);

    public static readonly Error SourceVersionConflict = new(
        "vehicle_catalog.source_version.conflict",
        "The same source version cannot contain different vehicle data.",
        ErrorType.Conflict);
}
