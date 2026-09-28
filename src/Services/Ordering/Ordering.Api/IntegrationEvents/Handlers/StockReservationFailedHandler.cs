using Contracts;
using EventBus;
using EventBus.Outbox;
using Microsoft.EntityFrameworkCore;
using Ordering.Api.Data;
using Ordering.Api.Domain;

namespace Ordering.Api.IntegrationEvents.Handlers;

public sealed class StockReservationFailedHandler(OrderingDbContext db, IOutbox outbox)
    : IIntegrationEventHandler<StockReservationFailedIntegrationEvent>
{
    public async Task HandleAsync(StockReservationFailedIntegrationEvent @event, CancellationToken ct)
    {
        if (await db.ProcessedMessages.AnyAsync(m => m.MessageId == @event.Id, ct))
            return;

        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == @event.OrderId, ct);
        if (order is null) return;

        order.Cancel(@event.Reason);

        db.ProcessedMessages.Add(new ProcessedMessage(@event.Id));
        outbox.Enqueue(new OrderCancelledIntegrationEvent(order.Id, order.UserEmail, @event.Reason));

        await db.SaveChangesAsync(ct);
    }
}
