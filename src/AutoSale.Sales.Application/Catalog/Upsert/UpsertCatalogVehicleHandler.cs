using AutoSale.Application.Abstractions.Clock;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Domain.Catalog;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Catalog.Upsert;

public sealed class UpsertCatalogVehicleHandler :
    ICommandHandler<UpsertCatalogVehicleCommand, Result<CatalogUpsertResult>>
{
    private readonly ICatalogRepository _catalogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpsertCatalogVehicleHandler(ICatalogRepository catalogRepository, IUnitOfWork unitOfWork, IClock clock)
    {
        _catalogRepository = catalogRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<CatalogUpsertResult>> HandleAsync(UpsertCatalogVehicleCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Snapshot is null || command.VehicleId == Guid.Empty || command.Snapshot.Id != command.VehicleId)
        {
            return Result.Failure<CatalogUpsertResult>(VehicleCatalogErrors.InvalidSnapshot);
        }

        var update = await CatalogUpdater.ApplyAsync(
            _catalogRepository, command.Snapshot, _clock.UtcNow, cancellationToken);
        if (update.IsFailure)
        {
            return Result.Failure<CatalogUpsertResult>(update.Error);
        }

        if (update.Value)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(new CatalogUpsertResult(update.Value));
    }
}

public sealed record CatalogUpsertResult(bool Applied);
