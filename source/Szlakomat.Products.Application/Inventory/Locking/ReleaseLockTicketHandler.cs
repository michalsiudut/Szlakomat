using MediatR;
using Szlakomat.Products.Domain.Inventory;

namespace Szlakomat.Products.Application.Inventory.Locking;

internal sealed class ReleaseLockTicketHandler : IRequestHandler<ReleaseLockTicket, bool>
{
    private readonly ITicketLockService _lockService;

    public ReleaseLockTicketHandler(ITicketLockService lockService)
    {
        _lockService = lockService;
    }

    public Task<bool> Handle(ReleaseLockTicket command, CancellationToken cancellationToken) =>
        _lockService.ReleaseAsync(command.LockId, cancellationToken);
}
