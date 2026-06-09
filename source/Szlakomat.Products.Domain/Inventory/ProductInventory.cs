using Szlakomat.Products.Domain.Common;

namespace Szlakomat.Products.Domain.Inventory;

internal class ProductInventory
{
    private readonly string _productId;
    private readonly StockLevel _stock;

    private ProductInventory(string? productId, StockLevel? stock)
    {
        Guard.IsNotNullOrWhiteSpace(productId);
        Guard.IsNotNull(stock);
        _productId = productId;
        _stock = stock;
    }

    public static ProductInventory Initialize(string productId, StockLevel stock) =>
        new(productId, stock);

    public string ProductId() => _productId;

    public StockLevel Stock() => _stock;

    public Result<string, ProductInventory> ApplyStockDelta(int delta)
    {
        try
        {
            var newStock = delta >= 0
                ? _stock.Increase(delta)
                : _stock.Decrease(-delta);
            return Result<string, ProductInventory>.SuccessOf(
                new ProductInventory(_productId, newStock));
        }
        catch (ArgumentException ex)
        {
            return Result<string, ProductInventory>.FailureOf(ex.Message);
        }
    }

    public override bool Equals(object? obj)
    {
        if (this == obj) return true;
        if (obj == null || GetType() != obj.GetType()) return false;
        ProductInventory that = (ProductInventory)obj;
        return _productId == that._productId;
    }

    public override int GetHashCode() => _productId.GetHashCode();

    public override string ToString() =>
        $"ProductInventory{{productId={_productId}, stock={_stock}}}";
}
