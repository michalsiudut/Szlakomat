using MediatR;
using Szlakomat.Products.Application.Inventory.Common;
using Szlakomat.Products.Domain.Inventory;

namespace Szlakomat.Products.Application.Inventory.FindInventory;

internal sealed class FindInventoryHandler
    : IRequestHandler<FindInventoryCriteria, InventoryView?>
{
    private readonly IInventoryRepository _inventoryRepository;

    public FindInventoryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public Task<InventoryView?> Handle(
        FindInventoryCriteria criteria,
        CancellationToken cancellationToken)
    {
        var inventory = _inventoryRepository.FindByProductId(criteria.ProductId);
        return Task.FromResult<InventoryView?>(
            inventory is null ? null : InventoryMapper.ToView(inventory));
    }
}
