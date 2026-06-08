using MediatR;
using Szlakomat.Products.Domain.Instances;
using Szlakomat.Products.Domain.Inventory;

namespace Szlakomat.Products.Application.Inventory.Locking;

public record AcquireLock(
    IReadOnlyList<BucketId> BucketIds,
    string LockedBy,
    int TtlSeconds
) : IRequest<TicketLock?>;
