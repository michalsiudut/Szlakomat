using MediatR;
using Szlakomat.Products.Domain.Common;

namespace Szlakomat.Products.Application.Inventory.AdjustStock;

// Delta > 0 increases stock; delta < 0 decreases stock.
public record AdjustStock(
    string ProductId,
    int Delta
) : IRequest<Result<string, string>>;
