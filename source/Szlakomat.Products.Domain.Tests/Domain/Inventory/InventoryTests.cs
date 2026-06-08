using Szlakomat.Products.Domain.Inventory;

namespace Szlakomat.Products.Domain.Tests.Domain.Inventory;

public class InventoryTests
{
    private const string ProductId = "PROD-1";

    private static DateTimeOffset At(int hour, int minute = 0) =>
        new(2026, 5, 18, hour, minute, 0, TimeSpan.Zero);

    private static InventoryLock MakeLock(string? holder = null) =>
        InventoryLock.Of(InventoryLockId.Generate(), holder, At(0));

    [Fact]
    public void Initialize_ShouldCreateUnlockedInventoryWithZeroRequests()
    {
        var inventory = ProductInventory.Initialize(ProductId, StockLevel.Of(10));

        inventory.ProductId().Should().Be(ProductId);
        inventory.Stock().Total.Should().Be(10);
        inventory.IsLocked().Should().BeFalse();
        inventory.CurrentLock().Should().BeNull();
        inventory.RequestCount().Should().Be(0);
    }

    [Fact]
    public void ApplyStockDelta_Positive_ShouldIncreaseStockAndIncrementRequestCount()
    {
        var inventory = ProductInventory.Initialize(ProductId, StockLevel.Of(10));

        var result = inventory.ApplyStockDelta(10);

        result.IsSuccess().Should().BeTrue();
        result.SuccessValue.Stock().Total.Should().Be(20);
        result.SuccessValue.RequestCount().Should().Be(1);
    }

    [Fact]
    public void ApplyStockDelta_Negative_ShouldDecreaseStock()
    {
        var inventory = ProductInventory.Initialize(ProductId, StockLevel.Of(10));

        var result = inventory.ApplyStockDelta(-3);

        result.IsSuccess().Should().BeTrue();
        result.SuccessValue.Stock().Total.Should().Be(7);
    }

    [Fact]
    public void ApplyStockDelta_Zero_ShouldLeaveStockUnchanged()
    {
        var inventory = ProductInventory.Initialize(ProductId, StockLevel.Of(10));

        var result = inventory.ApplyStockDelta(0);

        result.IsSuccess().Should().BeTrue();
        result.SuccessValue.Stock().Total.Should().Be(10);
    }

    [Fact]
    public void ApplyStockDelta_BelowZero_ShouldFail()
    {
        var inventory = ProductInventory.Initialize(ProductId, StockLevel.Of(5));

        var result = inventory.ApplyStockDelta(-10);

        result.IsFailure().Should().BeTrue();
        result.GetFailure()!.Should().Contain("zero");
    }

    [Fact]
    public void TryLock_OnUnlockedInventory_ShouldSucceedAndIncrementRequestCount()
    {
        var inventory = ProductInventory.Initialize(ProductId, StockLevel.Of(10));
        var @lock = MakeLock("thread-A");

        var result = inventory.TryLock(@lock);

        result.IsSuccess().Should().BeTrue();
        result.SuccessValue.IsLocked().Should().BeTrue();
        result.SuccessValue.CurrentLock()!.Id().Should().Be(@lock.Id());
        result.SuccessValue.RequestCount().Should().Be(1);
    }

    [Fact]
    public void TryLock_OnAlreadyLockedInventory_ShouldFail()
    {
        var inventory = ProductInventory.Initialize(ProductId, StockLevel.Of(10));
        var first = MakeLock("thread-A");
        var second = MakeLock("thread-B");

        var afterFirst = inventory.TryLock(first).SuccessValue;
        var result = afterFirst.TryLock(second);

        result.IsFailure().Should().BeTrue();
        result.GetFailure()!.Should().Contain("already locked");
    }

    [Fact]
    public void Release_WithMatchingLockId_ShouldUnlockAndIncrementRequestCount()
    {
        var inventory = ProductInventory.Initialize(ProductId, StockLevel.Of(10));
        var @lock = MakeLock("thread-A");
        var locked = inventory.TryLock(@lock).SuccessValue;

        var result = locked.Release(@lock.Id());

        result.IsSuccess().Should().BeTrue();
        result.SuccessValue.IsLocked().Should().BeFalse();
        result.SuccessValue.RequestCount().Should().Be(2);
    }

    [Fact]
    public void Release_WithMismatchedLockId_ShouldFail()
    {
        var inventory = ProductInventory.Initialize(ProductId, StockLevel.Of(10));
        var holderLock = MakeLock("thread-A");
        var locked = inventory.TryLock(holderLock).SuccessValue;
        var foreignId = InventoryLockId.Generate();

        var result = locked.Release(foreignId);

        result.IsFailure().Should().BeTrue();
        result.GetFailure()!.Should().Contain("does not match");
    }

    [Fact]
    public void Release_OnUnlockedInventory_ShouldFail()
    {
        var inventory = ProductInventory.Initialize(ProductId, StockLevel.Of(10));
        var someLockId = InventoryLockId.Generate();

        var result = inventory.Release(someLockId);

        result.IsFailure().Should().BeTrue();
        result.GetFailure()!.Should().Contain("not locked");
    }

    [Fact]
    public void Aggregate_AfterReleaseCanBeLockedAgain()
    {
        var inventory = ProductInventory.Initialize(ProductId, StockLevel.Of(10));
        var first = MakeLock("thread-A");
        var second = MakeLock("thread-B");

        var locked = inventory.TryLock(first).SuccessValue;
        var released = locked.Release(first.Id()).SuccessValue;
        var reLocked = released.TryLock(second);

        reLocked.IsSuccess().Should().BeTrue();
        reLocked.SuccessValue.CurrentLock()!.Id().Should().Be(second.Id());
    }

    [Fact]
    public void ApplyStockDelta_OnLockedInventory_DoesNotAffectLock()
    {
        var inventory = ProductInventory.Initialize(ProductId, StockLevel.Of(10));
        var @lock = MakeLock("thread-A");
        var locked = inventory.TryLock(@lock).SuccessValue;

        var result = locked.ApplyStockDelta(40);

        result.IsSuccess().Should().BeTrue();
        result.SuccessValue.IsLocked().Should().BeTrue();
        result.SuccessValue.CurrentLock()!.Id().Should().Be(@lock.Id());
        result.SuccessValue.Stock().Total.Should().Be(50);
    }
}

public class StockLevelTests
{
    [Fact]
    public void Of_NegativeTotal_ShouldThrow()
    {
        var act = () => StockLevel.Of(-1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Of_ZeroTotal_ShouldSucceed()
    {
        var stock = StockLevel.Of(0);

        stock.Total.Should().Be(0);
    }

    [Fact]
    public void Increase_ShouldAddToTotal()
    {
        var stock = StockLevel.Of(10).Increase(5);

        stock.Total.Should().Be(15);
    }

    [Fact]
    public void Decrease_BelowZero_ShouldThrow()
    {
        var act = () => StockLevel.Of(5).Decrease(10);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Decrease_ByNegativeAmount_ShouldThrow()
    {
        var act = () => StockLevel.Of(5).Decrease(-1);

        act.Should().Throw<ArgumentException>();
    }
}
