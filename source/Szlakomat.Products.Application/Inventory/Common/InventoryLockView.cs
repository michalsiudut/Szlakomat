namespace Szlakomat.Products.Application.Inventory.Common;

public record InventoryLockView(
    string LockId,
    string? HolderId,
    DateTimeOffset AcquiredAt
);
