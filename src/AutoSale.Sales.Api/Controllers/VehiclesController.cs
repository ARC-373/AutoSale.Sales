using AutoSale.Api.Contracts.Common;
using AutoSale.Api.Contracts.Sales;
using AutoSale.Api.Contracts.Vehicles;
using AutoSale.Api.Extensions;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Catalog;
using AutoSale.Application.Catalog.ListAvailable;
using AutoSale.Application.Common;
using AutoSale.Application.Sales;
using AutoSale.Application.Sales.Purchase;
using AutoSale.Domain.Sales;
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
    [ProducesResponseType<PagedResponse<AvailableVehicleResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<AvailableVehicleResponse>>> ListAvailableAsync(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromServices] IQueryHandler<ListAvailableVehiclesQuery,
            Result<PagedResult<AvailableVehicleDto>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListAvailableVehiclesQuery(page ?? 1, pageSize ?? 20), cancellationToken);
        return result.ToActionResult(this, pageResult =>
            PagedResponse<AvailableVehicleResponse>.From(pageResult, AvailableVehicleResponse.FromDto));
    }

    [HttpPost("{id:guid}/purchase")]
    [Authorize]
    [ProducesResponseType<SaleResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<SaleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaleResponse>> PurchaseAsync(
        Guid id,
        [FromBody] PurchaseVehicleRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromServices] ICommandHandler<PurchaseVehicleCommand, Result<SaleDto>> handler,
        CancellationToken cancellationToken)
    {
        var command = new PurchaseVehicleCommand(
            id, request.BuyerCpf, request.ExpectedPrice, idempotencyKey ?? string.Empty);
        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            return ResultExtensions.ToProblem(result.Error, this);
        }

        var response = SaleResponse.FromDto(result.Value!);
        var location = $"/api/v1/sales/{response.Id:D}";
        Response.Headers.Location = location;
        return result.Value!.Status is SaleStatus.Completed or SaleStatus.Cancelled or SaleStatus.Rejected
            ? Ok(response)
            : StatusCode(StatusCodes.Status202Accepted, response);
    }
}
