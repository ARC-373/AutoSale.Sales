using AutoSale.Api.Contracts.Common;
using AutoSale.Api.Contracts.Sales;
using AutoSale.Api.Extensions;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Common;
using AutoSale.Application.Sales;
using AutoSale.Application.Sales.GetById;
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
    [ProducesResponseType<PagedResponse<SoldVehicleResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<SoldVehicleResponse>>> ListSoldAsync(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromServices] IQueryHandler<ListSoldVehiclesQuery,
            Result<PagedResult<SoldVehicleDto>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListSoldVehiclesQuery(page ?? 1, pageSize ?? 20), cancellationToken);
        return result.ToActionResult(this, pageResult =>
            PagedResponse<SoldVehicleResponse>.From(pageResult, SoldVehicleResponse.FromDto));
    }

    [HttpGet("{saleId:guid}")]
    [Authorize]
    [ProducesResponseType<SaleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleResponse>> GetByIdAsync(
        Guid saleId,
        [FromServices] IQueryHandler<GetSaleByIdQuery, Result<SaleDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetSaleByIdQuery(saleId), cancellationToken);
        return result.ToActionResult(this, SaleResponse.FromDto);
    }
}
