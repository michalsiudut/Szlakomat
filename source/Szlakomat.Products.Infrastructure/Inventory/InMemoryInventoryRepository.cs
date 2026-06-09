using System.Collections.Concurrent;
using Szlakomat.Products.Domain.Inventory;

namespace Szlakomat.Products.Infrastructure.Inventory;

internal class InMemoryInventoryRepository : IInventoryRepository
{
    private readonly ConcurrentDictionary<string, ProductInventory> _storage = new();

    public void Save(ProductInventory inventory)
    {
        _storage[inventory.ProductId()] = inventory;
    }

    public ProductInventory? FindByProductId(string productId)
    {
        return _storage.TryGetValue(productId, out var value) ? value : null;
    }
}
