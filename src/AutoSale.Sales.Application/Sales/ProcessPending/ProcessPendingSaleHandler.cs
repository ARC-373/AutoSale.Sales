using AutoSale.Application.Abstractions.Clock;
using AutoSale.Application.Abstractions.Integrations;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Catalog;
using AutoSale.Application.Common;
using AutoSale.Domain.Sales;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Sales.ProcessPending;

public sealed class ProcessPendingSaleHandler :
    ICommandHandler<ProcessPendingSaleCommand, Result<ProcessPendingSaleResult>>
{
    private readonly ISaleRepository _saleRepository;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IVehiclesClient _vehiclesClient;
    private readonly IPaymentProcessorClient _paymentClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ProcessPendingSaleHandler(ISaleRepository saleRepository, ICatalogRepository catalogRepository,
        IVehiclesClient vehiclesClient, IPaymentProcessorClient paymentClient, IUnitOfWork unitOfWork, IClock clock)
    {
        _saleRepository = saleRepository;
        _catalogRepository = catalogRepository;
        _vehiclesClient = vehiclesClient;
        _paymentClient = paymentClient;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<ProcessPendingSaleResult>> HandleAsync(ProcessPendingSaleCommand command,
        CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdAsync(command.SaleId, cancellationToken);
        if (sale is null)
        {
            return Result.Failure<ProcessPendingSaleResult>(ApplicationErrors.SaleNotFound);
        }

        return sale.State switch
        {
            SaleStatus.Reserving => await ReserveVehicleAsync(sale, cancellationToken),
            SaleStatus.AwaitingPayment when !sale.PaymentRegisteredAtUtc.HasValue =>
                await RegisterPaymentAsync(sale, cancellationToken),
            SaleStatus.ConfirmingVehicle => await ConfirmVehicleAsync(sale, cancellationToken),
            SaleStatus.CancellingVehicle => await ReleaseVehicleAsync(sale, cancellationToken),
            _ => Result.Success(ToResult(sale, false, false, null))
        };
    }

    private async Task<Result<ProcessPendingSaleResult>> ReserveVehicleAsync(Sale sale,
        CancellationToken cancellationToken)
    {
        var response = await _vehiclesClient.ReserveAsync(
            sale.VehicleId, sale.Id, sale.ExpectedPrice, cancellationToken);
        if (!response.IsSuccess)
        {
            if (response.FailureKind is IntegrationFailureKind.NotFound or IntegrationFailureKind.Conflict)
            {
                var rejected = sale.RejectReservation(NormalizeFailureCode(response.ErrorCode));
                return rejected.IsFailure
                    ? Result.Failure<ProcessPendingSaleResult>(rejected.Error)
                    : await SaveSuccessAsync(sale, cancellationToken);
            }

            return await ScheduleRetryAsync(sale, response, cancellationToken);
        }

        var catalogUpdate = await CatalogUpdater.ApplyAsync(
            _catalogRepository, response.Value!, _clock.UtcNow, cancellationToken);
        if (catalogUpdate.IsFailure)
        {
            return await ScheduleRetryAsync(sale, catalogUpdate.Error.Code,
                catalogUpdate.Error.Description, null, cancellationToken);
        }

        var accepted = sale.AcceptReservation(response.Value!);
        if (accepted.IsFailure)
        {
            return Result.Failure<ProcessPendingSaleResult>(accepted.Error);
        }

        return await SaveSuccessAsync(sale, cancellationToken);
    }

    private async Task<Result<ProcessPendingSaleResult>> RegisterPaymentAsync(Sale sale,
        CancellationToken cancellationToken)
    {
        if (!sale.SalePrice.HasValue)
        {
            return await ScheduleRetryAsync(sale, "sale_price_missing",
                "Reserved sale has no confirmed price.", null, cancellationToken);
        }

        var response = await _paymentClient.CreatePaymentAsync(
            sale.PaymentCode, sale.Id, sale.SalePrice.Value, cancellationToken);
        if (!response.IsSuccess)
        {
            return await ScheduleRetryAsync(sale, response, cancellationToken);
        }

        var registered = sale.RegisterPayment(_clock.UtcNow);
        if (registered.IsFailure)
        {
            return Result.Failure<ProcessPendingSaleResult>(registered.Error);
        }

        return await SaveSuccessAsync(sale, cancellationToken);
    }

    private async Task<Result<ProcessPendingSaleResult>> ConfirmVehicleAsync(Sale sale,
        CancellationToken cancellationToken)
    {
        var response = await _vehiclesClient.ConfirmReservationAsync(
            sale.VehicleId, sale.Id, cancellationToken);
        if (!response.IsSuccess)
        {
            return await ScheduleRetryAsync(sale, response, cancellationToken);
        }

        if (response.Value!.Status != VehicleStatus.Sold)
        {
            return await ScheduleRetryAsync(sale, "invalid_confirmation_snapshot",
                "Vehicle confirmation did not return Sold status.", null, cancellationToken);
        }

        var catalogUpdate = await CatalogUpdater.ApplyAsync(
            _catalogRepository, response.Value, _clock.UtcNow, cancellationToken);
        if (catalogUpdate.IsFailure)
        {
            return await ScheduleRetryAsync(sale, catalogUpdate.Error.Code,
                catalogUpdate.Error.Description, null, cancellationToken);
        }

        var completed = sale.Complete(_clock.UtcNow);
        if (completed.IsFailure)
        {
            return Result.Failure<ProcessPendingSaleResult>(completed.Error);
        }

        return await SaveSuccessAsync(sale, cancellationToken);
    }

    private async Task<Result<ProcessPendingSaleResult>> ReleaseVehicleAsync(Sale sale,
        CancellationToken cancellationToken)
    {
        var response = await _vehiclesClient.ReleaseReservationAsync(
            sale.VehicleId, sale.Id, cancellationToken);
        if (!response.IsSuccess)
        {
            return await ScheduleRetryAsync(sale, response, cancellationToken);
        }

        if (response.Value!.Status != VehicleStatus.Available)
        {
            return await ScheduleRetryAsync(sale, "invalid_release_snapshot",
                "Vehicle release did not return Available status.", null, cancellationToken);
        }

        var catalogUpdate = await CatalogUpdater.ApplyAsync(
            _catalogRepository, response.Value, _clock.UtcNow, cancellationToken);
        if (catalogUpdate.IsFailure)
        {
            return await ScheduleRetryAsync(sale, catalogUpdate.Error.Code,
                catalogUpdate.Error.Description, null, cancellationToken);
        }

        var cancelled = sale.Cancel(_clock.UtcNow);
        if (cancelled.IsFailure)
        {
            return Result.Failure<ProcessPendingSaleResult>(cancelled.Error);
        }

        return await SaveSuccessAsync(sale, cancellationToken);
    }

    private Task<Result<ProcessPendingSaleResult>> ScheduleRetryAsync<TValue>(Sale sale,
        IntegrationResult<TValue> integrationResult, CancellationToken cancellationToken) =>
        ScheduleRetryAsync(sale, integrationResult.ErrorCode ?? "integration_failure",
            integrationResult.SanitizedError ?? "External integration failed.",
            integrationResult.RetryAfter, cancellationToken);

    private async Task<Result<ProcessPendingSaleResult>> ScheduleRetryAsync(Sale sale, string errorCode,
        string error, TimeSpan? retryAfter, CancellationToken cancellationToken)
    {
        var sanitizedError = $"{errorCode}: {error}";
        if (sanitizedError.Length > 2_000)
        {
            sanitizedError = sanitizedError[..2_000];
        }

        var retryAt = _clock.UtcNow.Add(RetrySchedule.GetDelay(sale.Attempts, retryAfter));
        var scheduled = sale.RecordProcessingFailure(sanitizedError, retryAt);
        if (scheduled.IsFailure)
        {
            return Result.Failure<ProcessPendingSaleResult>(scheduled.Error);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(ToResult(sale, false, true, errorCode));
    }

    private async Task<Result<ProcessPendingSaleResult>> SaveSuccessAsync(Sale sale,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(ToResult(sale, true, false, null));
    }

    private static ProcessPendingSaleResult ToResult(Sale sale, bool changed, bool retry, string? error) =>
        new(sale.Id, sale.State, changed, retry, error);

    private static string NormalizeFailureCode(string? errorCode)
    {
        var code = string.IsNullOrWhiteSpace(errorCode) ? "reservation_rejected" : errorCode.Trim();
        return code.Length <= 100 ? code : code[..100];
    }
}
