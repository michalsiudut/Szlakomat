using System.Collections.Concurrent;
using Szlakomat.Products.Domain.Instances;

namespace Szlakomat.Products.Infrastructure.Instances;

internal class InMemoryInstanceRepository : IInstanceRepository
{
    private readonly ConcurrentDictionary<InstanceId, IInstance> _storage = new();

    public void Save(IInstance instance)
    {
        _storage[instance.Id()] = instance;
    }

    public IInstance? FindById(InstanceId id)
    {
        return _storage.TryGetValue(id, out var value) ? value : null;
    }

    public IInstance? FindByStringId(string id)
    {
        return _storage.Values.FirstOrDefault(i => i.Id().ToString() == id);
    }

    public IReadOnlyList<IInstance> FindAll()
    {
        return _storage.Values.ToList();
    }

    public IReadOnlyList<IInstance> FindByBucketId(BucketId bucketId)
    {
        return _storage.Values.Where(i => i.BatchId()?.Value == bucketId.Value).ToList().AsReadOnly();
    }
}
