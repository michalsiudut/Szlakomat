using MediatR;
using Microsoft.AspNetCore.Mvc;
using Szlakomat.Products.Api.Contracts.Inventory;
using Szlakomat.Products.Api.Mappers;
using Szlakomat.Products.Application.Inventory.AdjustStock;
using Szlakomat.Products.Application.Inventory.FindInventory;
using Szlakomat.Products.Application.Inventory.LockProduct;
using Szlakomat.Products.Application.Inventory.RegisterInventory;
using Szlakomat.Products.Application.Inventory.ReleaseLock;

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

        var result = await mediator.Send(new AdjustStock(productId, req.NewTotal));
        if (!result.IsSuccess())
        {
            return BadRequest(new { error = result.GetFailure() });
        }

        return Ok(InventoryMapper.ToResponse(
            (await mediator.Send(new FindInventoryCriteria(productId)))!));
    }

    [HttpPost("{productId}/lock")]
    [ProducesResponseType(typeof(LockProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Lock(string productId, LockProductRequest req)
    {
        if (await mediator.Send(new FindInventoryCriteria(productId)) is null)
        {
            return NotFound();
        }

        var result = await mediator.Send(new LockProduct(productId, req.HolderId));
        if (result.IsSuccess())
        {
            return CreatedAtAction(
                nameof(GetByProductId),
                new { productId },
                new LockProductResponse(result.SuccessValue.Value));
        }

        var error = result.GetFailure() ?? "unknown error";
        return error.Contains("already locked", StringComparison.OrdinalIgnoreCase)
            ? Conflict(new { error })
            : BadRequest(new { error });
    }

    [HttpDelete("{productId}/lock/{lockId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Release(string productId, string lockId)
    {
        if (await mediator.Send(new FindInventoryCriteria(productId)) is null)
        {
            return NotFound();
        }

        var result = await mediator.Send(new ReleaseLock(productId, lockId));
        if (result.IsSuccess())
        {
            return NoContent();
        }

        var error = result.GetFailure() ?? "unknown error";
        return error.Contains("does not match", StringComparison.OrdinalIgnoreCase)
            ? Conflict(new { error })
            : BadRequest(new { error });
    }
}
