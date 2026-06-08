using MediatR;
using Szlakomat.Products.Domain.Common;

namespace Szlakomat.Products.Application.Inventory.AdjustStock;

public record AdjustStock(
    string ProductId,
    int Delta
) : IRequest<Result<string, string>>;
