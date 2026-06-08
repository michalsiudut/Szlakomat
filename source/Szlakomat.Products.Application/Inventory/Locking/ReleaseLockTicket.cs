using MediatR;

namespace Szlakomat.Products.Application.Inventory.Locking;

public record ReleaseLockTicket(Guid LockId) : IRequest<bool>;
