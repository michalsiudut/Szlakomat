namespace Szlakomat.Products.Api.Contracts.Inventory;

// Positive delta adds stock; negative delta removes stock.
public record AdjustStockRequest(int Delta);
