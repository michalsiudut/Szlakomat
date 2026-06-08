namespace Szlakomat.Products.Domain.Instances;

public record BucketId(Guid Value)
{
    public static BucketId NewOne() => new(Guid.NewGuid());
    public static BucketId Of(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}
