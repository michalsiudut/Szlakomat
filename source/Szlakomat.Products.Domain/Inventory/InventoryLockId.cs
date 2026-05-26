namespace Szlakomat.Products.Domain.Inventory;

public class InventoryLockId
{
    public string Value { get; }

    private InventoryLockId(string? value)
    {
        Guard.IsNotNullOrWhiteSpace(value);
        Value = value;
    }

    public static InventoryLockId Of(string value) => new(value);

    public static InventoryLockId Generate() => new($"LOCK-{Guid.NewGuid()}");

    public override bool Equals(object? obj)
    {
        if (this == obj) return true;
        if (obj == null || GetType() != obj.GetType()) return false;
        InventoryLockId that = (InventoryLockId)obj;
        return Value == that.Value;
    }

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;
}
