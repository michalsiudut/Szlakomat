using MediatR;
using Szlakomat.Products.Application.Inventory.Common;

namespace Szlakomat.Products.Application.Inventory.FindInventory;

public record FindInventoryCriteria(string ProductId) : IRequest<InventoryView?>;
