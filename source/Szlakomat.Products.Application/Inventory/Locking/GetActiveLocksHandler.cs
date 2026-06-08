using MediatR;
using Szlakomat.Products.Domain.Inventory;

namespace Szlakomat.Products.Application.Inventory.Locking;

internal sealed class GetActiveLocksHandler : IRequestHandler<GetActiveLocks, IReadOnlyList<TicketLock>>
{
    private readonly ITicketLockService _lockService;

    public GetActiveLocksHandler(ITicketLockService lockService)
    {
        _lockService = lockService;
    }

    public Task<IReadOnlyList<TicketLock>> Handle(GetActiveLocks query, CancellationToken cancellationToken) =>
        _lockService.GetActiveLocksAsync(cancellationToken);
}
