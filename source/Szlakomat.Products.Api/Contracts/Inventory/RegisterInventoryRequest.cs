namespace Szlakomat.Products.Api.Contracts.Inventory;

public record RegisterInventoryRequest(
    string ProductId,
    int InitialStock
);
