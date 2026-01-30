using TC.Agro.SharedKernel.Infrastructure.Messaging.Outbox;
using Wolverine.EntityFrameworkCore;

namespace TC.Agro.Farm.Infrastructure.Messaging;

/// <summary>
/// Farm service specific Outbox binding.
/// Uses SharedKernel's generic WolverineEfCoreOutbox with ApplicationDbContext.
/// </summary>
public sealed class FarmOutbox : WolverineEfCoreOutbox<ApplicationDbContext>
{
    public FarmOutbox(IDbContextOutbox<ApplicationDbContext> outbox) : base(outbox) { }
}
