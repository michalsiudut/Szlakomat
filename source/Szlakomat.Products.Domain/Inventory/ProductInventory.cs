using Szlakomat.Products.Domain.Common;

namespace Szlakomat.Products.Domain.Inventory;

internal class ProductInventory
{
    private readonly string _productId;
    private readonly StockLevel _stock;
    private readonly InventoryLock? _currentLock;
    private readonly long _requestCount;

    private ProductInventory(
        string? productId,
        StockLevel? stock,
        InventoryLock? currentLock,
        long requestCount)
    {
        Guard.IsNotNullOrWhiteSpace(productId);
        Guard.IsNotNull(stock);
        if (requestCount < 0)
        {
            throw new ArgumentException($"RequestCount cannot be negative: {requestCount}");
        }
        _productId = productId;
        _stock = stock;
        _currentLock = currentLock;
        _requestCount = requestCount;
    }

    public static ProductInventory Initialize(string productId, StockLevel stock) =>
        new(productId, stock, null, 0);

    public string ProductId() => _productId;

    public StockLevel Stock() => _stock;

    public InventoryLock? CurrentLock() => _currentLock;

    public bool IsLocked() => _currentLock is not null;

    public long RequestCount() => _requestCount;

    public Result<string, ProductInventory> ApplyStockDelta(int delta)
    {
        try
        {
            var newStock = delta >= 0
                ? _stock.Increase(delta)
                : _stock.Decrease(-delta);
            return Result<string, ProductInventory>.SuccessOf(
                new ProductInventory(_productId, newStock, _currentLock, _requestCount + 1));
        }
        catch (ArgumentException ex)
        {
            return Result<string, ProductInventory>.FailureOf(ex.Message);
        }
    }

    public Result<string, ProductInventory> TryLock(InventoryLock @lock)
    {
        Guard.IsNotNull(@lock);

        if (_currentLock is not null)
        {
            return Result<string, ProductInventory>.FailureOf(
                $"Resource {_productId} is already locked (lock {_currentLock.Id()}, holder {_currentLock.HolderId() ?? "—"})");
        }

        return Result<string, ProductInventory>.SuccessOf(
            new ProductInventory(_productId, _stock, @lock, _requestCount + 1));
    }

    public Result<string, ProductInventory> Release(InventoryLockId lockId)
    {
        Guard.IsNotNull(lockId);

        if (_currentLock is null)
        {
            return Result<string, ProductInventory>.FailureOf(
                $"Resource {_productId} is not locked");
        }

        if (!_currentLock.Id().Equals(lockId))
        {
            return Result<string, ProductInventory>.FailureOf(
                $"Lock {lockId} does not match current lock {_currentLock.Id()} on resource {_productId}");
        }

        return Result<string, ProductInventory>.SuccessOf(
            new ProductInventory(_productId, _stock, null, _requestCount + 1));
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
        $"ProductInventory{{productId={_productId}, stock={_stock}, locked={IsLocked()}, requests={_requestCount}}}";
}
