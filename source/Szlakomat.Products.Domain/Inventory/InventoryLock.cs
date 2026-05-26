namespace Szlakomat.Products.Domain.Inventory;

internal class InventoryLock
{
    private readonly InventoryLockId _id;
    private readonly string? _holderId;
    private readonly DateTimeOffset _acquiredAt;

    private InventoryLock(InventoryLockId? id, string? holderId, DateTimeOffset acquiredAt)
    {
        Guard.IsNotNull(id);
        _id = id;
        _holderId = holderId;
        _acquiredAt = acquiredAt;
    }

    public static InventoryLock Of(InventoryLockId id, string? holderId, DateTimeOffset acquiredAt) =>
        new(id, holderId, acquiredAt);

    public InventoryLockId Id() => _id;

    public string? HolderId() => _holderId;

    public DateTimeOffset AcquiredAt() => _acquiredAt;

    public override bool Equals(object? obj)
    {
        if (this == obj) return true;
        if (obj == null || GetType() != obj.GetType()) return false;
        InventoryLock that = (InventoryLock)obj;
        return _id.Equals(that._id);
    }

    public override int GetHashCode() => _id.GetHashCode();

    public override string ToString() =>
        $"Lock{{id={_id}, holder={_holderId ?? "—"}, acquired={_acquiredAt:O}}}";
}
