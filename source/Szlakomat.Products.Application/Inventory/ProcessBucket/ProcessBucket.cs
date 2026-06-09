using MediatR;
using Szlakomat.Products.Domain.Common;

namespace Szlakomat.Products.Application.Inventory.ProcessBucket;

public record ProcessBucket(
    string BucketId,
    string RequestedBy
) : IRequest<Result<string, string>>;
