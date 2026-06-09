using Szlakomat.Products.Domain.Inventory;

namespace Szlakomat.Products.Application.Inventory.Common;

internal static class InventoryMapper
{
    internal static InventoryView ToView(ProductInventory inventory) =>
        new(inventory.ProductId(), inventory.Stock().Total);
}
