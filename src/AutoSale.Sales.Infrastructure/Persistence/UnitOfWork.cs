using System.Data;
using AutoSale.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AutoSale.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AutoSaleDbContext _dbContext;

    public UnitOfWork(AutoSaleDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ITransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
    {
        var transaction = await _dbContext.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
        return new EfCoreTransaction(transaction);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed class EfCoreTransaction : ITransaction
    {
        private readonly IDbContextTransaction _transaction;

        public EfCoreTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public Task CommitAsync(CancellationToken cancellationToken) => _transaction.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync() => _transaction.DisposeAsync();
    }
}
