using AutoSale.Api.Authorization;
using AutoSale.Api.Contracts.Common;
using AutoSale.Api.Contracts.Sales;
using AutoSale.Api.Contracts.Vehicles;
using AutoSale.Api.Extensions;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Common;
using AutoSale.Application.Sales;
using AutoSale.Application.Sales.Purchase;
using AutoSale.Application.Vehicles;
using AutoSale.Application.Vehicles.ListAvailable;
using AutoSale.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoSale.Api.Controllers;

[ApiController]
[Route("api/v1/vehicles")]
public sealed class VehiclesController : ControllerBase
{
    [HttpGet("available")]
    [AllowAnonymous]
    [ProducesResponseType<PagedResponse<VehicleResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<VehicleResponse>>> ListAvailableAsync(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromServices] IQueryHandler<ListAvailableVehiclesQuery, Result<PagedResult<VehicleDto>>> handler,
        CancellationToken cancellationToken)
    {
        var query = new ListAvailableVehiclesQuery(page ?? 1, pageSize ?? 20);
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToActionResult(this, pageResult => PagedResponse<VehicleResponse>.From(pageResult, VehicleResponse.FromDto));
    }

    [HttpPost("{id:guid}/purchase")]
    [Authorize]
    [ProducesResponseType<SaleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaleResponse>> PurchaseAsync(
        Guid id,
        [FromBody] PurchaseVehicleRequest? request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromServices] ICommandHandler<PurchaseVehicleCommand, Result<SaleDto>> handler,
        CancellationToken cancellationToken)
    {
        var command = new PurchaseVehicleCommand(id, idempotencyKey ?? request?.IdempotencyKey);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, SaleResponse.FromDto, StatusCodes.Status201Created);
    }
}
