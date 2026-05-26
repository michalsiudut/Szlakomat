using Szlakomat.Products.Api.Contracts.Inventory;
using Szlakomat.Products.Application.Inventory.Common;

namespace Szlakomat.Products.Api.Mappers;

internal static class InventoryMapper
{
    internal static InventoryResponse ToResponse(InventoryView v) =>
        new(
            v.ProductId,
            v.StockTotal,
            v.RequestCount,
            v.IsLocked,
            v.CurrentLock is null ? null : ToResponse(v.CurrentLock));

    internal static InventoryLockResponse ToResponse(InventoryLockView v) =>
        new(v.LockId, v.HolderId, v.AcquiredAt);
}
