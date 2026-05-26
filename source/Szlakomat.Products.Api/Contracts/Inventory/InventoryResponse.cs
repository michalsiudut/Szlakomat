namespace Szlakomat.Products.Api.Contracts.Inventory;

public record InventoryResponse(
    string ProductId,
    int StockTotal,
    long RequestCount,
    bool IsLocked,
    InventoryLockResponse? CurrentLock
);
