using AutoSale.Payments.Mock.Authentication;
using AutoSale.Payments.Mock.Contracts;
using AutoSale.Payments.Mock.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoSale.Payments.Mock.Controllers;

[ApiController]
[Route("api/v1/payments")]
public sealed class PaymentsApiController(PaymentService paymentService) : ControllerBase
{
    [HttpPut("{paymentCode:guid}")]
    [Authorize(Policy = ApiKeySchemes.SalesPolicy)]
    public async Task<ActionResult<PaymentResponse>> Create(Guid paymentCode, CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await paymentService.CreateAsync(paymentCode, request.SaleId, request.Amount,
            request.Currency, cancellationToken);
        var response = PaymentResponse.From(result.Payment);
        return result.Created
            ? CreatedAtAction(nameof(Get), new { paymentCode }, response)
            : Ok(response);
    }

    [HttpGet("{paymentCode:guid}")]
    [Authorize(Policy = ApiKeySchemes.OperatorPolicy)]
    public async Task<ActionResult<PaymentResponse>> Get(Guid paymentCode, CancellationToken cancellationToken)
    {
        var payment = await paymentService.GetAsync(paymentCode, cancellationToken)
            ?? throw new PaymentNotFoundException(paymentCode);
        return Ok(PaymentResponse.From(payment));
    }

    [HttpPost("{paymentCode:guid}/approve")]
    [Authorize(Policy = ApiKeySchemes.OperatorPolicy)]
    public Task<ActionResult<PaymentResponse>> Approve(Guid paymentCode, CancellationToken cancellationToken) =>
        Decide(paymentCode, PaymentStatus.Paid, cancellationToken);

    [HttpPost("{paymentCode:guid}/reject")]
    [Authorize(Policy = ApiKeySchemes.OperatorPolicy)]
    public Task<ActionResult<PaymentResponse>> Reject(Guid paymentCode, CancellationToken cancellationToken) =>
        Decide(paymentCode, PaymentStatus.Cancelled, cancellationToken);

    [HttpPost("{paymentCode:guid}/retry-callback")]
    [Authorize(Policy = ApiKeySchemes.OperatorPolicy)]
    public async Task<ActionResult<PaymentResponse>> RetryCallback(Guid paymentCode,
        CancellationToken cancellationToken)
    {
        var payment = await paymentService.RetryCallbackAsync(paymentCode, cancellationToken);
        return AcceptedAtAction(nameof(Get), new { paymentCode }, PaymentResponse.From(payment));
    }

    private async Task<ActionResult<PaymentResponse>> Decide(Guid paymentCode, PaymentStatus status,
        CancellationToken cancellationToken)
    {
        var result = await paymentService.DecideAsync(paymentCode, status, cancellationToken);
        var response = PaymentResponse.From(result.Payment);
        return result.Changed
            ? AcceptedAtAction(nameof(Get), new { paymentCode }, response)
            : Ok(response);
    }
}
