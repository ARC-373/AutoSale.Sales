using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Domain.Catalog;
using AutoSale.Domain.Sales;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Catalog;

public static class CatalogUpdater
{
    public static async Task<Result<bool>> ApplyAsync(ICatalogRepository repository, VehicleSnapshot snapshot,
        DateTimeOffset synchronizedAtUtc, CancellationToken cancellationToken)
    {
        var current = await repository.GetByIdAsync(snapshot.Id, cancellationToken);
        if (current is null)
        {
            var creation = VehicleCatalog.Create(snapshot, synchronizedAtUtc);
            if (creation.IsFailure)
            {
                return Result.Failure<bool>(creation.Error);
            }

            await repository.AddAsync(creation.Value!, cancellationToken);
            return Result.Success(true);
        }

        var previousVersion = current.SourceVersion;
        var result = current.Apply(snapshot, synchronizedAtUtc);
        return result.IsFailure
            ? Result.Failure<bool>(result.Error)
            : Result.Success(snapshot.Version > previousVersion);
    }
}
