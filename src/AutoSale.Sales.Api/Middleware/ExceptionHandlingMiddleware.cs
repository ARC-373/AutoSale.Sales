using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoSale.Api.Middleware;

public sealed class ExceptionHandlingMiddleware : IExceptionHandler
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, code, title, detail, logLevel) = exception switch
        {
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "data.concurrency_conflict", "Conflict", "The resource was modified by another request.", LogLevel.Warning),
            DbUpdateException => (StatusCodes.Status409Conflict, "data.constraint_conflict", "Conflict", "The request conflicts with the current data state.", LogLevel.Warning),
            _ => (StatusCodes.Status500InternalServerError, "server.unexpected", "Unexpected error", "An unexpected error occurred.", LogLevel.Error)
        };

        _logger.Log(logLevel, exception, "Request failed with error code {ErrorCode}", code);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["code"] = code;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
