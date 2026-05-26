namespace Szlakomat.Products.Api.Contracts.Inventory;

public record InventoryLockResponse(
    string LockId,
    string? HolderId,
    DateTimeOffset AcquiredAt
);
