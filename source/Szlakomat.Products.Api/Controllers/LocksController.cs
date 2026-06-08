using MediatR;
using Microsoft.AspNetCore.Mvc;
using Szlakomat.Products.Api.Contracts.Locks;
using Szlakomat.Products.Application.Locking;

namespace Szlakomat.Products.Api.Controllers;

[ApiController]
[Route("api/locks")]
[Produces("application/json")]
public class LocksController(ISender mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(LockResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Acquire([FromBody] AcquireLockRequest req)
    {
        var tl = await mediator.Send(new AcquireLockCommand(req.BucketIds, req.LockedBy, req.TtlSeconds));
        if (tl is null) return Conflict(new { error = "No available bucket." });
        return CreatedAtAction(nameof(GetAll), null, new LockResponse(tl.LockId.ToString(), tl.BucketId.ToString(), tl.LockedBy, tl.ExpiresAt));
    }
    [HttpDelete("{lockId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Release(string lockId)
    {
        var released = await mediator.Send(new ReleaseLockCommand(lockId));
        return released ? NoContent() : NotFound();
    }
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LockResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var locks = await mediator.Send(new GetActiveLocksQuery());
        return Ok(locks.Select(tl => new LockResponse(tl.LockId.ToString(), tl.BucketId.ToString(), tl.LockedBy, tl.ExpiresAt)));
    }
}
