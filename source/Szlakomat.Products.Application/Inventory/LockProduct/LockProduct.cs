using MediatR;
using Szlakomat.Products.Domain.Common;
using Szlakomat.Products.Domain.Inventory;

namespace Szlakomat.Products.Application.Inventory.LockProduct;

public record LockProduct(
    string ProductId,
    string? HolderId
) : IRequest<Result<string, InventoryLockId>>;
