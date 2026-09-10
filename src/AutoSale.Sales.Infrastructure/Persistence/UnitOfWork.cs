using System.Data;
using AutoSale.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoSale.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly SalesDbContext _dbContext;

    public UnitOfWork(SalesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(IsolationLevel isolationLevel,
        Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                isolationLevel, cancellationToken);
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

}
