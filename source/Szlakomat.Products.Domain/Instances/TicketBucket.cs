namespace Szlakomat.Products.Domain.Instances;

public sealed class TicketBucket
{
    public BucketId Id { get; }
    public IReadOnlyList<InstanceId> InstanceIds { get; }
    public int Size => InstanceIds.Count;

    private TicketBucket(BucketId id, IReadOnlyList<InstanceId> instanceIds)
    {
        if (instanceIds.Count == 0)
            throw new ArgumentException("Bucket must contain at least one instance.");
        Id = id;
        InstanceIds = instanceIds;
    }

    public static TicketBucket Create(IEnumerable<InstanceId> instanceIds)
    {
        var list = instanceIds.ToList();
        return new TicketBucket(BucketId.NewOne(), list.AsReadOnly());
    }

    public static IReadOnlyList<TicketBucket> SplitIntoBuckets(IEnumerable<InstanceId> instanceIds, int bucketSize)
    {
        if (bucketSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(bucketSize), "Bucket size must be > 0.");
        var result = new List<TicketBucket>();
        var chunk = new List<InstanceId>(bucketSize);
        foreach (var id in instanceIds)
        {
            chunk.Add(id);
            if (chunk.Count == bucketSize)
            {
                result.Add(new TicketBucket(BucketId.NewOne(), chunk.AsReadOnly()));
                chunk = new List<InstanceId>(bucketSize);
            }
        }
        if (chunk.Count > 0)
            result.Add(new TicketBucket(BucketId.NewOne(), chunk.AsReadOnly()));
        return result.AsReadOnly();
    }
    public override string ToString() => $"TicketBucket{{id={Id}, size={Size}}}";
}
