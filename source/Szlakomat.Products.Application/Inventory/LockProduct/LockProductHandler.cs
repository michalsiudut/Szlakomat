using MediatR;
using Szlakomat.Products.Domain.Common;
using Szlakomat.Products.Domain.Inventory;

namespace Szlakomat.Products.Application.Inventory.LockProduct;

internal sealed class LockProductHandler
    : IRequestHandler<LockProduct, Result<string, string>>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly TimeProvider _timeProvider;

    public LockProductHandler(
        IInventoryRepository inventoryRepository,
        TimeProvider timeProvider)
    {
        _inventoryRepository = inventoryRepository;
        _timeProvider = timeProvider;
    }

    public Task<Result<string, string>> Handle(
        LockProduct command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.ProductId))
        {
            return Task.FromResult(
                Result<string, string>.FailureOf("ProductId is required"));
        }

        var lockId = InventoryLockId.Generate();
        var newLock = InventoryLock.Of(lockId, command.HolderId, _timeProvider.GetUtcNow());

        var result = _inventoryRepository.AtomicallyTryLock(command.ProductId, newLock);
        return Task.FromResult(result.Map(_ => lockId.Value));
    }
}
