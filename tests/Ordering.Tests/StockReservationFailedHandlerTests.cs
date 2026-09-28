using Contracts;
using Microsoft.EntityFrameworkCore;
using Ordering.Api.Domain;
using Ordering.Api.IntegrationEvents.Handlers;
using Xunit;
using static Ordering.Tests.OutboxTestHelpers;

namespace Ordering.Tests;

public class StockReservationFailedHandlerTests
{
    [Fact]
    public async Task HandleAsync_cancels_order_and_enqueues_OrderCancelled_in_outbox()
    {
        using var db = TestDbContextFactory.CreateInMemory(out var connection);
        using var _ = connection;

        var order = Order.Create(Guid.NewGuid(), "user@example.com", [(Guid.NewGuid(), 2)]);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var handler = new StockReservationFailedHandler(db, CreateOutbox(db));

        var @event = new StockReservationFailedIntegrationEvent(order.Id, "Yetersiz stok.");

        await handler.HandleAsync(@event, CancellationToken.None);

        var reloaded = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.Id);
        Assert.Equal(OrderStatus.Cancelled, reloaded.Status);
        Assert.Equal("Yetersiz stok.", reloaded.CancelReason);

        var outboxEvents = await GetOutboxEventsAsync(db);
        var enqueued = Assert.Single(outboxEvents);
        Assert.IsType<OrderCancelledIntegrationEvent>(enqueued);
    }

    [Fact]
    public async Task HandleAsync_is_idempotent_when_same_message_processed_twice()
    {
        using var db = TestDbContextFactory.CreateInMemory(out var connection);
        using var _ = connection;

        var order = Order.Create(Guid.NewGuid(), "user@example.com", [(Guid.NewGuid(), 2)]);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var handler = new StockReservationFailedHandler(db, CreateOutbox(db));

        var @event = new StockReservationFailedIntegrationEvent(order.Id, "Yetersiz stok.");

        await handler.HandleAsync(@event, CancellationToken.None);
        await handler.HandleAsync(@event, CancellationToken.None);

        var outboxEvents = await GetOutboxEventsAsync(db);
        Assert.Single(outboxEvents);
    }
}
