using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Sales.List;

public sealed class ListSalesHandler : IQueryHandler<ListSalesQuery, Result<PagedResult<SaleDto>>>
{
    private readonly ISaleRepository _saleRepository;

    public ListSalesHandler(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    public async Task<Result<PagedResult<SaleDto>>> HandleAsync(ListSalesQuery query,
        CancellationToken cancellationToken)
    {
        var validation = PagingValidator.Validate(query.Page, query.PageSize);
        if (validation.IsFailure)
        {
            return Result.Failure<PagedResult<SaleDto>>(validation.Error);
        }

        return Result.Success(await _saleRepository.ListAsync(query.Page, query.PageSize, cancellationToken));
    }
}
