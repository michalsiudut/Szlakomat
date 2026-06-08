using MediatR;
using Microsoft.AspNetCore.Mvc;
using Szlakomat.Products.Api.Contracts.Inventory;
using Szlakomat.Products.Application.Inventory.Locking;
using Szlakomat.Products.Domain.Instances;

namespace Szlakomat.Products.Api.Controllers;

[ApiController]
[Route("api/inventory/locks")]
[Produces("application/json")]
public class LocksController(ISender mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(LockResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Acquire([FromBody] AcquireLockRequest req)
    {
        var bucketIds = req.BucketIds.Select(BucketId.Of).ToList();
        var tl = await mediator.Send(new AcquireLock(bucketIds, req.LockedBy, req.TtlSeconds));
        if (tl is null) return Conflict(new { error = "No available bucket." });
        return CreatedAtAction(nameof(GetAll), null, new LockResponse(tl.LockId.ToString(), tl.BucketId.ToString(), tl.LockedBy, tl.ExpiresAt));
    }

    [HttpDelete("{lockId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Release(string lockId)
    {
        var released = await mediator.Send(new ReleaseLockTicket(Guid.Parse(lockId)));
        return released ? NoContent() : NotFound();
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LockResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var locks = await mediator.Send(new GetActiveLocks());
        return Ok(locks.Select(tl => new LockResponse(tl.LockId.ToString(), tl.BucketId.ToString(), tl.LockedBy, tl.ExpiresAt)));
    }
}
