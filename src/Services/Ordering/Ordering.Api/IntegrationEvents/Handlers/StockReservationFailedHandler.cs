using Contracts;
using EventBus;
using Microsoft.EntityFrameworkCore;
using Ordering.Api.Data;
using Ordering.Api.Domain;

namespace Ordering.Api.IntegrationEvents.Handlers;

public sealed class StockReservationFailedHandler(OrderingDbContext db, IEventBus eventBus)
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
        await db.SaveChangesAsync(ct);

        await eventBus.PublishAsync(new OrderCancelledIntegrationEvent(order.Id, order.UserEmail, @event.Reason), ct);
    }
}
