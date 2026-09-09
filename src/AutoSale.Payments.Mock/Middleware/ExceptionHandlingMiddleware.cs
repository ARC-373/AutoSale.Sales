using AutoSale.Payments.Mock.Payments;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AutoSale.Payments.Mock.Middleware;

public sealed class ExceptionHandlingMiddleware(
    IProblemDetailsService problemDetailsService,
    ILogger<ExceptionHandlingMiddleware> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, code, title) = exception switch
        {
            PaymentNotFoundException => (StatusCodes.Status404NotFound, "payment_not_found", "Not found"),
            PaymentConflictException conflict => (StatusCodes.Status409Conflict, conflict.Code, "Conflict"),
            ArgumentException => (StatusCodes.Status400BadRequest, "request.invalid", "Validation"),
            _ => (StatusCodes.Status500InternalServerError, "server_error", "Server error")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled request exception");
        }

        context.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status == 500 ? "An unexpected error occurred." : exception.Message,
                Instance = context.Request.Path,
                Extensions =
                {
                    ["code"] = code,
                    ["traceId"] = context.TraceIdentifier
                }
            }
        });
    }
}
