namespace Szlakomat.Products.Domain.Inventory;

internal class StockLevel
{
    public int Total { get; }

    private StockLevel(int total)
    {
        if (total < 0)
        {
            throw new ArgumentException($"StockLevel.Total cannot be negative: {total}");
        }
        Total = total;
    }

    public static StockLevel Of(int total) => new(total);

    public static StockLevel Empty() => new(0);

    public StockLevel SetTo(int newTotal) => Of(newTotal);

    public StockLevel Increase(int by)
    {
        if (by < 0)
        {
            throw new ArgumentException($"Increase amount cannot be negative: {by}");
        }
        return Of(Total + by);
    }

    public StockLevel Decrease(int by)
    {
        if (by < 0)
        {
            throw new ArgumentException($"Decrease amount cannot be negative: {by}");
        }
        if (by > Total)
        {
            throw new ArgumentException($"Cannot decrease below zero: {Total} - {by}");
        }
        return Of(Total - by);
    }

    public override bool Equals(object? obj)
    {
        if (this == obj) return true;
        if (obj == null || GetType() != obj.GetType()) return false;
        StockLevel that = (StockLevel)obj;
        return Total == that.Total;
    }

    public override int GetHashCode() => Total.GetHashCode();

    public override string ToString() => $"Stock({Total})";
}
