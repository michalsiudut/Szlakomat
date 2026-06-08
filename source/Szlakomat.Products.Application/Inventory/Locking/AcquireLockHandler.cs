using MediatR;
using Szlakomat.Products.Domain.Inventory;

namespace Szlakomat.Products.Application.Inventory.Locking;

internal sealed class AcquireLockHandler : IRequestHandler<AcquireLock, TicketLock?>
{
    private readonly ITicketLockService _lockService;

    public AcquireLockHandler(ITicketLockService lockService)
    {
        _lockService = lockService;
    }

    public Task<TicketLock?> Handle(AcquireLock command, CancellationToken cancellationToken) =>
        _lockService.TryAcquireFirstAvailableAsync(
            command.BucketIds,
            command.LockedBy,
            TimeSpan.FromSeconds(command.TtlSeconds),
            cancellationToken);
}
