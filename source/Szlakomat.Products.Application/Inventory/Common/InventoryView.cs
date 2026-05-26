namespace Szlakomat.Products.Application.Inventory.Common;

public record InventoryView(
    string ProductId,
    int StockTotal,
    long RequestCount,
    bool IsLocked,
    InventoryLockView? CurrentLock
);
