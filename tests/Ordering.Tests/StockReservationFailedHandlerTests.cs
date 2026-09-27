using Contracts;
using Microsoft.EntityFrameworkCore;
using Ordering.Api.Domain;
using Ordering.Api.IntegrationEvents.Handlers;
using Xunit;

namespace Ordering.Tests;

public class StockReservationFailedHandlerTests
{
    [Fact]
    public async Task HandleAsync_cancels_order_and_publishes_OrderCancelled()
    {
        using var db = TestDbContextFactory.CreateInMemory(out var connection);
        using var _ = connection;

        var order = Order.Create(Guid.NewGuid(), "user@example.com", [(Guid.NewGuid(), 2)]);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var bus = new FakeEventBus();
        var handler = new StockReservationFailedHandler(db, bus);

        var @event = new StockReservationFailedIntegrationEvent(order.Id, "Yetersiz stok.");

        await handler.HandleAsync(@event, CancellationToken.None);

        var reloaded = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.Id);
        Assert.Equal(OrderStatus.Cancelled, reloaded.Status);
        Assert.Equal("Yetersiz stok.", reloaded.CancelReason);

        var published = Assert.Single(bus.Published);
        Assert.IsType<OrderCancelledIntegrationEvent>(published);
    }

    [Fact]
    public async Task HandleAsync_is_idempotent_when_same_message_processed_twice()
    {
        using var db = TestDbContextFactory.CreateInMemory(out var connection);
        using var _ = connection;

        var order = Order.Create(Guid.NewGuid(), "user@example.com", [(Guid.NewGuid(), 2)]);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var bus = new FakeEventBus();
        var handler = new StockReservationFailedHandler(db, bus);

        var @event = new StockReservationFailedIntegrationEvent(order.Id, "Yetersiz stok.");

        await handler.HandleAsync(@event, CancellationToken.None);
        await handler.HandleAsync(@event, CancellationToken.None);

        Assert.Single(bus.Published);
    }
}
