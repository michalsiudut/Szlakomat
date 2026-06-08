using MediatR;
using Szlakomat.Products.Domain.Common;

namespace Szlakomat.Products.Application.Inventory.LockProduct;

public record LockProduct(
    string ProductId,
    string? HolderId
) : IRequest<Result<string, string>>;
