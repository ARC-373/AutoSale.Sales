using System.Data;
using AutoSale.Application.Abstractions.Clock;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.Application.Sales;
using AutoSale.Domain.Payments;
using AutoSale.Domain.Sales;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Payments.ReceiveResult;

public sealed class ReceivePaymentResultHandler :
    ICommandHandler<ReceivePaymentResultCommand, Result<PaymentResultReceipt>>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IPaymentCallbackRepository _callbackRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReceivePaymentResultHandler(ISaleRepository saleRepository,
        IPaymentCallbackRepository callbackRepository, IUnitOfWork unitOfWork, IClock clock)
    {
        _saleRepository = saleRepository;
        _callbackRepository = callbackRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<PaymentResultReceipt>> HandleAsync(ReceivePaymentResultCommand command,
        CancellationToken cancellationToken)
    {
        var validation = Validate(command);
        if (validation.IsFailure)
        {
            return Result.Failure<PaymentResultReceipt>(validation.Error);
        }

        return await _unitOfWork.ExecuteInTransactionAsync(IsolationLevel.ReadCommitted, async ct =>
        {
            var sale = await _saleRepository.GetByPaymentCodeForUpdateAsync(command.PaymentCode, ct);
            if (sale is null)
            {
                return Result.Failure<PaymentResultReceipt>(ApplicationErrors.PaymentNotFound);
            }

            var eventCallback = await _callbackRepository.GetByEventIdAsync(command.EventId, ct);
            if (eventCallback is not null)
            {
                if (eventCallback.PaymentCode != command.PaymentCode)
                {
                    return Result.Failure<PaymentResultReceipt>(ApplicationErrors.PaymentEventConflict);
                }

                return eventCallback.Outcome == command.Outcome
                    ? Result.Success(new PaymentResultReceipt(SaleDto.FromDomain(sale), true))
                    : Result.Failure<PaymentResultReceipt>(ApplicationErrors.PaymentResultConflict);
            }

            var paymentCallback = await _callbackRepository.GetByPaymentCodeAsync(command.PaymentCode, ct);
            if (paymentCallback is not null)
            {
                return paymentCallback.Outcome == command.Outcome
                    ? Result.Success(new PaymentResultReceipt(SaleDto.FromDomain(sale), true))
                    : Result.Failure<PaymentResultReceipt>(ApplicationErrors.PaymentResultConflict);
            }

            if (sale.State != SaleStatus.AwaitingPayment)
            {
                return Result.Failure<PaymentResultReceipt>(ApplicationErrors.PaymentStateConflict);
            }

            var callbackResult = PaymentCallback.Create(command.PaymentCode, command.EventId, command.Outcome,
                command.OccurredAtUtc, _clock.UtcNow);
            if (callbackResult.IsFailure)
            {
                return Result.Failure<PaymentResultReceipt>(callbackResult.Error);
            }

            var recordPayment = sale.RecordPayment(command.Outcome, command.OccurredAtUtc);
            if (recordPayment.IsFailure)
            {
                return Result.Failure<PaymentResultReceipt>(recordPayment.Error);
            }

            await _callbackRepository.AddAsync(callbackResult.Value!, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result.Success(new PaymentResultReceipt(SaleDto.FromDomain(sale), false));
        }, cancellationToken);
    }

    private static Result Validate(ReceivePaymentResultCommand command)
    {
        if (command.PaymentCode == Guid.Empty)
        {
            return Result.Failure(PaymentCallbackErrors.InvalidPaymentCode);
        }

        if (command.EventId == Guid.Empty)
        {
            return Result.Failure(PaymentCallbackErrors.InvalidEventId);
        }

        if (!Enum.IsDefined(command.Outcome))
        {
            return Result.Failure(PaymentCallbackErrors.InvalidOutcome);
        }

        return command.OccurredAtUtc == default
            ? Result.Failure(PaymentCallbackErrors.InvalidTimestamp)
            : Result.Success();
    }
}

public sealed record PaymentResultReceipt(SaleDto Sale, bool IsDuplicate);
