using Contracts;
using Microsoft.EntityFrameworkCore;
using Ordering.Api.Domain;
using Ordering.Api.IntegrationEvents.Handlers;
using Xunit;
using static Ordering.Tests.OutboxTestHelpers;

namespace Ordering.Tests;

public class StockReservedHandlerTests
{
    [Fact]
    public async Task HandleAsync_confirms_order_and_enqueues_OrderConfirmed_in_outbox()
    {
        using var db = TestDbContextFactory.CreateInMemory(out var connection);
        using var _ = connection;

        var productId = Guid.NewGuid();
        var order = Order.Create(Guid.NewGuid(), "user@example.com", [(productId, 2)]);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var handler = new StockReservedHandler(db, CreateOutbox(db));

        var @event = new StockReservedIntegrationEvent(order.Id, [new ReservedItem(productId, "Ürün", 25m, 2)]);

        await handler.HandleAsync(@event, CancellationToken.None);

        var reloaded = await db.Orders.AsNoTracking().Include(o => o.Items).FirstAsync(o => o.Id == order.Id);
        Assert.Equal(OrderStatus.Confirmed, reloaded.Status);
        Assert.Equal(50m, reloaded.Total);

        var outboxEvents = await GetOutboxEventsAsync(db);
        var enqueued = Assert.Single(outboxEvents);
        Assert.IsType<OrderConfirmedIntegrationEvent>(enqueued);
    }

    [Fact]
    public async Task HandleAsync_is_idempotent_when_same_message_processed_twice()
    {
        using var db = TestDbContextFactory.CreateInMemory(out var connection);
        using var _ = connection;

        var productId = Guid.NewGuid();
        var order = Order.Create(Guid.NewGuid(), "user@example.com", [(productId, 2)]);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var handler = new StockReservedHandler(db, CreateOutbox(db));

        var @event = new StockReservedIntegrationEvent(order.Id, [new ReservedItem(productId, "Ürün", 25m, 2)]);

        await handler.HandleAsync(@event, CancellationToken.None);
        await handler.HandleAsync(@event, CancellationToken.None);

        var outboxEvents = await GetOutboxEventsAsync(db);
        Assert.Single(outboxEvents);
    }
}
