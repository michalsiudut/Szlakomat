namespace Szlakomat.Products.Api.Contracts.Inventory;

public record InventoryResponse(
    string ProductId,
    int StockTotal,
    bool IsLocked
);
