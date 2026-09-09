using AutoSale.Payments.Mock.Contracts;
using AutoSale.Payments.Mock.Payments;
using Microsoft.AspNetCore.Mvc;

namespace AutoSale.Payments.Mock.Controllers;

public sealed class PaymentsController(PaymentService paymentService) : Controller
{
    [HttpGet("/")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var payments = await paymentService.ListPendingAsync(cancellationToken);
        return View(payments.Select(PaymentResponse.From).ToList());
    }

    [HttpPost("/payments/{paymentCode:guid}/approve")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Approve(Guid paymentCode, CancellationToken cancellationToken) =>
        Decide(paymentCode, PaymentStatus.Paid, cancellationToken);

    [HttpPost("/payments/{paymentCode:guid}/cancel")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Cancel(Guid paymentCode, CancellationToken cancellationToken) =>
        Decide(paymentCode, PaymentStatus.Cancelled, cancellationToken);

    private async Task<IActionResult> Decide(Guid paymentCode, PaymentStatus decision,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await paymentService.DecideAsync(paymentCode, decision, cancellationToken);
            TempData["Message"] = result.Changed
                ? "Decisão registrada. O callback para Vendas será enviado em seguida."
                : "Essa decisão já havia sido registrada.";
        }
        catch (Exception exception) when (exception is PaymentNotFoundException or PaymentConflictException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
