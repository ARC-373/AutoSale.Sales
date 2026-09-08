using AutoSale.Api.Extensions;
using AutoSale.SharedKernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AutoSale.Sales.Api.UnitTests.Extensions;

public sealed class ResultExtensionsTests
{
    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.Failure, 500)]
    public void ToProblem_MapsErrorAndIncludesTraceId(ErrorType type, int status)
    {
        var controller = new TestController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.HttpContext.TraceIdentifier = "trace-test";

        var result = ResultExtensions.ToProblem(new Error("test.code", "detail", type), controller);

        Assert.Equal(status, result.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal("test.code", problem.Extensions["code"]);
        Assert.Equal("trace-test", problem.Extensions["traceId"]);
    }

    private sealed class TestController : ControllerBase;
}
