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
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public PurchaseVehicleHandler(
        IVehicleRepository vehicleRepository,
        ISaleRepository saleRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _vehicleRepository = vehicleRepository;
        _saleRepository = saleRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<SaleDto>> HandleAsync(PurchaseVehicleCommand command, CancellationToken cancellationToken)
    {
        var validation = PurchaseVehicleValidator.Validate(command);
        if (validation.IsFailure)
        {
            return Result.Failure<SaleDto>(validation.Error);
        }

        var buyerSubject = _currentUser.Subject;
        if (string.IsNullOrWhiteSpace(buyerSubject))
        {
            return Result.Failure<SaleDto>(ApplicationErrors.Unauthenticated);
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var vehicle = await _vehicleRepository.GetByIdForPurchaseAsync(command.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            return Result.Failure<SaleDto>(ApplicationErrors.VehicleNotFound);
        }

        var now = _clock.UtcNow;
        var saleResult = Sale.Create(vehicle.Id, buyerSubject, vehicle.Price, now, command.IdempotencyKey);
        if (saleResult.IsFailure)
        {
            return Result.Failure<SaleDto>(saleResult.Error);
        }

        var markAsSold = vehicle.MarkAsSold(now);
        if (markAsSold.IsFailure)
        {
            return Result.Failure<SaleDto>(markAsSold.Error);
        }

        var sale = saleResult.Value!;
        await _saleRepository.AddAsync(sale, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(SaleDto.FromDomain(sale));
    }
}
