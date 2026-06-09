using Szlakomat.Products.Domain.Inventory;

namespace Szlakomat.Products.Domain.Tests.Domain.Inventory;

public class InventoryTests
{
    private const string ProductId = "PROD-1";

    [Fact]
    public void Initialize_ShouldCreateInventoryWithGivenStock()
    {
        var inventory = ProductInventory.Initialize(ProductId, StockLevel.Of(10));

        inventory.ProductId().Should().Be(ProductId);
        inventory.Stock().Total.Should().Be(10);
    }

    [Fact]
    public void ApplyStockDelta_Positive_ShouldIncreaseStock()
    {
        var inventory = ProductInventory.Initialize(ProductId, StockLevel.Of(10));

        var result = inventory.ApplyStockDelta(10);

        result.IsSuccess().Should().BeTrue();
        result.SuccessValue.Stock().Total.Should().Be(20);
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
