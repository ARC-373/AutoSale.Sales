using AutoSale.Application.Abstractions.Authentication;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Sales.GetById;

public sealed class GetSaleByIdHandler : IQueryHandler<GetSaleByIdQuery, Result<SaleDto>>
{
    private readonly ISaleRepository _saleRepository;
    private readonly ICurrentUser _currentUser;

    public GetSaleByIdHandler(ISaleRepository saleRepository, ICurrentUser currentUser)
    {
        _saleRepository = saleRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<SaleDto>> HandleAsync(GetSaleByIdQuery query, CancellationToken cancellationToken)
    {
        if (query.SaleId == Guid.Empty)
        {
            return Result.Failure<SaleDto>(ApplicationErrors.SaleNotFound);
        }

        var sale = await _saleRepository.GetByIdAsync(query.SaleId, cancellationToken);
        if (sale is null || (!_currentUser.IsAdmin &&
            !string.Equals(sale.BuyerSubject, _currentUser.Subject, StringComparison.Ordinal)))
        {
            return Result.Failure<SaleDto>(ApplicationErrors.SaleNotFound);
        }

        return Result.Success(SaleDto.FromDomain(sale));
    }
}
