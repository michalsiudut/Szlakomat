using System.Collections.Concurrent;
using Szlakomat.Products.Domain.Instances;
using Szlakomat.Products.Domain.Inventory;

namespace Szlakomat.Products.Infrastructure.Inventory;

internal sealed class InMemoryTicketLockService : ITicketLockService
{
    private readonly ConcurrentDictionary<Guid, TicketLock> _locks = new();
    private readonly ConcurrentDictionary<Guid, Guid> _bucketIndex = new();
    private readonly object _acquireLock = new();

    public Task<TicketLock?> TryAcquireFirstAvailableAsync(IReadOnlyList<BucketId> candidates, string lockedBy, TimeSpan ttl, CancellationToken ct = default)
    {
        lock (_acquireLock)
        {
            PurgeExpired();
            foreach (var bucketId in candidates)
            {
                if (_bucketIndex.ContainsKey(bucketId.Value))
                    continue;
                var lockId = Guid.NewGuid();
                var ticketLock = new TicketLock(lockId, bucketId, lockedBy, DateTimeOffset.UtcNow.Add(ttl));
                _locks[lockId] = ticketLock;
                _bucketIndex[bucketId.Value] = lockId;
                return Task.FromResult<TicketLock?>(ticketLock);
            }
        }
        return Task.FromResult<TicketLock?>(null);
    }

    public Task<bool> ReleaseAsync(Guid lockId, CancellationToken ct = default)
    {
        if (!_locks.TryRemove(lockId, out var ticketLock))
            return Task.FromResult(false);
        _bucketIndex.TryRemove(ticketLock.BucketId.Value, out _);
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<TicketLock>> GetActiveLocksAsync(CancellationToken ct = default)
    {
        PurgeExpired();
        return Task.FromResult<IReadOnlyList<TicketLock>>(_locks.Values.ToList().AsReadOnly());
    }

    private void PurgeExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var kv in _locks)
            if (kv.Value.ExpiresAt <= now && _locks.TryRemove(kv.Key, out var expired))
                _bucketIndex.TryRemove(expired.BucketId.Value, out _);
    }
}
