using MediatR;
using Szlakomat.Products.Domain.Inventory;

namespace Szlakomat.Products.Application.Inventory.Locking;

public record GetActiveLocks : IRequest<IReadOnlyList<TicketLock>>;
