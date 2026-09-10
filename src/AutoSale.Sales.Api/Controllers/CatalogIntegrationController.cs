using AutoSale.Api.Authorization;
using AutoSale.Api.Contracts.Catalog;
using AutoSale.Api.Extensions;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Catalog.Upsert;
using AutoSale.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoSale.Api.Controllers;

[ApiController]
[Route("internal/v1/catalog/vehicles")]
public sealed class CatalogIntegrationController : ControllerBase
{
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.VehiclesIntegration)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpsertAsync(
        Guid id,
        [FromBody] CatalogVehicleRequest request,
        [FromServices] ICommandHandler<UpsertCatalogVehicleCommand,
            Result<CatalogUpsertResult>> handler,
        CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            return ResultExtensions.ToProblem(
                AutoSale.Domain.Catalog.VehicleCatalogErrors.InvalidSnapshot, this);
        }

        var snapshot = request.ToDomain();
        if (snapshot.IsFailure)
        {
            return ResultExtensions.ToProblem(snapshot.Error, this);
        }

        var result = await handler.HandleAsync(
            new UpsertCatalogVehicleCommand(id, snapshot.Value!), cancellationToken);
        return result.IsFailure
            ? ResultExtensions.ToProblem(result.Error, this)
            : NoContent();
    }
}
