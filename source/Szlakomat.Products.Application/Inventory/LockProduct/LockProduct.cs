using MediatR;
using Szlakomat.Products.Domain.Common;

namespace Szlakomat.Products.Application.Inventory.LockProduct;

// Internal use only – lock management is not exposed via the public API.
public record LockProduct(
    string ProductId,
    string? HolderId
) : IRequest<Result<string, string>>;
