using System.Data;
using AutoSale.Application.Abstractions.Authentication;
using AutoSale.Application.Abstractions.Clock;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.Domain.Sales;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Sales.Purchase;

public sealed class PurchaseVehicleHandler : ICommandHandler<PurchaseVehicleCommand, Result<SaleDto>>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public PurchaseVehicleHandler(ISaleRepository saleRepository, IUnitOfWork unitOfWork,
        ICurrentUser currentUser, IClock clock)
    {
        _saleRepository = saleRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<SaleDto>> HandleAsync(PurchaseVehicleCommand command,
        CancellationToken cancellationToken)
    {
        var validation = PurchaseVehicleValidator.Validate(command);
        if (validation.IsFailure)
        {
            return Result.Failure<SaleDto>(validation.Error);
        }

        var buyerSubject = _currentUser.Subject?.Trim();
        if (string.IsNullOrWhiteSpace(buyerSubject))
        {
            return Result.Failure<SaleDto>(ApplicationErrors.Unauthenticated);
        }

        var idempotencyKey = command.IdempotencyKey.Trim();
        var requestHash = PurchaseRequestHash.Create(command.VehicleId, validation.Value!, command.ExpectedPrice);

        return await _unitOfWork.ExecuteInTransactionAsync(IsolationLevel.ReadCommitted, async ct =>
        {
            var existing = await _saleRepository.GetByBuyerAndIdempotencyKeyAsync(
                buyerSubject, idempotencyKey, ct);

            if (existing is not null)
            {
                return string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal)
                    ? Result.Success(SaleDto.FromDomain(existing))
                    : Result.Failure<SaleDto>(ApplicationErrors.IdempotencyConflict);
            }

            var saleResult = Sale.Create(command.VehicleId, buyerSubject, validation.Value!, command.ExpectedPrice,
                idempotencyKey, requestHash, _clock.UtcNow);
            if (saleResult.IsFailure)
            {
                return Result.Failure<SaleDto>(saleResult.Error);
            }

            var sale = saleResult.Value!;
            await _saleRepository.AddAsync(sale, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result.Success(SaleDto.FromDomain(sale));
        }, cancellationToken);
    }
}
