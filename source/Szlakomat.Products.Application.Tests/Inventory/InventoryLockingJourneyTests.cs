using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Szlakomat.Products.Application.Instances.CreateProductInstance;
using Szlakomat.Products.Application.Inventory.AdjustStock;
using Szlakomat.Products.Application.Inventory.FindInventory;
using Szlakomat.Products.Application.Inventory.ProcessBucket;
using Szlakomat.Products.Application.Inventory.RegisterInventory;
using Szlakomat.Products.Application.Tests.Assemblers;
using Szlakomat.Products.Application.Tests.Infrastructure;
using Szlakomat.Products.Domain.Instances;
using Szlakomat.Products.Domain.Inventory;

namespace Szlakomat.Products.Application.Tests.Inventory;

public class InventoryLockingJourneyTests
{
    private readonly IServiceProvider _provider;
    private readonly IMediator _mediator;
    private readonly ITicketLockService _lockService;

    public InventoryLockingJourneyTests()
    {
        _provider = ServiceProviderFactory.Create();
        _mediator = _provider.GetRequiredService<IMediator>();
        _lockService = _provider.GetRequiredService<ITicketLockService>();
    }

    private async Task<string> CreateTimedEntryAttraction()
    {
        var productId = Guid.NewGuid().ToString();
        var result = await _mediator.Send(CatalogCommandAssembler.TimedEntryAttraction(productId));
        Assert.True(result.IsSuccess());
        return productId;
    }

    private async Task<string> CreateGroupAttraction()
    {
        var productId = Guid.NewGuid().ToString();
        var result = await _mediator.Send(CatalogCommandAssembler.GroupAttraction(productId));
        Assert.True(result.IsSuccess());
        return productId;
    }

    private async Task<string> FillBucket(string productId, int instanceCount)
    {
        var bucketId = Guid.NewGuid().ToString();
        for (var i = 0; i < instanceCount; i++)
        {
            var created = await _mediator.Send(
                new CreateProductInstance(productId, null, bucketId, "1", "pcs", null));
            Assert.True(created.IsSuccess());
        }
        return bucketId;
    }

    [Fact]
    public async Task RegisterInventory_ForExistingProduct_ShouldSucceed()
    {
        var productId = await CreateTimedEntryAttraction();

        var result = await _mediator.Send(new RegisterInventory(productId, 10));

        Assert.True(result.IsSuccess());
        var view = await _mediator.Send(new FindInventoryCriteria(productId));
        Assert.NotNull(view);
        Assert.Equal(10, view!.StockTotal);
    }

    [Fact]
    public async Task RegisterInventory_ForUnknownProduct_ShouldFail()
    {
        var result = await _mediator.Send(
            new RegisterInventory("nonexistent-product-id", 10));

        Assert.False(result.IsSuccess());
        Assert.Contains("not found", result.GetFailure()!);
    }

    [Fact]
    public async Task RegisterInventory_TwiceForSameProduct_ShouldFailSecondTime()
    {
        var productId = await CreateTimedEntryAttraction();
        await _mediator.Send(new RegisterInventory(productId, 10));

        var second = await _mediator.Send(new RegisterInventory(productId, 5));

        Assert.False(second.IsSuccess());
        Assert.Contains("already exists", second.GetFailure()!);
    }

    [Fact]
    public async Task AdjustStock_AppliesPositiveDelta_IncreasesStock()
    {
        var productId = await CreateTimedEntryAttraction();
        await _mediator.Send(new RegisterInventory(productId, 10));

        var adjusted = await _mediator.Send(new AdjustStock(productId, +10));
        Assert.True(adjusted.IsSuccess());

        var view = await _mediator.Send(new FindInventoryCriteria(productId));
        Assert.Equal(20, view!.StockTotal);
    }

    [Fact]
    public async Task AdjustStock_AppliesNegativeDelta_ReducesStock()
    {
        var productId = await CreateTimedEntryAttraction();
        await _mediator.Send(new RegisterInventory(productId, 10));

        var adjusted = await _mediator.Send(new AdjustStock(productId, -4));
        Assert.True(adjusted.IsSuccess());

        var view = await _mediator.Send(new FindInventoryCriteria(productId));
        Assert.Equal(6, view!.StockTotal);
    }

    [Fact]
    public async Task AdjustStock_BelowZero_ShouldFail()
    {
        var productId = await CreateTimedEntryAttraction();
        await _mediator.Send(new RegisterInventory(productId, 5));

        var result = await _mediator.Send(new AdjustStock(productId, -10));

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public async Task BucketLock_OnFreeBucket_IsGrantedToTheFirstHolder()
    {
        var bucket = BucketId.NewOne();

        var ticket = await _lockService.TryAcquireFirstAvailableAsync(
            new[] { bucket }, "processor-A", TimeSpan.FromMinutes(1));

        Assert.NotNull(ticket);
        Assert.Equal(bucket, ticket!.BucketId);
        Assert.Equal("processor-A", ticket.LockedBy);
    }

    [Fact]
    public async Task BucketLock_WhileHeld_CannotBeAcquiredAgain()
    {
        var bucket = BucketId.NewOne();
        await _lockService.TryAcquireFirstAvailableAsync(new[] { bucket }, "processor-A", TimeSpan.FromMinutes(1));

        var second = await _lockService.TryAcquireFirstAvailableAsync(new[] { bucket }, "processor-B", TimeSpan.FromMinutes(1));

        Assert.Null(second);
    }

    [Fact]
    public async Task BucketLock_PicksFirstAvailableBucket_SkippingHeldOnes()
    {
        var held = BucketId.NewOne();
        var free = BucketId.NewOne();
        await _lockService.TryAcquireFirstAvailableAsync(new[] { held }, "processor-A", TimeSpan.FromMinutes(1));

        var ticket = await _lockService.TryAcquireFirstAvailableAsync(
            new[] { held, free }, "processor-B", TimeSpan.FromMinutes(1));

        Assert.NotNull(ticket);
        Assert.Equal(free, ticket!.BucketId);
    }

    [Fact]
    public async Task BucketLock_AfterRelease_CanBeAcquiredAgain()
    {
        var bucket = BucketId.NewOne();
        var ticket = await _lockService.TryAcquireFirstAvailableAsync(new[] { bucket }, "processor-A", TimeSpan.FromMinutes(1));
        Assert.NotNull(ticket);

        var released = await _lockService.ReleaseAsync(ticket!.LockId);
        Assert.True(released);

        var reacquired = await _lockService.TryAcquireFirstAvailableAsync(new[] { bucket }, "processor-B", TimeSpan.FromMinutes(1));
        Assert.NotNull(reacquired);
    }

    [Fact]
    public async Task ProcessBucket_WhenGoesThrough_ConsumesStockForBucketInstances()
    {
        var productId = await CreateGroupAttraction();
        await _mediator.Send(new RegisterInventory(productId, 100));
        var bucketId = await FillBucket(productId, 3);

        var result = await _mediator.Send(new ProcessBucket(bucketId, "fulfilment-service"));

        Assert.True(result.IsSuccess());
        var view = await _mediator.Send(new FindInventoryCriteria(productId));
        Assert.Equal(97, view!.StockTotal);
    }

    [Fact]
    public async Task ProcessBucket_WhenGoesThrough_ReleasesTheLock()
    {
        var productId = await CreateGroupAttraction();
        await _mediator.Send(new RegisterInventory(productId, 100));
        var bucketId = await FillBucket(productId, 2);

        await _mediator.Send(new ProcessBucket(bucketId, "fulfilment-service"));

        var active = await _lockService.GetActiveLocksAsync();
        Assert.DoesNotContain(active, t => t.BucketId.Value == Guid.Parse(bucketId));
    }

    [Fact]
    public async Task ProcessBucket_WhileBucketIsLockedElsewhere_FailsAsBeingProcessed()
    {
        var productId = await CreateGroupAttraction();
        await _mediator.Send(new RegisterInventory(productId, 100));
        var bucketId = await FillBucket(productId, 1);
        var held = await _lockService.TryAcquireFirstAvailableAsync(
            new[] { BucketId.Of(bucketId) }, "other-system", TimeSpan.FromMinutes(1));
        Assert.NotNull(held);

        var result = await _mediator.Send(new ProcessBucket(bucketId, "fulfilment-service"));

        Assert.False(result.IsSuccess());
        Assert.Contains("already being processed", result.GetFailure()!);
        var view = await _mediator.Send(new FindInventoryCriteria(productId));
        Assert.Equal(100, view!.StockTotal);
    }

    [Fact]
    public async Task ProcessBucket_AfterTheHoldingLockIsReleased_GoesThrough()
    {
        var productId = await CreateGroupAttraction();
        await _mediator.Send(new RegisterInventory(productId, 100));
        var bucketId = await FillBucket(productId, 1);
        var held = await _lockService.TryAcquireFirstAvailableAsync(
            new[] { BucketId.Of(bucketId) }, "other-system", TimeSpan.FromMinutes(1));
        await _lockService.ReleaseAsync(held!.LockId);

        var result = await _mediator.Send(new ProcessBucket(bucketId, "fulfilment-service"));

        Assert.True(result.IsSuccess());
        var view = await _mediator.Send(new FindInventoryCriteria(productId));
        Assert.Equal(99, view!.StockTotal);
    }

    [Fact]
    public async Task ProcessBucket_WhenWorkFallsThrough_LeavesStockUntouchedAndReleasesLock()
    {
        var productId = await CreateGroupAttraction();
        await _mediator.Send(new RegisterInventory(productId, 2));
        var bucketId = await FillBucket(productId, 3);

        var result = await _mediator.Send(new ProcessBucket(bucketId, "fulfilment-service"));

        Assert.False(result.IsSuccess());
        var view = await _mediator.Send(new FindInventoryCriteria(productId));
        Assert.Equal(2, view!.StockTotal);
        var active = await _lockService.GetActiveLocksAsync();
        Assert.DoesNotContain(active, t => t.BucketId.Value == Guid.Parse(bucketId));
    }

    [Fact]
    public async Task ProcessBucket_ForUnknownBucket_FailsAsFallThrough()
    {
        var unknownBucket = Guid.NewGuid().ToString();

        var result = await _mediator.Send(new ProcessBucket(unknownBucket, "fulfilment-service"));

        Assert.False(result.IsSuccess());
        Assert.Contains("no instances", result.GetFailure()!);
    }

    [Fact]
    public async Task ConcurrentProcessing_OfTheSameBucket_LeavesStockConsistentWithSuccesses()
    {
        var productId = await CreateGroupAttraction();
        await _mediator.Send(new RegisterInventory(productId, 100));
        var bucketId = await FillBucket(productId, 5);

        const int parallelism = 16;
        var barrier = new Barrier(parallelism);
        var tasks = Enumerable.Range(0, parallelism)
            .Select(i => Task.Run(async () =>
            {
                barrier.SignalAndWait();
                return await _mediator.Send(new ProcessBucket(bucketId, $"worker-{i}"));
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        var successes = results.Count(r => r.IsSuccess());
        var view = await _mediator.Send(new FindInventoryCriteria(productId));
        Assert.True(view!.StockTotal >= 0);
        Assert.Equal(100 - successes * 5, view.StockTotal);
    }
}
