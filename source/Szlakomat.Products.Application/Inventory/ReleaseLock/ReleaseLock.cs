using MediatR;
using Szlakomat.Products.Domain.Common;

namespace Szlakomat.Products.Application.Inventory.ReleaseLock;

public record ReleaseLock(
    string ProductId,
    string LockId
) : IRequest<Result<string, string>>;
