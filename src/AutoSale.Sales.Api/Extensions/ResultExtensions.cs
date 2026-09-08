using AutoSale.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;

namespace AutoSale.Api.Extensions;

public static class ResultExtensions
{
    public static ActionResult<TResponse> ToActionResult<TValue, TResponse>(
        this Result<TValue> result,
        ControllerBase controller,
        Func<TValue, TResponse> map,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsFailure)
        {
            return ToProblem(result.Error, controller);
        }

        var response = map(result.Value!);
        return successStatusCode == StatusCodes.Status200OK
            ? controller.Ok(response)
            : new ObjectResult(response) { StatusCode = successStatusCode };
    }

    public static ObjectResult ToProblem(Error error, ControllerBase controller)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = error.Type.ToString(),
            Detail = error.Description,
            Instance = controller.HttpContext.Request.Path
        };
        problem.Extensions["code"] = error.Code;

        return new ObjectResult(problem) { StatusCode = statusCode };
    }
}
