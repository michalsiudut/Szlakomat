using MediatR;
using Szlakomat.Products.Domain.Common;
using Szlakomat.Products.Domain.Instances;
using Szlakomat.Products.Domain.Inventory;

namespace Szlakomat.Products.Application.Inventory.ProcessBucket;

internal sealed class ProcessBucketHandler
    : IRequestHandler<ProcessBucket, Result<string, string>>
{
    private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(5);

    private readonly ITicketLockService _lockService;
    private readonly IInstanceRepository _instanceRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public ProcessBucketHandler(
        ITicketLockService lockService,
        IInstanceRepository instanceRepository,
        IInventoryRepository inventoryRepository)
    {
        _lockService = lockService;
        _instanceRepository = instanceRepository;
        _inventoryRepository = inventoryRepository;
    }

    public async Task<Result<string, string>> Handle(
        ProcessBucket command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.BucketId))
        {
            return Result<string, string>.FailureOf("BucketId is required");
        }

        BucketId bucketId;
        try
        {
            bucketId = BucketId.Of(command.BucketId);
        }
        catch (FormatException)
        {
            return Result<string, string>.FailureOf($"Invalid bucket id: {command.BucketId}");
        }

        var ticket = await _lockService.TryAcquireFirstAvailableAsync(
            new[] { bucketId }, command.RequestedBy, LockTtl, cancellationToken);
        if (ticket is null)
        {
            return Result<string, string>.FailureOf(
                $"Bucket {command.BucketId} is already being processed");
        }

        try
        {
            return Consume(bucketId);
        }
        finally
        {
            await _lockService.ReleaseAsync(ticket.LockId, cancellationToken);
        }
    }

    private Result<string, string> Consume(BucketId bucketId)
    {
        var instances = _instanceRepository.FindByBucketId(bucketId);
        if (instances.Count == 0)
        {
            return Result<string, string>.FailureOf(
                $"Bucket {bucketId} has no instances to process");
        }

        var consumptionByProduct = instances
            .GroupBy(instance => instance.Product().Id().ToString())
            .ToDictionary(group => group.Key, group => group.Count());

        var consumed = new List<ProductInventory>(consumptionByProduct.Count);
        foreach (var (productId, count) in consumptionByProduct)
        {
            var inventory = _inventoryRepository.FindByProductId(productId);
            if (inventory is null)
            {
                return Result<string, string>.FailureOf(
                    $"Inventory not found for product: {productId}");
            }

            var applied = inventory.ApplyStockDelta(-count);
            if (applied.IsFailure())
            {
                return Result<string, string>.FailureOf(applied.GetFailure()!);
            }

            consumed.Add(applied.SuccessValue);
        }

        foreach (var inventory in consumed)
        {
            _inventoryRepository.Save(inventory);
        }

        return Result<string, string>.SuccessOf(bucketId.ToString());
    }
}
