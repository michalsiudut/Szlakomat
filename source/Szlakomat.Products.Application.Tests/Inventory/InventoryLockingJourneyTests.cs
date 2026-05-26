using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Szlakomat.Products.Application.Inventory.AdjustStock;
using Szlakomat.Products.Application.Inventory.FindInventory;
using Szlakomat.Products.Application.Inventory.LockProduct;
using Szlakomat.Products.Application.Inventory.RegisterInventory;
using Szlakomat.Products.Application.Inventory.ReleaseLock;
using Szlakomat.Products.Application.Tests.Assemblers;
using Szlakomat.Products.Application.Tests.Infrastructure;

namespace Szlakomat.Products.Application.Tests.Inventory;

public class InventoryLockingJourneyTests
{
    private readonly IMediator _mediator;

    public InventoryLockingJourneyTests()
    {
        _mediator = ServiceProviderFactory.Create().GetRequiredService<IMediator>();
    }

    private async Task<string> CreateAttraction()
    {
        var productId = Guid.NewGuid().ToString();
        var result = await _mediator.Send(CatalogCommandAssembler.TimedEntryAttraction(productId));
        Assert.True(result.IsSuccess());
        return productId;
    }

    [Fact]
    public async Task RegisterInventory_ForExistingProduct_ShouldSucceed()
    {
        var productId = await CreateAttraction();

        var result = await _mediator.Send(new RegisterInventory(productId, 10));

        Assert.True(result.IsSuccess());
        var view = await _mediator.Send(new FindInventoryCriteria(productId));
        Assert.NotNull(view);
        Assert.Equal(10, view!.StockTotal);
        Assert.False(view.IsLocked);
        Assert.Null(view.CurrentLock);
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
        var productId = await CreateAttraction();
        await _mediator.Send(new RegisterInventory(productId, 10));

        var second = await _mediator.Send(new RegisterInventory(productId, 5));

        Assert.False(second.IsSuccess());
        Assert.Contains("already exists", second.GetFailure()!);
    }

    [Fact]
    public async Task AdjustStock_ChangesTotalWithoutAffectingLock()
    {
        var productId = await CreateAttraction();
        await _mediator.Send(new RegisterInventory(productId, 10));
        var lockResult = await _mediator.Send(new LockProduct(productId, "holder-A"));
        Assert.True(lockResult.IsSuccess());

        var adjusted = await _mediator.Send(new AdjustStock(productId, 20));
        Assert.True(adjusted.IsSuccess());

        var view = await _mediator.Send(new FindInventoryCriteria(productId));
        Assert.Equal(20, view!.StockTotal);
        Assert.True(view.IsLocked);
    }

    [Fact]
    public async Task LockProduct_OnExistingInventory_ShouldSucceedAndReturnLockId()
    {
        var productId = await CreateAttraction();
        await _mediator.Send(new RegisterInventory(productId, 10));

        var result = await _mediator.Send(new LockProduct(productId, "holder-A"));

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.SuccessValue);
        Assert.StartsWith("LOCK-", result.SuccessValue.Value);
    }

    [Fact]
    public async Task LockProduct_AlreadyLocked_ShouldFailWithConflict()
    {
        var productId = await CreateAttraction();
        await _mediator.Send(new RegisterInventory(productId, 10));
        await _mediator.Send(new LockProduct(productId, "holder-A"));

        var result = await _mediator.Send(new LockProduct(productId, "holder-B"));

        Assert.False(result.IsSuccess());
        Assert.Contains("already locked", result.GetFailure()!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LockProduct_ForUnknownInventory_ShouldFail()
    {
        var result = await _mediator.Send(new LockProduct("nonexistent", "holder-A"));

        Assert.False(result.IsSuccess());
        Assert.Contains("not found", result.GetFailure()!);
    }

    [Fact]
    public async Task ReleaseLock_WithMatchingLockId_ShouldFreeResource()
    {
        var productId = await CreateAttraction();
        await _mediator.Send(new RegisterInventory(productId, 10));
        var firstLock = await _mediator.Send(new LockProduct(productId, "holder-A"));
        Assert.True(firstLock.IsSuccess());

        var release = await _mediator.Send(
            new ReleaseLock(productId, firstLock.SuccessValue.Value));
        Assert.True(release.IsSuccess());

        var reLock = await _mediator.Send(new LockProduct(productId, "holder-B"));
        Assert.True(reLock.IsSuccess());
    }

    [Fact]
    public async Task ReleaseLock_WithForeignLockId_ShouldFail()
    {
        var productId = await CreateAttraction();
        await _mediator.Send(new RegisterInventory(productId, 10));
        await _mediator.Send(new LockProduct(productId, "holder-A"));

        var release = await _mediator.Send(
            new ReleaseLock(productId, "LOCK-foreign-id"));

        Assert.False(release.IsSuccess());
        Assert.Contains("does not match", release.GetFailure()!);
    }

    [Fact]
    public async Task ReleaseLock_OnUnlockedInventory_ShouldFail()
    {
        var productId = await CreateAttraction();
        await _mediator.Send(new RegisterInventory(productId, 10));

        var result = await _mediator.Send(new ReleaseLock(productId, "LOCK-ghost"));

        Assert.False(result.IsSuccess());
        Assert.Contains("not locked", result.GetFailure()!);
    }

    [Fact]
    public async Task ConcurrentLockAttempts_ExactlyOneShouldSucceed()
    {
        var productId = await CreateAttraction();
        await _mediator.Send(new RegisterInventory(productId, 10));

        const int parallelism = 20;
        var barrier = new Barrier(parallelism);
        var tasks = Enumerable.Range(0, parallelism)
            .Select(i => Task.Run(async () =>
            {
                barrier.SignalAndWait();
                return await _mediator.Send(new LockProduct(productId, $"thread-{i}"));
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var successes = results.Count(r => r.IsSuccess());

        Assert.Equal(1, successes);

        var view = await _mediator.Send(new FindInventoryCriteria(productId));
        Assert.True(view!.IsLocked);
    }

    [Fact]
    public async Task RequestCount_ShouldIncrementWithEachStateChangingOperation()
    {
        var productId = await CreateAttraction();
        await _mediator.Send(new RegisterInventory(productId, 10));

        await _mediator.Send(new AdjustStock(productId, 15));
        var locked = await _mediator.Send(new LockProduct(productId, null));
        await _mediator.Send(new ReleaseLock(productId, locked.SuccessValue.Value));

        var view = await _mediator.Send(new FindInventoryCriteria(productId));
        Assert.Equal(3, view!.RequestCount);
    }
}
