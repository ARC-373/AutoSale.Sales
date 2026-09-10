using AutoSale.Api.Authorization;
using AutoSale.Api.Contracts.Payments;
using AutoSale.Api.Contracts.Sales;
using AutoSale.Api.Extensions;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Payments.ReceiveResult;
using AutoSale.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoSale.Api.Controllers;

[ApiController]
[Route("api/v1/payments/webhook")]
public sealed class PaymentsWebhookController : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.PaymentWebhook)]
    [ProducesResponseType<SaleResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<SaleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaleResponse>> ReceiveAsync(
        [FromBody] PaymentWebhookRequest request,
        [FromServices] ICommandHandler<ReceivePaymentResultCommand,
            Result<PaymentResultReceipt>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ReceivePaymentResultCommand(
            request.PaymentCode, request.EventId, request.Status, request.OccurredAtUtc), cancellationToken);
        if (result.IsFailure)
        {
            return ResultExtensions.ToProblem(result.Error, this);
        }

        var response = SaleResponse.FromDto(result.Value!.Sale);
        return result.Value.IsDuplicate
            ? Ok(response)
            : StatusCode(StatusCodes.Status202Accepted, response);
    }
}
