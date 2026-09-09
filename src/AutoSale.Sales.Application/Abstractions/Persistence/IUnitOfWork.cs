using System.Data;

namespace AutoSale.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task<TResult> ExecuteInTransactionAsync<TResult>(IsolationLevel isolationLevel,
        Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
