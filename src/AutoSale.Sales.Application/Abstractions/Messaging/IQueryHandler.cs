namespace AutoSale.Application.Abstractions.Messaging;

public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
