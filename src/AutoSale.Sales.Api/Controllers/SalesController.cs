using AutoSale.Api.Contracts.Common;
using AutoSale.Api.Contracts.Sales;
using AutoSale.Api.Extensions;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Common;
using AutoSale.Application.Sales;
using AutoSale.Application.Sales.ListSold;
using AutoSale.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoSale.Api.Controllers;

[ApiController]
[Route("api/v1/sales")]
public sealed class SalesController : ControllerBase
{
    [HttpGet("sold")]
    [AllowAnonymous]
    [ProducesResponseType<PagedResponse<SaleResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<SaleResponse>>> ListSoldAsync(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromServices] IQueryHandler<ListSoldVehiclesQuery, Result<PagedResult<SaleDto>>> handler,
        CancellationToken cancellationToken)
    {
        var query = new ListSoldVehiclesQuery(page ?? 1, pageSize ?? 20);
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToActionResult(this, pageResult => PagedResponse<SaleResponse>.From(pageResult, SaleResponse.FromDto));
    }
}
