using Szlakomat.Products.Domain.Common;

namespace Szlakomat.Products.Domain.Inventory;

internal interface IInventoryRepository
{
    void Save(ProductInventory inventory);

    ProductInventory? FindByProductId(string productId);

    IReadOnlySet<ProductInventory> FindAll();

    void Remove(string productId);

    Result<string, ProductInventory> AtomicallyTryLock(string productId, InventoryLock @lock);

    Result<string, ProductInventory> AtomicallyRelease(string productId, InventoryLockId lockId);
}
