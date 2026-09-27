using Catalog.Api.Domain;
using Catalog.Api.IntegrationEvents.Handlers;
using Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Catalog.Tests;

public class OrderCreatedHandlerTests
{
    [Fact]
    public async Task HandleAsync_reserves_stock_and_publishes_StockReserved_when_stock_sufficient()
    {
        using var db = TestDbContextFactory.CreateInMemory(out var connection);
        using var _ = connection;

        var product = Product.Create("Test Ürün", null, 10m, 5, null);
        db.AddWithRowVersion(product);
        await db.SaveChangesAsync();

        var bus = new FakeEventBus();
        var handler = new OrderCreatedHandler(db, bus, NullLogger<OrderCreatedHandler>.Instance);

        var orderId = Guid.NewGuid();
        var @event = new OrderCreatedIntegrationEvent(orderId, Guid.NewGuid(), "user@example.com",
            [new OrderItemLine(product.Id, 3)]);

        await handler.HandleAsync(@event, CancellationToken.None);

        var reloaded = await db.Products.AsNoTracking().FirstAsync(p => p.Id == product.Id);
        Assert.Equal(2, reloaded.Stock);

        var published = Assert.Single(bus.Published);
        var reservedEvent = Assert.IsType<StockReservedIntegrationEvent>(published);
        Assert.Equal(orderId, reservedEvent.OrderId);
    }

    [Fact]
    public async Task HandleAsync_publishes_ReservationFailed_and_reduces_no_stock_when_one_item_insufficient()
    {
        using var db = TestDbContextFactory.CreateInMemory(out var connection);
        using var _ = connection;

        var enoughStock = Product.Create("Yeterli Stoklu", null, 10m, 5, null);
        var notEnoughStock = Product.Create("Yetersiz Stoklu", null, 10m, 1, null);
        db.AddWithRowVersion(enoughStock);
        db.AddWithRowVersion(notEnoughStock);
        await db.SaveChangesAsync();

        var bus = new FakeEventBus();
        var handler = new OrderCreatedHandler(db, bus, NullLogger<OrderCreatedHandler>.Instance);

        var orderId = Guid.NewGuid();
        var @event = new OrderCreatedIntegrationEvent(orderId, Guid.NewGuid(), "user@example.com",
            [new OrderItemLine(enoughStock.Id, 2), new OrderItemLine(notEnoughStock.Id, 5)]);

        await handler.HandleAsync(@event, CancellationToken.None);

        var reloadedEnough = await db.Products.AsNoTracking().FirstAsync(p => p.Id == enoughStock.Id);
        var reloadedNotEnough = await db.Products.AsNoTracking().FirstAsync(p => p.Id == notEnoughStock.Id);

        Assert.Equal(5, reloadedEnough.Stock);
        Assert.Equal(1, reloadedNotEnough.Stock);

        var published = Assert.Single(bus.Published);
        Assert.IsType<StockReservationFailedIntegrationEvent>(published);
    }

    [Fact]
    public async Task HandleAsync_is_idempotent_when_same_message_processed_twice()
    {
        using var db = TestDbContextFactory.CreateInMemory(out var connection);
        using var _ = connection;

        var product = Product.Create("Test Ürün", null, 10m, 5, null);
        db.AddWithRowVersion(product);
        await db.SaveChangesAsync();

        var bus = new FakeEventBus();
        var handler = new OrderCreatedHandler(db, bus, NullLogger<OrderCreatedHandler>.Instance);

        var @event = new OrderCreatedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), "user@example.com",
            [new OrderItemLine(product.Id, 2)]);

        await handler.HandleAsync(@event, CancellationToken.None);
        await handler.HandleAsync(@event, CancellationToken.None);

        var reloaded = await db.Products.AsNoTracking().FirstAsync(p => p.Id == product.Id);
        Assert.Equal(3, reloaded.Stock);
        Assert.Single(bus.Published);
    }
}
