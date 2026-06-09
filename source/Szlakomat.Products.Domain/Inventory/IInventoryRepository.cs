namespace Szlakomat.Products.Domain.Inventory;

internal interface IInventoryRepository
{
    void Save(ProductInventory inventory);

    ProductInventory? FindByProductId(string productId);
}
