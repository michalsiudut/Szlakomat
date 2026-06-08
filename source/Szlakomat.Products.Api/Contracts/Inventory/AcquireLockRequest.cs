namespace Szlakomat.Products.Api.Contracts.Inventory;

public record AcquireLockRequest(
    IReadOnlyList<string> BucketIds,
    string LockedBy,
    int TtlSeconds
);
