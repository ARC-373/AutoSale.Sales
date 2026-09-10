using AutoSale.Application.Abstractions.Messaging;

namespace AutoSale.Sales.Api.UnitTests;

internal sealed class FakeCommandHandler<TCommand, TResult>(Func<TCommand, TResult> handle) :
    ICommandHandler<TCommand, TResult>
{
    public Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(handle(command));
}

internal sealed class FakeQueryHandler<TQuery, TResult>(Func<TQuery, TResult> handle) :
    IQueryHandler<TQuery, TResult>
{
    public Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(handle(query));
}
