using MediatR;
using Szlakomat.Products.Domain.Catalog.ProductType;
using Szlakomat.Products.Domain.Common;
using Szlakomat.Products.Domain.Inventory;

namespace Szlakomat.Products.Application.Inventory.RegisterInventory;

internal sealed class RegisterInventoryHandler
    : IRequestHandler<RegisterInventory, Result<string, string>>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IProductTypeRepository _productTypeRepository;

    public RegisterInventoryHandler(
        IInventoryRepository inventoryRepository,
        IProductTypeRepository productTypeRepository)
    {
        _inventoryRepository = inventoryRepository;
        _productTypeRepository = productTypeRepository;
    }

    public Task<Result<string, string>> Handle(
        RegisterInventory command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.ProductId))
        {
            return Task.FromResult(
                Result<string, string>.FailureOf("ProductId is required"));
        }

        if (command.InitialStock < 0)
        {
            return Task.FromResult(
                Result<string, string>.FailureOf(
                    $"InitialStock cannot be negative: {command.InitialStock}"));
        }

        var product = _productTypeRepository.FindByIdValue(command.ProductId);
        if (product is null)
        {
            return Task.FromResult(
                Result<string, string>.FailureOf(
                    $"Product type not found: {command.ProductId}"));
        }

        if (_inventoryRepository.FindByProductId(command.ProductId) is not null)
        {
            return Task.FromResult(
                Result<string, string>.FailureOf(
                    $"Inventory already exists for product: {command.ProductId}"));
        }

        var inventory = ProductInventory.Initialize(
            command.ProductId,
            StockLevel.Of(command.InitialStock));
        _inventoryRepository.Save(inventory);

        return Task.FromResult(
            Result<string, string>.SuccessOf(command.ProductId));
    }
}
