using Szlakomat.Products.Domain.Instances;

namespace Szlakomat.Products.Domain.Inventory;

internal interface ITicketLockService
{
    Task<TicketLock?> TryAcquireFirstAvailableAsync(IReadOnlyList<BucketId> candidates, string lockedBy, TimeSpan ttl, CancellationToken ct = default);
    Task<bool> ReleaseAsync(Guid lockId, CancellationToken ct = default);
    Task<IReadOnlyList<TicketLock>> GetActiveLocksAsync(CancellationToken ct = default);
}

internal record TicketLock(Guid LockId, BucketId BucketId, string LockedBy, DateTimeOffset ExpiresAt);
