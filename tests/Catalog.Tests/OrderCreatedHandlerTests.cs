using System.Text.Json;
using Catalog.Api.Data;
using Catalog.Api.Domain;
using Catalog.Api.IntegrationEvents.Handlers;
using Common.Correlation;
using Contracts;
using EventBus.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Catalog.Tests;

public class OrderCreatedHandlerTests
{
    private static IOutbox CreateOutbox(CatalogDbContext db) => new EfOutbox<CatalogDbContext>(db, new CorrelationIdAccessor());

    private static async Task<IReadOnlyList<IntegrationEvent>> GetOutboxEventsAsync(CatalogDbContext db)
    {
        var messages = await db.OutboxMessages.AsNoTracking().ToListAsync();
        return messages
            .Select(m => (IntegrationEvent)JsonSerializer.Deserialize(m.Content, Type.GetType(m.Type)!, new JsonSerializerOptions(JsonSerializerDefaults.Web))!)
            .ToList();
    }

    [Fact]
    public async Task HandleAsync_reserves_stock_and_enqueues_StockReserved_in_outbox()
    {
        using var db = TestDbContextFactory.CreateInMemory(out var connection);
        using var _ = connection;

        var product = Product.Create("Test Ürün", null, 10m, 5, null);
        db.AddWithRowVersion(product);
        await db.SaveChangesAsync();

        var handler = new OrderCreatedHandler(db, CreateOutbox(db), NullLogger<OrderCreatedHandler>.Instance);

        var orderId = Guid.NewGuid();
        var @event = new OrderCreatedIntegrationEvent(orderId, Guid.NewGuid(), "user@example.com",
            [new OrderItemLine(product.Id, 3)]);

        await handler.HandleAsync(@event, CancellationToken.None);

        var reloaded = await db.Products.AsNoTracking().FirstAsync(p => p.Id == product.Id);
        Assert.Equal(2, reloaded.Stock);

        var outboxEvents = await GetOutboxEventsAsync(db);
        var enqueued = Assert.Single(outboxEvents);
        var reservedEvent = Assert.IsType<StockReservedIntegrationEvent>(enqueued);
        Assert.Equal(orderId, reservedEvent.OrderId);
    }

    [Fact]
    public async Task HandleAsync_enqueues_ReservationFailed_and_reduces_no_stock_when_one_item_insufficient()
    {
        using var db = TestDbContextFactory.CreateInMemory(out var connection);
        using var _ = connection;

        var enoughStock = Product.Create("Yeterli Stoklu", null, 10m, 5, null);
        var notEnoughStock = Product.Create("Yetersiz Stoklu", null, 10m, 1, null);
        db.AddWithRowVersion(enoughStock);
        db.AddWithRowVersion(notEnoughStock);
        await db.SaveChangesAsync();

        var handler = new OrderCreatedHandler(db, CreateOutbox(db), NullLogger<OrderCreatedHandler>.Instance);

        var orderId = Guid.NewGuid();
        var @event = new OrderCreatedIntegrationEvent(orderId, Guid.NewGuid(), "user@example.com",
            [new OrderItemLine(enoughStock.Id, 2), new OrderItemLine(notEnoughStock.Id, 5)]);

        await handler.HandleAsync(@event, CancellationToken.None);

        var reloadedEnough = await db.Products.AsNoTracking().FirstAsync(p => p.Id == enoughStock.Id);
        var reloadedNotEnough = await db.Products.AsNoTracking().FirstAsync(p => p.Id == notEnoughStock.Id);

        Assert.Equal(5, reloadedEnough.Stock);
        Assert.Equal(1, reloadedNotEnough.Stock);

        var outboxEvents = await GetOutboxEventsAsync(db);
        var enqueued = Assert.Single(outboxEvents);
        Assert.IsType<StockReservationFailedIntegrationEvent>(enqueued);
    }

    [Fact]
    public async Task HandleAsync_is_idempotent_when_same_message_processed_twice()
    {
        using var db = TestDbContextFactory.CreateInMemory(out var connection);
        using var _ = connection;

        var product = Product.Create("Test Ürün", null, 10m, 5, null);
        db.AddWithRowVersion(product);
        await db.SaveChangesAsync();

        var handler = new OrderCreatedHandler(db, CreateOutbox(db), NullLogger<OrderCreatedHandler>.Instance);

        var @event = new OrderCreatedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), "user@example.com",
            [new OrderItemLine(product.Id, 2)]);

        await handler.HandleAsync(@event, CancellationToken.None);
        await handler.HandleAsync(@event, CancellationToken.None);

        var reloaded = await db.Products.AsNoTracking().FirstAsync(p => p.Id == product.Id);
        Assert.Equal(3, reloaded.Stock);

        var outboxEvents = await GetOutboxEventsAsync(db);
        Assert.Single(outboxEvents);
    }
}
