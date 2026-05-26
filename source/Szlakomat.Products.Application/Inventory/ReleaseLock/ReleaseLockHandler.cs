using MediatR;
using Szlakomat.Products.Domain.Common;
using Szlakomat.Products.Domain.Inventory;

namespace Szlakomat.Products.Application.Inventory.ReleaseLock;

internal sealed class ReleaseLockHandler
    : IRequestHandler<ReleaseLock, Result<string, string>>
{
    private readonly IInventoryRepository _inventoryRepository;

    public ReleaseLockHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public Task<Result<string, string>> Handle(
        ReleaseLock command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.ProductId))
        {
            return Task.FromResult(
                Result<string, string>.FailureOf("ProductId is required"));
        }

        if (string.IsNullOrWhiteSpace(command.LockId))
        {
            return Task.FromResult(
                Result<string, string>.FailureOf("LockId is required"));
        }

        var lockId = InventoryLockId.Of(command.LockId);
        var result = _inventoryRepository.AtomicallyRelease(command.ProductId, lockId);
        return Task.FromResult(result.Map(_ => command.LockId));
    }
}
