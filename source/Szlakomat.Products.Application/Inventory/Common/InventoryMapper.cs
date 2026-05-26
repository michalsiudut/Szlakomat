using Szlakomat.Products.Domain.Inventory;

namespace Szlakomat.Products.Application.Inventory.Common;

internal static class InventoryMapper
{
    internal static InventoryView ToView(ProductInventory inventory)
    {
        var current = inventory.CurrentLock();
        return new InventoryView(
            inventory.ProductId(),
            inventory.Stock().Total,
            inventory.RequestCount(),
            inventory.IsLocked(),
            current is null ? null : ToView(current));
    }

    internal static InventoryLockView ToView(InventoryLock @lock) =>
        new(@lock.Id().Value, @lock.HolderId(), @lock.AcquiredAt());
}
