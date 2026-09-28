using Common.Correlation;
using Contracts;
using Microsoft.EntityFrameworkCore;

namespace EventBus.Outbox;

internal sealed class EfOutbox<TContext>(TContext db, ICorrelationIdAccessor correlationIdAccessor) : IOutbox
    where TContext : DbContext
{
    public void Enqueue<T>(T @event) where T : IntegrationEvent
    {
        CorrelationStamper.Stamp(@event, correlationIdAccessor);
        db.Set<OutboxMessage>().Add(OutboxMessage.FromEvent(@event));
    }
}
