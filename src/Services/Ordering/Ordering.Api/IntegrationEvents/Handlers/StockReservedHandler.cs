using Contracts;
using EventBus;
using Microsoft.EntityFrameworkCore;
using Ordering.Api.Data;
using Ordering.Api.Domain;

namespace Ordering.Api.IntegrationEvents.Handlers;

public sealed class StockReservedHandler(OrderingDbContext db, IEventBus eventBus) : IIntegrationEventHandler<StockReservedIntegrationEvent>
{
    public async Task HandleAsync(StockReservedIntegrationEvent @event, CancellationToken ct)
    {
        if (await db.ProcessedMessages.AnyAsync(m => m.MessageId == @event.Id, ct))
            return;

        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == @event.OrderId, ct);
        if (order is null) return;

        var resolvedItems = @event.Items.ToDictionary(i => i.ProductId, i => (i.ProductName, i.UnitPrice));
        order.Confirm(resolvedItems);

        db.ProcessedMessages.Add(new ProcessedMessage(@event.Id));
        await db.SaveChangesAsync(ct);

        await eventBus.PublishAsync(new OrderConfirmedIntegrationEvent(order.Id, order.UserEmail, order.Total), ct);
    }
}
