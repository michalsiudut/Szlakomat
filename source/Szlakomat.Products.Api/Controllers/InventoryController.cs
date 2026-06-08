using MediatR;
using Microsoft.AspNetCore.Mvc;
using Szlakomat.Products.Api.Contracts.Inventory;
using Szlakomat.Products.Api.Mappers;
using Szlakomat.Products.Application.Inventory.AdjustStock;
using Szlakomat.Products.Application.Inventory.FindInventory;
using Szlakomat.Products.Application.Inventory.RegisterInventory;

namespace Szlakomat.Products.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[Produces("application/json")]
public class InventoryController(ISender mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(InventoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterInventoryRequest req)
    {
        var result = await mediator.Send(new RegisterInventory(req.ProductId, req.InitialStock));
        if (!result.IsSuccess())
        {
            return BadRequest(new { error = result.GetFailure() });
        }

        var view = await mediator.Send(new FindInventoryCriteria(req.ProductId));
        return CreatedAtAction(
            nameof(GetByProductId),
            new { productId = req.ProductId },
            InventoryMapper.ToResponse(view!));
    }

    [HttpGet("{productId}")]
    [ProducesResponseType(typeof(InventoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByProductId(string productId)
    {
        var view = await mediator.Send(new FindInventoryCriteria(productId));
        return view is not null
            ? Ok(InventoryMapper.ToResponse(view))
            : NotFound();
    }

    [HttpPatch("{productId}/stock")]
    [ProducesResponseType(typeof(InventoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdjustStock(string productId, AdjustStockRequest req)
    {
        if (await mediator.Send(new FindInventoryCriteria(productId)) is null)
        {
            return NotFound();
        }

        var result = await mediator.Send(new AdjustStock(productId, req.Delta));
        if (!result.IsSuccess())
        {
            return BadRequest(new { error = result.GetFailure() });
        }

        return Ok(InventoryMapper.ToResponse(
            (await mediator.Send(new FindInventoryCriteria(productId)))!));
    }
}
