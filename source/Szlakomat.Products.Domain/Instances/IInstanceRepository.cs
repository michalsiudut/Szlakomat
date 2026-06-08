namespace Szlakomat.Products.Domain.Instances;

public interface IInstanceRepository
{
    void Save(IInstance instance);
    IInstance? FindById(InstanceId id);
    IInstance? FindByStringId(string id);
    IReadOnlyList<IInstance> FindAll();
    IReadOnlyList<IInstance> FindByBucketId(BucketId bucketId);
}
