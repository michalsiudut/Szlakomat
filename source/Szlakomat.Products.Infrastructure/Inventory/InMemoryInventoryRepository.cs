using System.Collections.Concurrent;
using Szlakomat.Products.Domain.Common;
using Szlakomat.Products.Domain.Inventory;

namespace Szlakomat.Products.Infrastructure.Inventory;

internal class InMemoryInventoryRepository : IInventoryRepository
{
    private readonly ConcurrentDictionary<string, ProductInventory> _storage = new();
    private readonly ConcurrentDictionary<string, object> _aggregateLocks = new();

    public void Save(ProductInventory inventory)
    {
        _storage[inventory.ProductId()] = inventory;
    }

    public ProductInventory? FindByProductId(string productId)
    {
        return _storage.TryGetValue(productId, out var value) ? value : null;
    }

    public IReadOnlySet<ProductInventory> FindAll()
    {
        return new HashSet<ProductInventory>(_storage.Values);
    }

    public void Remove(string productId)
    {
        _storage.TryRemove(productId, out _);
        _aggregateLocks.TryRemove(productId, out _);
    }

    public Result<string, ProductInventory> AtomicallyTryLock(
        string productId,
        InventoryLock @lock)
    {
        var monitor = _aggregateLocks.GetOrAdd(productId, _ => new object());
        lock (monitor)
        {
            if (!_storage.TryGetValue(productId, out var inventory))
            {
                return Result<string, ProductInventory>.FailureOf(
                    $"Inventory not found for product: {productId}");
            }

            var result = inventory.TryLock(@lock);
            if (result.IsSuccess())
            {
                _storage[productId] = result.SuccessValue;
            }
            return result;
        }
    }

    public Result<string, ProductInventory> AtomicallyRelease(
        string productId,
        InventoryLockId lockId)
    {
        var monitor = _aggregateLocks.GetOrAdd(productId, _ => new object());
        lock (monitor)
        {
            if (!_storage.TryGetValue(productId, out var inventory))
            {
                return Result<string, ProductInventory>.FailureOf(
                    $"Inventory not found for product: {productId}");
            }

            var result = inventory.Release(lockId);
            if (result.IsSuccess())
            {
                _storage[productId] = result.SuccessValue;
            }
            return result;
        }
    }
}
