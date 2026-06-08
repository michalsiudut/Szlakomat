using MediatR;
using Szlakomat.Products.Domain.Common;
using Szlakomat.Products.Domain.Inventory;

namespace Szlakomat.Products.Application.Inventory.AdjustStock;

internal sealed class AdjustStockHandler
    : IRequestHandler<AdjustStock, Result<string, string>>
{
    private readonly IInventoryRepository _inventoryRepository;

    public AdjustStockHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public Task<Result<string, string>> Handle(
        AdjustStock command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.ProductId))
        {
            return Task.FromResult(
                Result<string, string>.FailureOf("ProductId is required"));
        }

        var inventory = _inventoryRepository.FindByProductId(command.ProductId);
        if (inventory is null)
        {
            return Task.FromResult(
                Result<string, string>.FailureOf(
                    $"Inventory not found for product: {command.ProductId}"));
        }

        var result = inventory.ApplyStockDelta(command.Delta);
        if (result.IsFailure())
        {
            return Task.FromResult(
                Result<string, string>.FailureOf(result.GetFailure()!));
        }

        _inventoryRepository.Save(result.SuccessValue);
        return Task.FromResult(
            Result<string, string>.SuccessOf(command.ProductId));
    }
}
