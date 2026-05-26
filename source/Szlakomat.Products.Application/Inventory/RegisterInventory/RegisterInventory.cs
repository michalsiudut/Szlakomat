using MediatR;
using Szlakomat.Products.Domain.Common;

namespace Szlakomat.Products.Application.Inventory.RegisterInventory;

public record RegisterInventory(
    string ProductId,
    int InitialStock
) : IRequest<Result<string, string>>;
