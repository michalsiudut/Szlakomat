namespace Szlakomat.Products.Api.Contracts.Inventory;

public record LockResponse(
    string LockId,
    string BucketId,
    string LockedBy,
    DateTimeOffset ExpiresAt
);
